using AutoMapper;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.BLL.Validation.UserValidation;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO.UserDto;
using Orbits.GeneralProject.DTO.UserDtos;
using Orbits.GeneralProject.Repositroy.Base;

namespace Orbits.GeneralProject.BLL.UserService
{
    public class UserBLL : BaseBLL, IUserBLL
    {
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Circle> _circleRepository;
        private readonly IRepository<ManagerCircle> _managerCircleRepository;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public UserBLL(IMapper mapper, IRepository<User> userrepository,
             IUnitOfWork unitOfWork,
             IHostEnvironment hostEnvironment, IRepository<ManagerCircle> managerCircleRepository, IRepository<Circle> circleRepository) : base(mapper)
        {
            _userRepository = userrepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _managerCircleRepository = managerCircleRepository;
            _circleRepository = circleRepository;
        }

        public async Task<IResponse<bool>> Add(DTO.UserDto.CreateUserDto createUserDto)
        {
            Response<bool> output = new Response<bool>();
            // Validations

            AddUserValidation validation = new AddUserValidation();
            ValidationResult validationResult = validation.Validate(createUserDto);
            if (!validationResult.IsValid)
                return output.AppendErrors(validationResult.Errors);

            if (await _userRepository.AnyAsync(x => x.Mobile == createUserDto.Mobile))
                return output.CreateResponse(MessageCodes.PhoneNumberAlreadyExisted);

            if (await _userRepository.AnyAsync(x => x.Email == createUserDto.Email))
                return output.CreateResponse(MessageCodes.EmailAlreadyExists);

            if (string.IsNullOrEmpty(createUserDto.PasswordHash))
                return output.CreateResponse(MessageCodes.PasswordIsRequired);

            // Create User
            var user = _mapper.Map<User>(createUserDto);
            user.RegisterAt = DateTime.Now;
           
            user.Inactive = false;
            user.IsDeleted = false;
            if (user.UserTypeId == (int)UserTypesEnum.Student)
            {
                user.Inactive = false;
            }
            user.PasswordHash = GenerateHashPassword(user, createUserDto.PasswordHash);

            await _userRepository.AddAsync(user);
            await _unitOfWork.CommitAsync(); // Save user and generate ID
       

            await _unitOfWork.CommitAsync(); // Persist all changes
            return output.CreateResponse(true);

        }

