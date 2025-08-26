using AutoMapper;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.Helpers.MailHelper;
using Orbits.GeneralProject.BLL.Helpers.SMSHelper;
using Orbits.GeneralProject.BLL.Validation.LoginValidation;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO.HelperDtos.MailDtos;
using Orbits.GeneralProject.DTO.LoginDtos;
using Orbits.GeneralProject.DTO.Setting.Authentication;
using Orbits.GeneralProject.DTO.Setting.EmailBodyTemplateSetting;
using Orbits.GeneralProject.DTO.Setting.FilesPath;
using Orbits.GeneralProject.DTO.Setting.FrontSettings;
using Orbits.GeneralProject.DTO.Setting.SMSSettings;
using Orbits.GeneralProject.DTO.Setting.ToolsSettings;
using Orbits.GeneralProject.Repositroy.Base;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Orbits.GeneralProject.BLL.AuthenticationService
{
    public class AuthBLL : IAuthBLL
    {
        private readonly IRepository<User> _userRepository;
        private readonly AuthSetting _authSetting;
        private readonly OTPSetting _amanaSetting;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        //private readonly IRepository<FileUpload> _fileUploadRepository;
        private readonly BaseUrl _baseUrlSetting;
        //private readonly IFileServiceBLL _fileService;
        private readonly ISMSServiceBLL _smsService;
        private readonly ToolsSetting _toolsSetting;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly EmailBodyTemplateSetting _emailBodyTemplateSetting;
        private readonly FrontSettings _frontSetting;
        private readonly IMailServiceBLL _mailService;
        private readonly IMemoryCache _memoryCache;

        public AuthBLL(IRepository<User> userRepository,
            //IFileServiceBLL fileService,
            IOptions<BaseUrl> baseUrlSettingSetting,
            //IRepository<FileUpload> fileUploadRepository,
            IOptions<AuthSetting> authSetting, IMapper mapper, IUnitOfWork unitOfWork,
            IOptions<OTPSetting> amanaSetting, ISMSServiceBLL smsService, IOptions<ToolsSetting> toolsSetting, IHostEnvironment hostEnvironment,
            IOptions<EmailBodyTemplateSetting> emailBodyTemplateSetting, IOptions<FrontSettings> frontSetting, IMailServiceBLL mailService,
            IMemoryCache memoryCache)
        {
            _userRepository = userRepository;
            _authSetting = authSetting.Value;
            _mapper = mapper;
            //_fileUploadRepository = fileUploadRepository;
            _unitOfWork = unitOfWork;
            _baseUrlSetting = baseUrlSettingSetting.Value;
            //_fileService = fileService;
            _amanaSetting = amanaSetting.Value;
            _smsService = smsService;
            _toolsSetting = toolsSetting.Value;
            _hostEnvironment = hostEnvironment;
            _emailBodyTemplateSetting = emailBodyTemplateSetting.Value;
            _frontSetting = frontSetting.Value;
            _mailService = mailService;
            _memoryCache = memoryCache;
        }

        public async Task<IResponse<VerifyLoginCodeDto>> Login(LoginDto model)
        {
            Response<VerifyLoginCodeDto> output = new Response<VerifyLoginCodeDto>();
            LoginValidation validation = new LoginValidation();
            ValidationResult validationResult = validation.Validate(model);
            if (!validationResult.IsValid)
                return output.AppendErrors(validationResult.Errors);
            User user = await _userRepository.GetAsync(x => x.Email.Trim().ToLower() == model.Email.Trim().ToLower());
            if (user == null)
                return output.CreateResponse(MessageCodes.FailedToLogin);
            if (user.Inactive || user.IsDeleted)
                return output.AppendError(MessageCodes.NotActive);

            if (string.IsNullOrEmpty(user.PasswordHash))
                return output.CreateResponse(MessageCodes.PasswordIsNullInThisUser);

            // Check if the rate limit has been exceeded
            if (RateLimitReachedForFailedLogin(model.Email))
                return output.CreateResponse(MessageCodes.ReachedForFailedAttempts);
            PasswordHasher<User> passwordHasher = new PasswordHasher<User>();
            PasswordVerificationResult passwordCheck = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
            if (passwordCheck == PasswordVerificationResult.Failed)
            {
                // Log failed attempt and respond
                LoginFailedAttempt(model.Email);
                return output.CreateResponse(MessageCodes.FailedToLogin);
            }
            // If the password is correct, reset failed login attempts
            ResetFailedLoginAttempts(model.Email);

            VerifyLoginCodeDto codeDto = new VerifyLoginCodeDto();
            string code = new Random().Next(1000, 9999).ToString();

            if (_amanaSetting.CanSendToFront)
            {
                var body = GetOTPBodyFromHTMLFile(user.Email, code);
                var message = await _mailService.SendEmail(new EmailMessage
                {
                    To = model.Email,
                    Subject = "OTP",
                    Body = body,
                });
                if (message != null && !message.IsSuccess)
                {
                    return output.CreateResponse(MessageCodes.CouldnotSendEmail);
                }
            }
            else
            {
                codeDto.Code = code;
            }
            codeDto.PasswordIsCorrect = true;
            codeDto.Email = model.Email;
            user.Code = code.Trim();
            user.CodeExpirationTime = DateTime.Now;
            _userRepository.Update(user);
            await _unitOfWork.CommitAsync();
            return output.CreateResponse(codeDto);
        }
        public async Task<IResponse<LoginResultDto>> VerifyCode(VerifyLoginCodeDto model)
        {
            Response<LoginResultDto> output = new Response<LoginResultDto>();

            VerifyLoginCodeValidation validation = new VerifyLoginCodeValidation();
            ValidationResult validationResult = validation.Validate(model);
            if (!validationResult.IsValid)
                return output.AppendErrors(validationResult.Errors);
            User user = await _userRepository.GetAsync(x => x.Email.Trim().ToLower() == model.Email.Trim().ToLower());
            if (user == null)
                return output.CreateResponse(MessageCodes.FailedToLogin);

            // Check if the rate limit has been exceeded
            if (user.CodeExpirationTime > DateTime.Now.AddMinutes(2) || RateLimitReachedForFailedVerifyCode(model.Email))
            {
                return output.CreateResponse(MessageCodes.CodeNotValidAnyMore);
            }

            if (user.Code.Trim() != model.Code)
            {
                // Log failed attempt and respond
                VerifyCodeFailedAttempt(model.Email);
                return output.CreateResponse(MessageCodes.InvalidCode);
            }
            // If the Code is correct, reset failed Verify Code attempts
            ResetFailedVerifyCodeAttempts(model.Email);

            //return model if success
            LoginResultDto loginResult = new LoginResultDto();
            //check if any refresh token not active in RefreshToken Table 
            JwtSecurityToken generatedToken = this.GenerateToken(user, DateTime.Now.AddDays(Convert.ToDouble(_authSetting.ExpiryInDays)));
            string tokenString = new JwtSecurityTokenHandler().WriteToken(generatedToken);
            if (user.RefreshTokens.Any())
            {
                RefreshToken activeRefreshToken = user.RefreshTokens.FirstOrDefault();
                loginResult.RefreshToken = activeRefreshToken.Token;
            }
            // else generate new refresh token and save it in database 
            else
            {
                JwtSecurityToken refreshToken = this.GenerateRefreshToken(user);
                string generatedRefreshToken = new JwtSecurityTokenHandler().WriteToken(refreshToken);
                loginResult.RefreshToken = generatedRefreshToken;
                user.RefreshTokens.Add(new RefreshToken
                {
                    CreatedOn = DateTime.Now,
                    ExpiresOn = refreshToken.ValidTo,
                    Token = generatedRefreshToken,
                    UserId = user.Id
                });
            }
            loginResult.Token = tokenString;
            loginResult.Role = user.UserTypeId;
            user.Code = null;
            user.CodeExpirationTime = null;
            _userRepository.Update(user);
            await _unitOfWork.CommitAsync();
            return output.CreateResponse(loginResult);
        }
        //public async Task<IResponse<LoginResultDto>> RefreshToken(RequestForRefreshTokenDto model)
        //{
        //    Response<LoginResultDto> output = new Response<LoginResultDto>();
        //    LoginResultDto loginResult = new LoginResultDto();
        //    var refreshTokenClaims = this.GetPrincipalFromToken(model.RefreshToken);
        //    var oldTokenClaims = this.GetPrincipalFromToken(model.OldToken);
        //    if (!refreshTokenClaims.Claims.Any() && !oldTokenClaims.Claims.Any())
        //        return output.CreateResponse(MessageCodes.NotFound);
        //    if (string.IsNullOrEmpty(refreshTokenClaims.FindFirstValue(ClaimTypes.NameIdentifier)) ||
        //        string.IsNullOrEmpty(oldTokenClaims.FindFirstValue(ClaimTypes.NameIdentifier))
        //        || refreshTokenClaims.FindFirstValue(ClaimTypes.NameIdentifier) != oldTokenClaims.FindFirstValue(ClaimTypes.NameIdentifier))
        //        return output.CreateResponse(MessageCodes.NotFound);
        //    User user = await _userRepository.GetByIdAsync(int.Parse(refreshTokenClaims.FindFirstValue(ClaimTypes.NameIdentifier)));
        //    JwtSecurityToken currentToken = this.GenerateToken(user, DateTime.Now.AddDays(Convert.ToDouble(_authSetting.ExpiryInDays)));
        //    string tokenString = new JwtSecurityTokenHandler().WriteToken(currentToken);
        //    JwtSecurityToken refreshToken = this.GenerateRefreshToken(user);
        //    string generatedRefreshToken = new JwtSecurityTokenHandler().WriteToken(refreshToken);
        //    if (user.RefreshTokens.Any())
        //        user.RefreshTokens.FirstOrDefault().Token = generatedRefreshToken;
        //    loginResult.RefreshToken = generatedRefreshToken;
        //    loginResult.Token = tokenString;
        //    loginResult.Role = user.UserType?.Id;
        //    loginResult.Username = user.Username;
        //    return output.CreateResponse(loginResult);
        //}
        //public async Task<IResponse<ProfileDto>> GetProfile(int userId)
        //{
        //    Response<ProfileDto> output = new Response<ProfileDto>();

        //    User user = await _userRepository.GetByIdAsync(userId);
        //    if (user != null)
        //    {
        //        ProfileDto mappdedResult = _mapper.Map<ProfileDto>(user);
        //        var result = _fileUploadRepository.Where(x => x.Id == user.FileId).Select(x => new
        //        {
        //            x.FilePath,
        //        }).FirstOrDefault();
        //        if (result != null)
        //        {
        //            string fullPath = _baseUrlSetting.BaseServerURL + "/";
        //            mappdedResult.PhotoPath = Path.Combine(fullPath, result.FilePath).Replace("\\", "/");
        //        }
        //        return output.CreateResponse(mappdedResult);
        //    }
        //    return output.CreateResponse(MessageCodes.NotFound);
        //}
        public JwtSecurityToken GenerateToken(User user, DateTime expireOn)
        {
            List<Claim> claims = new List<Claim>
                        {
                         new Claim (ClaimTypes.Name,user.FullName),
                         new Claim (ClaimTypes.NameIdentifier,user.Id.ToString()),
                         new Claim (ClaimTypes.Email,user.Email ?? ""),
                         new Claim (ClaimTypes.Role , user.UserTypeId.ToString())
                        };
            var key = Encoding.ASCII.GetBytes(_authSetting.Key);
            JwtSecurityToken tokenOptions = new JwtSecurityToken(
                claims: claims,
                expires: expireOn,
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256));
            return tokenOptions;
        }
        public JwtSecurityToken GenerateRefreshToken(User user)
        {
            List<Claim> claims = new List<Claim>
                        {
                         new Claim (ClaimTypes.NameIdentifier,user.Id.ToString())
                        };
            var key = Encoding.ASCII.GetBytes(_authSetting.Key);
            JwtSecurityToken tokenOptions = new JwtSecurityToken(
                claims: claims,
                expires: null,
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256));
            return tokenOptions;
        }
        //public ClaimsPrincipal GetPrincipalFromToken(string token)
        //{
        //    if (string.IsNullOrEmpty(token))
        //        throw new ArgumentException($"'{nameof(token)}' cannot be null or empty.", nameof(token));
        //    var jwtKey = _authSetting.Key;
        //    var tokenValidationParameters = new TokenValidationParameters
        //    {
        //        ValidateAudience = false,
        //        ValidateIssuer = false,
        //        ValidateIssuerSigningKey = true,
        //        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        //        ValidateLifetime = false
        //    };
        //    var tokenHandler = new JwtSecurityTokenHandler();
        //    var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
        //    return securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase)
        //        ? throw new SecurityTokenException("Invalid token")
        //        : principal;
        //}
        //public async Task<IResponse<bool>> ActiveUser(ActivateUserDto dto)
        //{
        //    var output = new Response<bool>();
        //    try
        //    {
        //        ActivateUserValidation validation = new ActivateUserValidation();
        //        ValidationResult validationResult = validation.Validate(dto);
        //        if (!validationResult.IsValid)
        //        {
        //            return output.AppendErrors(validationResult.Errors);
        //        }
        //        dto.Email = EncryptTools.DecryptString(dto.Email, _toolsSetting.EncryptKey);
        //        var entity = _userRepository.DisableFilter("Inactive").FirstOrDefault(x => x.Email == dto.Email);
        //        if (entity == null)
        //            return output.CreateResponse(MessageCodes.NotFound);
        //        //if (!string.IsNullOrEmpty(entity.Password))
        //        //    return output.CreateResponse(MessageCodes.CannotActiveThisAccountByEmail);
        //        entity.Inactive = false;
        //        if (string.IsNullOrEmpty(dto.Password))
        //            return output.CreateResponse(MessageCodes.PasswordIsRequired);
        //        entity.Password = GenerateHashPassword(entity, dto.Password);

        //        await _unitOfWork.CommitAsync();
        //        if (entity.UserTypeId == (int)UserTypesEnum.Supervisor)
        //        {
        //            _memoryCache.Remove(CacheKeys.ActiveSupervisor);
        //        }
        //        return output.CreateResponse(true);
        //    }
        //    catch (Exception ex)
        //    {
        //        return output.CreateResponse(MessageCodes.Exception, ex.Message);
        //    }
        //}
        //public async Task<IResponse<string>> ChangePassword(ChangePasswordDto dto, int userId)
        //{
        //    var output = new Response<string>();
        //    try
        //    {
        //        ChangePasswordValidation validation = new ChangePasswordValidation();
        //        ValidationResult validationResult = validation.Validate(dto);
        //        if (!validationResult.IsValid)
        //        {
        //            return output.AppendErrors(validationResult.Errors);
        //        }
        //        var user = await _userRepository.GetByIdAsync(userId);
        //        if (user != null)
        //        {
        //            user.Password = this.GenerateHashPassword(user, dto.NewPassword);
        //            await _unitOfWork.CommitAsync();
        //        }
        //        return output.CreateResponse(LoginValidationReponseConstants.ChangePasswordCodeSent);
        //    }
        //    catch (Exception ex)
        //    {
        //        return output.AppendError(MessageCodes.Exception, ex.Message);
        //    }
        //}
        //public async Task<IResponse<string>> ForgetPassword(ForgetPasswordDto dto)
        //{
        //    var output = new Response<string>();
        //    try
        //    {
        //        ForgetPasswordValidation validation = new ForgetPasswordValidation();
        //        ValidationResult validationResult = validation.Validate(dto);
        //        if (!validationResult.IsValid)
        //        {
        //            return output.AppendErrors(validationResult.Errors);
        //        }
        //        var user = await _userRepository.GetAsync(x => x.Email == dto.Email);
        //        if (user != null)
        //        {
        //            var fullPath = Path.Combine(_hostEnvironment.ContentRootPath,
        //                _emailBodyTemplateSetting.EmailBodyPath,
        //                _emailBodyTemplateSetting.resetPassword);
        //            var body = System.IO.File.ReadAllText(fullPath);
        //            var encryptEmail = EncryptTools.EncryptString(user.Email, _toolsSetting.EncryptKey);
        //            var result = await _mailService.SendEmail(new DTO.HelperDtos.MailDtos.EmailMessage
        //            {
        //                Subject = "نسيت كلمة المرور",
        //                Body = body
        //                        .Replace("{%UserName%}", user.Username)
        //                        .Replace("%Link%", this.GenerateLink(_frontSetting.FrontLinkURL, _frontSetting.ResetPassword) + encryptEmail),
        //                To = user.Email
        //            });
        //        }
        //        return output.CreateResponse(LoginValidationReponseConstants.ChangePasswordCodeSent);
        //    }
        //    catch (Exception ex)
        //    {
        //        return output.AppendError(MessageCodes.Exception, ex.Message);
        //    }
        //}
        //public async Task<IResponse<string>> ResetPassword(ChangePasswordDto dto)
        //{
        //    var output = new Response<string>();
        //    try
        //    {
        //        ChangePasswordValidation validation = new ChangePasswordValidation();
        //        ValidationResult validationResult = validation.Validate(dto);
        //        if (!validationResult.IsValid)
        //        {
        //            return output.AppendErrors(validationResult.Errors);
        //        }
        //        dto.Email = EncryptTools.DecryptString(dto.Email, _toolsSetting.EncryptKey);
        //        var user = await _userRepository.GetAsync(x => x.Email == dto.Email);
        //        if (user != null)
        //        {
        //            user.Password = this.GenerateHashPassword(user, dto.NewPassword);
        //            await _unitOfWork.CommitAsync();
        //        }
        //        return output.CreateResponse(LoginValidationReponseConstants.ChangePasswordCodeSent);
        //    }
        //    catch (Exception ex)
        //    {
        //        return output.AppendError(MessageCodes.Exception, ex.Message);
        //    }
        //}
        //private string GenerateHashPassword(User user, string newPassword)
        //{
        //    PasswordHasher<User> passwordHasher = new PasswordHasher<User>();
        //    string hashedPassword = passwordHasher.HashPassword(user, newPassword);
        //    return hashedPassword;
        //}
        //public async Task<IResponse<bool>> UpdateProfile(UpdateAccountDto updateAccountDto, int userId)
        //{
        //    Response<bool> output = new Response<bool>();
        //    FileUpload fileid = new FileUpload();
        //    UpdateProfileValidation validation = new UpdateProfileValidation();
        //    ValidationResult validationResult = validation.Validate(updateAccountDto);
        //    if (!validationResult.IsValid)
        //        return output.AppendErrors(validationResult.Errors);
        //    User user = await _userRepository.GetByIdAsync(userId);
        //    if (!string.IsNullOrEmpty(updateAccountDto.Password))
        //        user.Password = this.GenerateHashPassword(user, updateAccountDto.Password);
        //    if (!string.IsNullOrEmpty(updateAccountDto.Username))
        //        user.Username = updateAccountDto.Username;
        //    if (!string.IsNullOrEmpty(updateAccountDto.Mobile))
        //    {
        //        if (_userRepository.Any(x => x.Mobile == updateAccountDto.Mobile && x.Id != user.Id))
        //            return output.CreateResponse(MessageCodes.PhoneNumberAlreadyExisted);
        //        user.Mobile = updateAccountDto.Mobile;
        //    }
        //    if (updateAccountDto.BirthDate.HasValue)
        //        user.BirthDate = updateAccountDto.BirthDate;
        //    if (!string.IsNullOrEmpty(updateAccountDto.NationalId))
        //    {
        //        if (_userRepository.Any(x => x.NationalId == updateAccountDto.NationalId && x.Id != user.Id))
        //            return output.CreateResponse(MessageCodes.NationalIdAlreadyExisted);
        //        user.NationalId = updateAccountDto.NationalId;
        //    }
        //    if (!string.IsNullOrEmpty(updateAccountDto.JobName))
        //        user.JobName = updateAccountDto.JobName;
        //    await _unitOfWork.CommitAsync();
        //    return output.CreateResponse(true);
        //}

        //public async Task<IResponse<bool>> UpdateProfilePicture(IFormFile file, int userId)
        //{
        //    Response<bool> output = new Response<bool>();
        //    FileUpload fileid = new FileUpload();
        //    User user = await _userRepository.GetByIdAsync(userId);
        //    FileUpload photoToUpdate = await _fileUploadRepository.GetAsync(x => x.Id == user.FileId);
        //    var FileUpload = await _fileService.AddProfilePhoto(file);
        //    if (FileUpload == null) { return output.AppendErrors(FileUpload.Errors); }
        //    if (photoToUpdate != null)
        //    {
        //        _fileService.DeleteFile(photoToUpdate.FilePath);
        //        photoToUpdate.FilePath = FileUpload.Data.FilePath;
        //        photoToUpdate.OriginalName = FileUpload.Data.OriginalName;
        //    }
        //    else
        //    {
        //        FileUpload a = await _fileUploadRepository.AddAsync(FileUpload.Data);
        //        await _unitOfWork.CommitAsync();
        //        fileid.Id = a.Id;
        //        user.FileId = fileid.Id;
        //    }
        //    await _unitOfWork.CommitAsync();
        //    return output.CreateResponse(true);
        //}
        //public string ReadEmailBodyFromHTMLFile(string path, string fileName, string email, string userName)
        //{
        //    try
        //    {
        //        var fullPath = Path.Combine(_hostEnvironment.ContentRootPath, path, fileName);
        //        var body = System.IO.File.ReadAllText(fullPath);
        //        var encryptEmail = EncryptTools.EncryptString(email, _toolsSetting.EncryptKey);
        //        if (!string.IsNullOrEmpty(body))
        //            //To do read Link From Appsetting
        //            body = body.Replace("{%UserName%}", userName)
        //                       .Replace("%Link%", this.GenerateLink(_frontSetting.FrontLinkURL, _frontSetting.ActiveInvitation) +
        //                encryptEmail);
        //        return body;
        //    }
        //    catch (Exception ex)
        //    {
        //        return ex.Message;
        //    }
        //}
        public string GetOTPBodyFromHTMLFile(string userName, string code)
        {
            try
            {
                var filePath = Path.Combine(_hostEnvironment.ContentRootPath, "Templates\\EmailBody", "otpTemplate.html");
                var body = System.IO.File.ReadAllText(filePath);
                if (!string.IsNullOrEmpty(body))
                    body = body.Replace("{%UserName%}", userName)
                        .Replace("{%OTP%}", code);
                return body;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        private string GenerateLink(string url, string actionMethod) =>
             url + actionMethod;

        private bool RateLimitReachedForFailedLogin(string userEmail)
        {
            var cacheKey = $"LoginAttempts_{userEmail}";
            int maxLoginAttempts = 5;
            int timeWindowSeconds = 300; // 5 minutes time window

            var attempts = _memoryCache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(timeWindowSeconds);
                return new List<DateTime>();
            });

            // Remove attempts outside the time window
            attempts.RemoveAll(attemptTime => attemptTime < DateTime.Now.AddSeconds(-timeWindowSeconds));

            // If the rate limit is reached, return true
            return attempts.Count >= maxLoginAttempts;
        }

        private void LoginFailedAttempt(string userEmail)
        {
            var cacheKey = $"LoginAttempts_{userEmail}";
            var attempts = _memoryCache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(300);
                return new List<DateTime>();
            });

            // Log the current failed attempt
            attempts.Add(DateTime.Now);
            _memoryCache.Set(cacheKey, attempts, TimeSpan.FromSeconds(300));
        }

        private void ResetFailedLoginAttempts(string userEmail)
        {
            var cacheKey = $"LoginAttempts_{userEmail}";
            _memoryCache.Remove(cacheKey); // Remove the failed attempts cache
        }

        private bool RateLimitReachedForFailedVerifyCode(string userEmail)
        {
            var cacheKey = $"VerifyCodeAttempts_{userEmail}";
            int maxVerifyCodeAttempts = 4;
            int timeWindowSeconds = 120; // 5 minutes time window

            var attempts = _memoryCache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(timeWindowSeconds);
                return new List<DateTime>();
            });

            // Remove attempts outside the time window
            attempts.RemoveAll(attemptTime => attemptTime < DateTime.Now.AddSeconds(-timeWindowSeconds));

            // If the rate limit is reached, return true
            return attempts.Count >= maxVerifyCodeAttempts;
        }

        private void VerifyCodeFailedAttempt(string userEmail)
        {
            var cacheKey = $"VerifyCodeAttempts_{userEmail}";
            var attempts = _memoryCache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(300);
                return new List<DateTime>();
            });

            // Log the current failed attempt
            attempts.Add(DateTime.Now);
            _memoryCache.Set(cacheKey, attempts, TimeSpan.FromSeconds(300));
        }

        private void ResetFailedVerifyCodeAttempts(string userEmail)
        {
            var cacheKey = $"VerifyCodeAttempts_{userEmail}";
            _memoryCache.Remove(cacheKey); // Remove the failed attempts cache
        }
    }
}
