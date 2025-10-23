using Microsoft.AspNetCore.Http;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.DTO.LoginDtos;
using Orbits.GeneralProject.DTO.UserDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.BLL.AuthenticationService
{
    public interface IAuthBLL
    {
        //Task<IResponse<ProfileDto>> GetProfile(int userId);
        Task<IResponse<VerifyLoginCodeDto>> Login(LoginDto model);
        Task<IResponse<LoginResultDto>> VerifyCode(VerifyLoginCodeDto model);
        //Task<IResponse<bool>> UpdateProfile(UpdateAccountDto updateUserDto, int userId);
        //Task<IResponse<bool>> UpdateProfilePicture(IFormFile file, int userId);
        //Task<IResponse<bool>> ActiveUser(ActivateUserDto dto);
        //string ReadEmailBodyFromHTMLFile(string path, string fileName, string email, string userName);
        //string GetOTPBodyFromHTMLFile(string userName, string code);
        Task<IResponse<string>> ForgetPassword(ForgetPasswordDto dto);
        Task<IResponse<string>> ResetPassword(ResetPasswordDto dto);
        Task<IResponse<string>> ChangePassword(ChangePasswordDto dto, int userId);
        //Task<IResponse<LoginResultDto>> RefreshToken(RequestForRefreshTokenDto model);
    }
}