        public async Task<IResponse<bool>> Update(UpdateUserDto updateUserDto, int? userid)
        {
            var output = new Response<bool>();

            // 1) Validate DTO
            var validation = new UpdateUserValidation();
            ValidationResult validationResult = validation.Validate(updateUserDto);
            if (!validationResult.IsValid)
                return output.AppendErrors(validationResult.Errors);

            // 2) Caller (??????? ?? ????? ???????)
            var loggedInUser = _userRepository.Get(x => x.Id == userid);

            // 3) Fetch target user
            var existedUser = await _userRepository.GetByIdAsync(updateUserDto.Id);
            if (existedUser == null)
                return output.CreateResponse(MessageCodes.NotFound);

            // 4) Uniqueness: Mobile
            if (!string.IsNullOrWhiteSpace(updateUserDto.Mobile))
            {
                bool isExistedByPhoneNumber = await _userRepository.AnyAsync(
                    x => x.Mobile == updateUserDto.Mobile && x.Id != existedUser.Id
                );
                if (isExistedByPhoneNumber)
                    return output.CreateResponse(MessageCodes.PhoneNumberAlreadyExisted);
            }

            // 5) Map basic fields
            _mapper.Map(updateUserDto, existedUser);
            existedUser.ModefiedAt = DateTime.Now;
            existedUser.ModefiedBy = userid;
            var studentIds = (updateUserDto.StudentIds ?? new List<int>())
                                .Where(id => id > 0).Distinct().ToHashSet();
            // 6) ?? ???????? ?? Manager ????? ???/??? ???????? ???????
            var isManager = (UserTypesEnum)(existedUser.UserTypeId ?? 0) == UserTypesEnum.Manager;
            if (isManager)
            {
                // --- Normalize incoming IDs (?????? 0 ?????? ???????)
                var teacherIds = (updateUserDto.TeacherIds ?? new List<int>())
                                 .Where(id => id > 0).Distinct().ToHashSet();
               
                // IMPORTANT: ?????? Query() ?? ?????? ?? _dbContext.Users.AsQueryable()
                var usersQuery = _userRepository.GetAll();

                // ===== TEACHERS =====
                // ???????? ????????? ???? ??????
                var currentTeachers =  usersQuery
                    .Where(u => u.ManagerId == existedUser.Id
                                && u.UserTypeId == (int)UserTypesEnum.Teacher).ToList();

                // ????????? ??????? ????? (????? ?? ?????)
                var teachersToAssign = teacherIds.Count == 0
                    ? new List<User>()
                    :  usersQuery
                        .Where(u => teacherIds.Contains(u.Id)
                                    && u.UserTypeId == (int)UserTypesEnum.Teacher)
                        .ToList();

                // ???? ?? ???? ?? ??????
                foreach (var t in teachersToAssign)
                {
                    if (t.ManagerId != existedUser.Id)
                    {
                        t.ManagerId = existedUser.Id;
                        t.ModefiedAt = DateTime.Now;
                        t.ModefiedBy = userid;
                        _userRepository.Update(t);
                    }
                }

                // ???? ?? ?? ??? ????? ??? ????? ?? ?????? ???????
                var teachersToDetach = currentTeachers.Where(t => !teacherIds.Contains(t.Id)).ToList();
                foreach (var t in teachersToDetach)
                {
                    t.ManagerId = null;
                    t.ModefiedAt = DateTime.Now;
                    t.ModefiedBy = userid;
                    _userRepository.Update(t);
                }

                // ===== STUDENTS =====
                var currentStudents =  usersQuery
                    .Where(u => u.ManagerId == existedUser.Id
                                && u.UserTypeId == (int)UserTypesEnum.Student)
                    .ToList();

                var studentsToAssign = studentIds.Count == 0
                    ? new List<User>()
                    :  usersQuery
                        .Where(u => studentIds.Contains(u.Id)
                                    && u.UserTypeId == (int)UserTypesEnum.Student)
                        .ToList();

                foreach (var s in studentsToAssign)
                {
                    if (s.ManagerId != existedUser.Id)
                    {
                        s.ManagerId = existedUser.Id;
                        s.ModefiedAt = DateTime.Now;
                        s.ModefiedBy = userid;
                        _userRepository.Update(s);
                    }
                }

                var studentsToDetach = currentStudents.Where(s => !studentIds.Contains(s.Id)).ToList();
                foreach (var s in studentsToDetach)
                {
                    s.ManagerId = null;
                    s.ModefiedAt = DateTime.Now;
                    s.ModefiedBy = userid;
                    _userRepository.Update(s);
                }
            }
            var IsTeacher = (UserTypesEnum)(existedUser.UserTypeId ?? 0) == UserTypesEnum.Teacher;
            if (IsTeacher)
            {
                // --- Normalize incoming IDs (?????? 0 ?????? ???????)
               
                // IMPORTANT: ?????? Query() ?? ?????? ?? _dbContext.Users.AsQueryable()
                var usersQuery = _userRepository.GetAll();

                // ===== TEACHERS =====
                // ???????? ????????? ???? ??????
                var currentStudents = usersQuery
                    .Where(u => u.TeacherId == existedUser.Id
                                && u.UserTypeId == (int)UserTypesEnum.Student).ToList();

                // ????????? ??????? ????? (????? ?? ?????)
                var studentsToAssign = studentIds.Count == 0
                    ? new List<User>()
                    : usersQuery
                        .Where(u => studentIds.Contains(u.Id)
                                    && u.UserTypeId == (int)UserTypesEnum.Student)
                        .ToList();

                // ???? ?? ???? ?? ??????
                foreach (var t in studentsToAssign)
                {
                    
                    {
                        t.TeacherId = existedUser.Id;
                        t.CircleId = existedUser.CircleId;
                        t.ModefiedAt = DateTime.Now;
                        t.ModefiedBy = userid;
                        _userRepository.Update(t);
                    }
                }

                // ???? ?? ?? ??? ????? ??? ????? ?? ?????? ???????
                var studentsToDetach = currentStudents.Where(t => !studentIds.Contains(t.Id)).ToList();
                foreach (var t in studentsToDetach)
                {
                    t.TeacherId = null;
                    t.CircleId = null;
                    t.ModefiedAt = DateTime.Now;
                    t.ModefiedBy = userid;
                    _userRepository.Update(t);
                }
                if (updateUserDto.CircleId != null )
                {
                    var updatedCircle = _circleRepository.GetById(existedUser.CircleId.Value);
                    updatedCircle.TeacherId = existedUser.Id;
                    updatedCircle.ModefiedAt = DateTime.Now;
                    updatedCircle.ModefiedBy = userid;
                    _circleRepository.Update(updatedCircle);
                }
             

            }

            // ===== MANAGER CIRCLES (refresh only the provided CircleIds) =====
            if (existedUser.UserTypeId == (int)UserTypesEnum.Manager)
            {
                var circleIds = (updateUserDto.CircleIds ?? new List<int>()).Where(id => id > 0).Distinct().ToList();
            
                // 1) Remove old rows for this manager where CircleId is in the provided list
                var oldRows = _managerCircleRepository.GetAll()
                    .Where(mc => mc.ManagerId == existedUser.Id)
                    .ToList();

                foreach (var mc in oldRows)
                    _managerCircleRepository.Delete(mc);
            if (circleIds.Count > 0)
            {
                // 2) Re-insert fresh rows for exactly these CircleIds
                foreach (var cid in circleIds)
                {
                    _managerCircleRepository.Add(new ManagerCircle
                    {
                        ManagerId = existedUser.Id,
                        CircleId = cid,
                        ModefiedAt = DateTime.Now,
                        ModefiedBy = userid
                        // If you have audit fields, set them here (e.g., CreatedAt/CreatedBy)
                    });
                }
            }
            }
           
            // 7) Update main user
            _userRepository.Update(existedUser);

            // 8) Commit once
            await _unitOfWork.CommitAsync();

            return output.CreateResponse(true);
        }


