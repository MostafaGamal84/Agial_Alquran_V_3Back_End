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
using Orbits.GeneralProject.Repositroy.Base;

namespace Orbits.GeneralProject.BLL.UserService
{
    public class UserBLL : BaseBLL, IUserBLL
    {
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Student> _studentRepository;
        private readonly IRepository<Teacher> _teacherRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public UserBLL(IMapper mapper, IRepository<User> userrepository,
             IUnitOfWork unitOfWork,
             IHostEnvironment hostEnvironment,
             IRepository<Student> studentRepository, IRepository<Teacher> teacherRepository) : base(mapper)
        {
            _userRepository = userrepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _studentRepository = studentRepository;
            _teacherRepository = teacherRepository;
        }

        public async Task<IResponse<bool>> Add(CreateUserDto createUserDto)
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
            user.PasswordHash = GenerateHashPassword(user, createUserDto.PasswordHash);

            await _userRepository.AddAsync(user);
            await _unitOfWork.CommitAsync(); // Save user and generate ID
                                             // Determine user type
            switch ((UserTypesEnum)createUserDto.UserTypeId)
            {
                case UserTypesEnum.Teacher:
                    await AddTeacherAsync(user.Id);
                    break;

                case UserTypesEnum.Student:
                    await AddStudentAsync(user.Id);
                    break;

                default:
                    break;
            }

            await _unitOfWork.CommitAsync(); // Persist all changes
            return output.CreateResponse(true);

        }

        // ------------------ Helpers ------------------

        private async Task AddTeacherAsync(int userId)
        {
            var teacher = new Teacher
            {
                Id = userId,
                // IdNavigation = ... (optional navigation assignment)
            };

            await _teacherRepository.AddAsync(teacher);
        }

        private async Task AddStudentAsync(int userId)
        {
            var student = new Student
            {
                Id = userId,
                // IdNavigation = ... (optional navigation assignment)
            };

            await _studentRepository.AddAsync(student);
        }
        private string GenerateHashPassword(User user, string newPassword)
        {
            PasswordHasher<User> passwordHasher = new PasswordHasher<User>();
            string hashedPassword = passwordHasher.HashPassword(user, newPassword);
            return hashedPassword;
        }
    }


}