        public async Task<IResponse<ProfileDto>> GetProfile(int userId)
        {
            var output = new Response<ProfileDto>();

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || user.IsDeleted)
                return output.CreateResponse(MessageCodes.NotFound);

            var mappedResult = _mapper.Map<ProfileDto>(user);

            return output.CreateResponse(mappedResult);
        }

        public async Task<IResponse<bool>> UpdateProfile(UpdateProfileDto updateProfileDto, int userId)
        {
            var output = new Response<bool>();

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || user.IsDeleted)
                return output.CreateResponse(MessageCodes.NotFound);

            if (updateProfileDto.Email != null)
            {
                var normalizedEmail = updateProfileDto.Email.Trim();
                if (!string.IsNullOrWhiteSpace(normalizedEmail) &&
                    !string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
                {
                    var normalizedEmailLower = normalizedEmail.ToLowerInvariant();
                    var emailExists = await _userRepository.AnyAsync(x =>
                        x.Id != userId && x.Email != null &&
                        x.Email.ToLower() == normalizedEmailLower);

                    if (emailExists)
                        return output.CreateResponse(MessageCodes.EmailAlreadyExists);

                    user.Email = normalizedEmail;
                }
                else if (string.IsNullOrWhiteSpace(normalizedEmail))
                {
                    user.Email = null;
                }
            }

            if (updateProfileDto.Mobile != null)
            {
                var normalizedMobile = updateProfileDto.Mobile.Trim();
                if (!string.IsNullOrWhiteSpace(normalizedMobile) &&
                    !string.Equals(user.Mobile, normalizedMobile, StringComparison.Ordinal))
                {
                    var phoneExists = await _userRepository.AnyAsync(x =>
                        x.Id != userId && x.Mobile == normalizedMobile);

                    if (phoneExists)
                        return output.CreateResponse(MessageCodes.PhoneNumberAlreadyExisted);

                    user.Mobile = normalizedMobile;
                }
                else if (string.IsNullOrWhiteSpace(normalizedMobile))
                {
                    user.Mobile = null;
                }
            }

            if (updateProfileDto.FullName != null)
                user.FullName = string.IsNullOrWhiteSpace(updateProfileDto.FullName)
                    ? null
                    : updateProfileDto.FullName.Trim();

            if (updateProfileDto.SecondMobile != null)
                user.SecondMobile = string.IsNullOrWhiteSpace(updateProfileDto.SecondMobile)
                    ? null
                    : updateProfileDto.SecondMobile.Trim();

            if (updateProfileDto.NationalityId.HasValue)
                user.NationalityId = updateProfileDto.NationalityId;

            if (updateProfileDto.GovernorateId.HasValue)
                user.GovernorateId = updateProfileDto.GovernorateId;

            if (updateProfileDto.BranchId.HasValue)
                user.BranchId = updateProfileDto.BranchId;

            user.ModefiedAt = DateTime.Now;
            user.ModefiedBy = userId;

            _userRepository.Update(user);
            await _unitOfWork.CommitAsync();

            return output.CreateResponse(true);
        }


        public async Task<IResponse<bool>> DisableUser(int id ,bool statue)
        {
            Response<bool> output = new Response<bool>();
            User entity = await _userRepository.GetByIdAsync(id);
            if (entity == null)
                return output.CreateResponse(MessageCodes.NotFound);
            if (statue)
            {
                entity.IsDeleted = false;
                entity.Inactive = false;

            }
            else
            {
                entity.Inactive = true;
                entity.IsDeleted = true;


            }
            User result = _userRepository.Update(entity);
            await _unitOfWork.CommitAsync();
            return output.CreateResponse(true);
        }
        // ------------------ Helpers ------------------


        private string GenerateHashPassword(User user, string newPassword)
        {
            PasswordHasher<User> passwordHasher = new PasswordHasher<User>();
            string hashedPassword = passwordHasher.HashPassword(user, newPassword);
            return hashedPassword;
        }
    }


}
