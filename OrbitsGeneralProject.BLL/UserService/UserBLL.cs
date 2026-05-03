using AutoMapper;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.Helpers;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.BLL.StudentSubscribeService;
using Orbits.GeneralProject.BLL.Validation.UserValidation;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Enums;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO.StudentSubscribDtos;
using Orbits.GeneralProject.DTO.UserDto;
using Orbits.GeneralProject.DTO.UserDtos;
using Orbits.GeneralProject.Repositroy.Base;
using Orbits.GeneralProject.Repositroy.Enums;
using Microsoft.EntityFrameworkCore;

namespace Orbits.GeneralProject.BLL.UserService
{
    public class UserBLL : BaseBLL, IUserBLL
    {
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Circle> _circleRepository;
        private readonly IRepository<ManagerCircle> _managerCircleRepository;
        private readonly IRepository<ManagerTeacher> _managerTeacherRepository;
        private readonly IRepository<ManagerStudent> _managerStudentRepository;
        private readonly IRepository<Nationality> _nationalityRepository;
        private readonly IRepository<StudentSubscribe> _studentSubscribeRepository;
        private readonly IRepository<Subscribe> _subscribeRepository;
        private readonly IRepository<SubscribeType> _subscribeTypeRepository;
        private readonly IStudentSubscribeBLL _studentSubscribeBLL;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public UserBLL(IMapper mapper, IRepository<User> userrepository,
             IUnitOfWork unitOfWork,
             IHostEnvironment hostEnvironment, IRepository<Nationality> nationalityRepository, IRepository<ManagerCircle> managerCircleRepository, IRepository<Circle> circleRepository, IRepository<ManagerTeacher> managerTeacherRepository, IRepository<ManagerStudent> managerStudentRepository, IRepository<StudentSubscribe> studentSubscribeRepository, IRepository<Subscribe> subscribeRepository, IRepository<SubscribeType> subscribeTypeRepository, IStudentSubscribeBLL studentSubscribeBLL) : base(mapper)
        {
            _userRepository = userrepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _managerCircleRepository = managerCircleRepository;
            _circleRepository = circleRepository;
            _managerTeacherRepository = managerTeacherRepository;
            _managerStudentRepository = managerStudentRepository;
            _nationalityRepository = nationalityRepository;
            _studentSubscribeRepository = studentSubscribeRepository;
            _subscribeRepository = subscribeRepository;
            _subscribeTypeRepository = subscribeTypeRepository;
            _studentSubscribeBLL = studentSubscribeBLL;
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

            var (governorateValid, governorateError) = await ValidateGovernorateRequirementAsync(createUserDto.ResidentId, createUserDto.GovernorateId);
            if (!governorateValid)
                return output.AppendError(MessageCodes.InputValidationError, nameof(DTO.UserDto.CreateUserDto.GovernorateId), governorateError ?? string.Empty);

            // Create User
            var user = _mapper.Map<User>(createUserDto);
            user.RegisterAt = BusinessDateTime.UtcNow;

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

            var loggedInUser = _userRepository.Get(x => x.Id == userid);
            if (loggedInUser == null)
                return output.CreateResponse(MessageCodes.NotFound);
            var requesterType = (UserTypesEnum)(loggedInUser.UserTypeId ?? 0);
            var canAssignStudentManagerWithoutTeacher =
                requesterType == UserTypesEnum.Admin || requesterType == UserTypesEnum.BranchLeader;

            var existedUser = await _userRepository.GetByIdAsync(updateUserDto.Id);
            if (existedUser == null)
                return output.CreateResponse(MessageCodes.NotFound);

            if (!CanUpdateUser(loggedInUser, existedUser))
                return output.CreateResponse(MessageCodes.UnAuthorizedAccess);

            var targetResidentId = updateUserDto.ResidentId ?? existedUser.ResidentId;
            var targetGovernorateId = updateUserDto.GovernorateId ?? existedUser.GovernorateId;
            var (isGovernorateValid, governorateValidationError) = await ValidateGovernorateRequirementAsync(targetResidentId, targetGovernorateId);
            if (!isGovernorateValid)
                return output.AppendError(MessageCodes.InputValidationError, nameof(UpdateUserDto.GovernorateId), governorateValidationError ?? string.Empty);

            var isStudentUser = (UserTypesEnum)(existedUser.UserTypeId ?? 0) == UserTypesEnum.Student;
            var requestedStudentSubscribeId = updateUserDto.StudentSubscribeId;
            var shouldUpdateStudentSubscription = false;
            var isResidenceTriggeredSubscriptionUpdate = false;

            if (isStudentUser)
            {
                var targetSubscribeGroup = await ResolveSubscribeTypeCategoryByResidentIdAsync(targetResidentId);
                var residentChanged = updateUserDto.ResidentId.HasValue && updateUserDto.ResidentId != existedUser.ResidentId;
                var latestStudentSubscription = GetLatestStudentSubscription(existedUser.Id);
                var currentSubscribeGroup = ResolveSubscribeTypeCategory(latestStudentSubscription);

                if (residentChanged && latestStudentSubscription != null && currentSubscribeGroup != targetSubscribeGroup && !requestedStudentSubscribeId.HasValue)
                {
                    return output.AppendError(
                        MessageCodes.InputValidationError,
                        nameof(UpdateUserDto.StudentSubscribeId),
                        "A compatible subscription must be selected before changing the student's residence.");
                }

                if (requestedStudentSubscribeId.HasValue)
                {
                    var subscribeValidationError = ValidateRequestedSubscription(
                        requestedStudentSubscribeId.Value,
                        targetSubscribeGroup);

                    if (!string.IsNullOrWhiteSpace(subscribeValidationError))
                    {
                        return output.AppendError(
                            MessageCodes.InputValidationError,
                            nameof(UpdateUserDto.StudentSubscribeId),
                            subscribeValidationError);
                    }

                    shouldUpdateStudentSubscription = true;
                    isResidenceTriggeredSubscriptionUpdate = residentChanged;
                }
            }

            // 4) Uniqueness: Mobile
            if (!string.IsNullOrWhiteSpace(updateUserDto.Mobile))
            {
                bool isExistedByPhoneNumber = await _userRepository.AnyAsync(
                    x => x.Mobile == updateUserDto.Mobile && x.Id != existedUser.Id
                );
                if (isExistedByPhoneNumber)
                    return output.CreateResponse(MessageCodes.PhoneNumberAlreadyExisted);
            }

            var userType = (UserTypesEnum)(existedUser.UserTypeId ?? 0);
            var isManager = userType == UserTypesEnum.Manager;
            var isTeacher = userType == UserTypesEnum.Teacher;
            var isStudent = userType == UserTypesEnum.Student;
            var previousCircleId = existedUser.CircleId;
            var previousTeacherId = existedUser.TeacherId;

            _mapper.Map(updateUserDto, existedUser);

            if ((isTeacher || isStudent) && updateUserDto.UpdateCircleId != true)
                existedUser.CircleId = previousCircleId;

            if (isStudent && updateUserDto.UpdateTeacherId != true)
                existedUser.TeacherId = previousTeacherId;

            if (isStudent && (updateUserDto.UpdateTeacherId == true || updateUserDto.UpdateCircleId == true))
            {
                if (existedUser.TeacherId.HasValue)
                {
                    var assignedTeacher = await _userRepository.GetByIdAsync(existedUser.TeacherId.Value);
                    if (assignedTeacher == null || assignedTeacher.UserTypeId != (int)UserTypesEnum.Teacher)
                    {
                        return output.AppendError(
                            MessageCodes.InputValidationError,
                            nameof(UpdateUserDto.TeacherId),
                            "The selected teacher is invalid.");
                    }

                    existedUser.CircleId = assignedTeacher.CircleId;
                }
                else
                {
                    existedUser.CircleId = null;
                }
            }

            existedUser.ModefiedAt = BusinessDateTime.UtcNow;
            existedUser.ModefiedBy = userid;
            var studentIds = (updateUserDto.StudentIds ?? new List<int>())
                                .Where(id => id > 0).Distinct().ToHashSet();
            var managerIds = (updateUserDto.ManagerIds ?? new List<int>())
                                .Where(id => id > 0).Distinct().ToHashSet();
            if (isManager)
            {
                var teacherIds = (updateUserDto.TeacherIds ?? new List<int>())
                                 .Where(id => id > 0).Distinct().ToHashSet();
                var usersQuery = _userRepository.GetAll();

                if (updateUserDto.TeacherIds != null)
                {
                    var teacherUserIds = teacherIds.Count == 0
                        ? new HashSet<int>()
                        : usersQuery
                            .Where(u => teacherIds.Contains(u.Id)
                                        && u.UserTypeId == (int)UserTypesEnum.Teacher)
                            .Select(u => u.Id)
                            .ToHashSet();

                    var currentTeacherLinks = _managerTeacherRepository.GetAll()
                        .Where(mt => mt.ManagerId == existedUser.Id && mt.TeacherId.HasValue)
                        .ToList();

                    var currentTeacherIds = currentTeacherLinks
                        .Where(mt => mt.TeacherId.HasValue)
                        .Select(mt => mt.TeacherId!.Value)
                        .ToHashSet();

                    var linksToDelete = currentTeacherLinks
                        .Where(mt => !teacherUserIds.Contains(mt.TeacherId!.Value))
                        .ToList();

                    foreach (var link in linksToDelete)
                        _managerTeacherRepository.Delete(link);

                    var teacherIdsToAdd = teacherUserIds.Where(id => !currentTeacherIds.Contains(id));
                    foreach (var teacherId in teacherIdsToAdd)
                    {
                        _managerTeacherRepository.Add(new ManagerTeacher
                        {
                            ManagerId = existedUser.Id,
                            TeacherId = teacherId,
                            CreatedBy = userid,
                            CreatedAt = BusinessDateTime.UtcNow,
                            ModefiedBy = userid,
                            ModefiedAt = BusinessDateTime.UtcNow
                        });
                    }
                }

                if (updateUserDto.StudentIds != null)
                {
                    var currentStudents = (from ms in _managerStudentRepository.GetAll()
                                           join u in usersQuery on ms.StudentId equals u.Id
                                           where ms.ManagerId == existedUser.Id
                                                 && u.UserTypeId == (int)UserTypesEnum.Student
                                           select u)
                        .ToList();

                    var studentsToAssign = studentIds.Count == 0
                        ? new List<User>()
                        : usersQuery
                            .Where(u => studentIds.Contains(u.Id)
                                        && u.UserTypeId == (int)UserTypesEnum.Student)
                            .ToList();

                    foreach (var s in studentsToAssign)
                    {
                        if (_managerStudentRepository.GetAll().Any(ms => ms.ManagerId == existedUser.Id && ms.StudentId == s.Id))
                            continue;

                        _managerStudentRepository.Add(new ManagerStudent
                        {
                            ManagerId = existedUser.Id,
                            StudentId = s.Id,
                            CreatedBy = userid,
                            CreatedAt = BusinessDateTime.UtcNow,
                            ModefiedAt = BusinessDateTime.UtcNow,
                            ModefiedBy = userid
                        });
                        s.ModefiedAt = BusinessDateTime.UtcNow;
                        s.ModefiedBy = userid;
                        _userRepository.Update(s);
                    }

                    var studentsToDetach = currentStudents.Where(s => !studentIds.Contains(s.Id)).ToList();
                    foreach (var s in studentsToDetach)
                    {
                        var links = _managerStudentRepository.GetAll()
                            .Where(ms => ms.ManagerId == existedUser.Id && ms.StudentId == s.Id)
                            .ToList();
                        foreach (var link in links)
                            _managerStudentRepository.Delete(link);
                        s.ModefiedAt = BusinessDateTime.UtcNow;
                        s.ModefiedBy = userid;
                        _userRepository.Update(s);
                    }
                }
            }
            if (isTeacher)
            {
                var usersQuery = _userRepository.GetAll();

                if (updateUserDto.UpdateCircleId == true && existedUser.CircleId.HasValue)
                {
                    var requestedCircleId = existedUser.CircleId.Value;

                    var teachersWithSameCircle = usersQuery
                        .Where(u => u.Id != existedUser.Id
                                    && u.UserTypeId == (int)UserTypesEnum.Teacher
                                    && u.CircleId == requestedCircleId)
                        .ToList();

                    foreach (var teacherWithSameCircle in teachersWithSameCircle)
                    {
                        teacherWithSameCircle.CircleId = null;
                        teacherWithSameCircle.ModefiedAt = BusinessDateTime.UtcNow;
                        teacherWithSameCircle.ModefiedBy = userid;
                        _userRepository.Update(teacherWithSameCircle);

                        var linkedStudents = usersQuery
                            .Where(u => u.UserTypeId == (int)UserTypesEnum.Student
                                        && u.TeacherId == teacherWithSameCircle.Id)
                            .ToList();

                        foreach (var linkedStudent in linkedStudents)
                        {
                            linkedStudent.CircleId = null;
                            linkedStudent.ModefiedAt = BusinessDateTime.UtcNow;
                            linkedStudent.ModefiedBy = userid;
                            _userRepository.Update(linkedStudent);
                        }
                    }
                }

                if (updateUserDto.StudentIds != null)
                {
                    var currentStudents = usersQuery
                        .Where(u => u.TeacherId == existedUser.Id
                                    && u.UserTypeId == (int)UserTypesEnum.Student)
                        .ToList();

                    var studentsToAssign = studentIds.Count == 0
                        ? new List<User>()
                        : usersQuery
                            .Where(u => studentIds.Contains(u.Id)
                                        && u.UserTypeId == (int)UserTypesEnum.Student)
                            .ToList();

                    foreach (var t in studentsToAssign)
                    {
                        t.TeacherId = existedUser.Id;
                        t.CircleId = existedUser.CircleId;
                        t.ModefiedAt = BusinessDateTime.UtcNow;
                        t.ModefiedBy = userid;
                        _userRepository.Update(t);
                    }

                    var studentsToDetach = currentStudents.Where(t => !studentIds.Contains(t.Id)).ToList();
                    foreach (var t in studentsToDetach)
                    {
                        t.TeacherId = null;
                        t.CircleId = null;
                        t.ModefiedAt = BusinessDateTime.UtcNow;
                        t.ModefiedBy = userid;
                        _userRepository.Update(t);
                    }
                }

                if (updateUserDto.UpdateCircleId == true && previousCircleId.HasValue && previousCircleId != existedUser.CircleId)
                {
                    var previousCircle = _circleRepository.GetById(previousCircleId.Value);
                    if (previousCircle != null && previousCircle.TeacherId == existedUser.Id)
                    {
                        previousCircle.TeacherId = null;
                        previousCircle.ModefiedAt = BusinessDateTime.UtcNow;
                        previousCircle.ModefiedBy = userid;
                        _circleRepository.Update(previousCircle);
                    }
                }

                if (updateUserDto.UpdateCircleId == true && existedUser.CircleId.HasValue)
                {
                    var updatedCircle = _circleRepository.GetById(existedUser.CircleId.Value);
                    if (updatedCircle != null)
                    {
                        updatedCircle.TeacherId = existedUser.Id;
                        updatedCircle.ModefiedAt = BusinessDateTime.UtcNow;
                        updatedCircle.ModefiedBy = userid;
                        _circleRepository.Update(updatedCircle);
                    }
                }

                if (updateUserDto.UpdateCircleId == true)
                {
                    var linkedStudents = usersQuery
                        .Where(u => u.TeacherId == existedUser.Id
                                    && u.UserTypeId == (int)UserTypesEnum.Student)
                        .ToList();

                    foreach (var linkedStudent in linkedStudents)
                    {
                        linkedStudent.CircleId = existedUser.CircleId;
                        linkedStudent.ModefiedAt = BusinessDateTime.UtcNow;
                        linkedStudent.ModefiedBy = userid;
                        _userRepository.Update(linkedStudent);
                    }
                }

                if (updateUserDto.ManagerIds != null)
                {
                    var currentManagerLinks = _managerTeacherRepository.GetAll()
                        .Where(mt => mt.TeacherId == existedUser.Id && mt.ManagerId.HasValue)
                        .ToList();

                    var currentManagerIds = currentManagerLinks
                        .Where(mt => mt.ManagerId.HasValue)
                        .Select(mt => mt.ManagerId!.Value)
                        .ToHashSet();

                    var linksToDelete = currentManagerLinks
                        .Where(mt => !managerIds.Contains(mt.ManagerId!.Value))
                        .ToList();

                    foreach (var link in linksToDelete)
                        _managerTeacherRepository.Delete(link);

                    if (managerIds.Count > 0)
                    {
                        var managerUserIds = usersQuery
                            .Where(u => managerIds.Contains(u.Id)
                                        && u.UserTypeId == (int)UserTypesEnum.Manager)
                            .Select(u => u.Id)
                            .ToHashSet();

                        var managerIdsToAdd = managerUserIds.Where(id => !currentManagerIds.Contains(id));
                        foreach (var managerId in managerIdsToAdd)
                        {
                            _managerTeacherRepository.Add(new ManagerTeacher
                            {
                                ManagerId = managerId,
                                TeacherId = existedUser.Id,
                                CreatedBy = userid,
                                CreatedAt = BusinessDateTime.UtcNow,
                                ModefiedBy = userid,
                                ModefiedAt = BusinessDateTime.UtcNow
                            });
                        }
                    }
                }
            }

            if (isStudent)
            {
                var usersQuery = _userRepository.GetAll();
                var currentManagerLinks = _managerStudentRepository.GetAll()
                    .Where(ms => ms.StudentId == existedUser.Id && ms.ManagerId.HasValue)
                    .ToList();

                var currentManagerIds = currentManagerLinks
                    .Select(ms => ms.ManagerId!.Value)
                    .ToHashSet();

                if (!canAssignStudentManagerWithoutTeacher &&
                    (updateUserDto.ManagerIds != null || updateUserDto.UpdateTeacherId == true))
                {
                    var effectiveManagerIds = updateUserDto.ManagerIds != null ? managerIds : currentManagerIds;
                    if (effectiveManagerIds.Count > 0 && !existedUser.TeacherId.HasValue)
                    {
                        return output.AppendError(
                            MessageCodes.InputValidationError,
                            nameof(UpdateUserDto.TeacherId),
                            "اختيار المعلم مطلوب عند إسناد مشرف للطالب.");
                    }
                }

                if (updateUserDto.ManagerIds != null)
                {
                    var linksToDelete = currentManagerLinks
                        .Where(ms => !managerIds.Contains(ms.ManagerId!.Value))
                        .ToList();

                    foreach (var link in linksToDelete)
                        _managerStudentRepository.Delete(link);

                    if (managerIds.Count > 0)
                    {
                        var validManagerIds = usersQuery
                            .Where(u => managerIds.Contains(u.Id) && u.UserTypeId == (int)UserTypesEnum.Manager)
                            .Select(u => u.Id)
                            .ToHashSet();

                        var managerIdsToAdd = validManagerIds.Where(id => !currentManagerIds.Contains(id));
                        foreach (var managerId in managerIdsToAdd)
                        {
                            _managerStudentRepository.Add(new ManagerStudent
                            {
                                ManagerId = managerId,
                                StudentId = existedUser.Id,
                                CreatedBy = userid,
                                CreatedAt = BusinessDateTime.UtcNow,
                                ModefiedBy = userid,
                                ModefiedAt = BusinessDateTime.UtcNow
                            });
                        }
                    }
                }
            }

            if (isManager)
            {
                if (updateUserDto.CircleIds != null)
                {
                    var targetCircleIds = updateUserDto.CircleIds
                        .Where(id => id > 0)
                        .Distinct()
                        .ToList();

                    var targetSet = targetCircleIds.ToHashSet();
                    var currentLinks = _managerCircleRepository.GetAll()
                        .Where(mc => mc.ManagerId == existedUser.Id && mc.CircleId.HasValue)
                        .ToList();

                    var currentCircleIds = currentLinks
                        .Select(mc => mc.CircleId!.Value)
                        .ToHashSet();

                    var linksToDelete = currentLinks
                        .Where(mc => !targetSet.Contains(mc.CircleId!.Value))
                        .ToList();

                    foreach (var link in linksToDelete)
                    {
                        _managerCircleRepository.Delete(link);
                    }

                    var validTargetCircleIds = targetCircleIds.Count == 0
                        ? new HashSet<int>()
                        : _circleRepository.GetAll()
                            .Where(c => targetSet.Contains(c.Id) && c.IsDeleted != true)
                            .Select(c => c.Id)
                            .ToHashSet();

                    var circleIdsToAdd = validTargetCircleIds
                        .Where(id => !currentCircleIds.Contains(id))
                        .ToList();

                    foreach (var circleId in circleIdsToAdd)
                    {
                        _managerCircleRepository.Add(new ManagerCircle
                        {
                            ManagerId = existedUser.Id,
                            CircleId = circleId,
                            ModefiedAt = BusinessDateTime.UtcNow,
                            ModefiedBy = userid
                        });
                    }
                }
            }
           
            // 7) Update main user
            _userRepository.Update(existedUser);

            if (shouldUpdateStudentSubscription && requestedStudentSubscribeId.HasValue)
            {
                var studentSubscribeResponse = await _studentSubscribeBLL.AddAsync(
                    new AddStudentSubscribeDto
                    {
                        StudentId = existedUser.Id,
                        StudentSubscribeId = requestedStudentSubscribeId.Value,
                        ActionType = isResidenceTriggeredSubscriptionUpdate ? "ResidenceChanged" : null
                    },
                    userid);

                if (!studentSubscribeResponse.IsSuccess)
                    return output.AppendErrors(studentSubscribeResponse.Errors);
            }

            // 8) Commit once
            await _unitOfWork.CommitAsync();

            return output.CreateResponse(true);
        }

        private bool CanUpdateUser(User requester, User target)
        {
            var requesterType = (UserTypesEnum)(requester.UserTypeId ?? 0);
            var targetType = (UserTypesEnum)(target.UserTypeId ?? 0);

            if (requesterType == UserTypesEnum.Admin)
                return true;

            if (requester.Id == target.Id)
                return true;

            if (GetUserRank(requesterType) <= GetUserRank(targetType))
                return false;

            return requesterType switch
            {
                UserTypesEnum.BranchLeader => requester.BranchId.HasValue && requester.BranchId == target.BranchId,
                UserTypesEnum.Manager => targetType switch
                {
                    UserTypesEnum.Teacher => _managerTeacherRepository.GetAll()
                        .Any(mt => mt.ManagerId == requester.Id && mt.TeacherId == target.Id),
                    UserTypesEnum.Student => _managerStudentRepository.GetAll()
                        .Any(ms => ms.ManagerId == requester.Id && ms.StudentId == target.Id),
                    _ => false
                },
                UserTypesEnum.Teacher => targetType == UserTypesEnum.Student && target.TeacherId == requester.Id,
                _ => false
            };
        }

        private static int GetUserRank(UserTypesEnum userType)
        {
            return userType switch
            {
                UserTypesEnum.Admin => 100,
                UserTypesEnum.BranchLeader => 80,
                UserTypesEnum.Manager => 60,
                UserTypesEnum.Teacher => 40,
                UserTypesEnum.Student => 20,
                _ => 0
            };
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

            var targetResidentId = updateProfileDto.ResidentId ?? user.ResidentId;
            var targetGovernorateId = updateProfileDto.GovernorateId ?? user.GovernorateId;
            var (profileGovernorateValid, profileGovernorateError) = await ValidateGovernorateRequirementAsync(targetResidentId, targetGovernorateId);
            if (!profileGovernorateValid)
                return output.AppendError(MessageCodes.InputValidationError, nameof(UpdateProfileDto.GovernorateId), profileGovernorateError ?? string.Empty);

            if (updateProfileDto.SecondMobile != null)
                user.SecondMobile = string.IsNullOrWhiteSpace(updateProfileDto.SecondMobile)
                    ? null
                    : updateProfileDto.SecondMobile.Trim();

            if (updateProfileDto.ResidentId.HasValue)
                user.ResidentId = updateProfileDto.ResidentId;

            if (updateProfileDto.NationalityId.HasValue)
                user.NationalityId = updateProfileDto.NationalityId;

            if (updateProfileDto.GovernorateId.HasValue)
                user.GovernorateId = updateProfileDto.GovernorateId;

            if (updateProfileDto.BranchId.HasValue)
                user.BranchId = updateProfileDto.BranchId;

            user.ModefiedAt = BusinessDateTime.UtcNow;
            user.ModefiedBy = userId;

            _userRepository.Update(user);
            await _unitOfWork.CommitAsync();

            return output.CreateResponse(true);
        }



        public async Task<IResponse<bool>> Restore(int id, int userId)
        {
            Response<bool> output = new Response<bool>();

            User entity = await _userRepository
                .DisableFilter(nameof(DynamicFilters.IsDeleted))
                .FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
                return output.AppendError(MessageCodes.NotFound, nameof(id), "Entity not found");

            var allowedTypes = new[]
            {
                (int)UserTypesEnum.Student,
                (int)UserTypesEnum.Teacher,
                (int)UserTypesEnum.Manager,
                (int)UserTypesEnum.BranchLeader
            };

            if (!entity.UserTypeId.HasValue || !allowedTypes.Contains(entity.UserTypeId.Value))
                return output.AppendError(MessageCodes.NotFound, nameof(id), "Entity not found");

            if (!entity.IsDeleted)
                return output.CreateResponse(true);

            entity.IsDeleted = false;
            entity.Inactive = false;
            entity.ModefiedBy = userId;
            entity.ModefiedAt = BusinessDateTime.UtcNow;

            _userRepository.Update(entity);
            await _unitOfWork.CommitAsync();

            return output.CreateResponse(true);
        }

        public async Task<IResponse<bool>> DisableUser(int id ,bool statue)
        {
            Response<bool> output = new Response<bool>();
            User entity =  _userRepository.GetById(id);
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

        public async Task<IResponse<BulkDisableUsersResultDto>> DisableUsersBulk(BulkDisableUsersDto model, int userId)
        {
            var output = new Response<BulkDisableUsersResultDto>();
            var requestedIds = (model?.UserIds ?? new List<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (requestedIds.Count == 0)
            {
                return output.AppendError(
                    MessageCodes.InputValidationError,
                    nameof(BulkDisableUsersDto.UserIds),
                    "At least one user must be selected.");
            }

            var allowedTypes = new[]
            {
                (int)UserTypesEnum.Student,
                (int)UserTypesEnum.Teacher,
                (int)UserTypesEnum.Manager,
                (int)UserTypesEnum.BranchLeader
            };

            var entities = await _userRepository
                .DisableFilter(nameof(DynamicFilters.IsDeleted))
                .Where(x => requestedIds.Contains(x.Id))
                .ToListAsync();

            var nowUtc = BusinessDateTime.UtcNow;
            var result = new BulkDisableUsersResultDto
            {
                RequestedCount = requestedIds.Count
            };

            foreach (var entity in entities)
            {
                if (!entity.UserTypeId.HasValue || !allowedTypes.Contains(entity.UserTypeId.Value))
                {
                    result.SkippedUserIds.Add(entity.Id);
                    continue;
                }

                if (entity.IsDeleted && entity.Inactive)
                {
                    result.SkippedUserIds.Add(entity.Id);
                    continue;
                }

                entity.Inactive = true;
                entity.IsDeleted = true;
                entity.ModefiedBy = userId;
                entity.ModefiedAt = nowUtc;

                _userRepository.Update(entity);
                result.DisabledUserIds.Add(entity.Id);
            }

            var foundIds = entities.Select(x => x.Id).ToHashSet();
            result.SkippedUserIds.AddRange(requestedIds.Where(id => !foundIds.Contains(id)));
            result.SkippedUserIds = result.SkippedUserIds.Distinct().ToList();
            result.DisabledUserIds = result.DisabledUserIds.Distinct().ToList();
            result.DisabledCount = result.DisabledUserIds.Count;

            if (result.DisabledCount > 0)
            {
                await _unitOfWork.CommitAsync();
            }

            return output.CreateResponse(result);
        }
        // ------------------ Helpers ------------------


        private string GenerateHashPassword(User user, string newPassword)
        {
            PasswordHasher<User> passwordHasher = new PasswordHasher<User>();
            string hashedPassword = passwordHasher.HashPassword(user, newPassword);
            return hashedPassword;
        }

        private async Task<(bool IsValid, string? ErrorMessage)> ValidateGovernorateRequirementAsync(int? residentId, int? governorateId)
        {
            if (!residentId.HasValue)
                return (true, null);

            var resident = await _nationalityRepository.GetByIdAsync(residentId.Value);
            if (!IsEgyptianNationality(resident))
                return (true, null);

            if (governorateId.HasValue && governorateId.Value > 0)
                return (true, null);

            return (false, UserValidationReponseConstants.GovernorateRequiredForEgyptian);
        }

        private StudentSubscribe? GetLatestStudentSubscription(int userId)
        {
            return _studentSubscribeRepository
                .Where(x => x.StudentId == userId)
                .OrderByDescending(x => x.ModefiedAt ?? x.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();
        }

        private async Task<SubscribeTypeCategory?> ResolveSubscribeTypeCategoryByResidentIdAsync(int? residentId)
        {
            if (!residentId.HasValue || residentId.Value <= 0)
                return null;

            var resident = await _nationalityRepository.GetByIdAsync(residentId.Value);
            return ResolveSubscribeTypeCategory(resident);
        }

        private SubscribeTypeCategory? ResolveSubscribeTypeCategory(StudentSubscribe? studentSubscribe)
        {
            if (studentSubscribe?.StudentSubscribeTypeId.HasValue != true)
                return null;

            if (studentSubscribe.StudentSubscribeType?.Group.HasValue == true)
                return (SubscribeTypeCategory?)studentSubscribe.StudentSubscribeType.Group.Value;

            var subscribeType = _subscribeTypeRepository.GetById(studentSubscribe.StudentSubscribeTypeId.Value);
            return subscribeType?.Group.HasValue == true
                ? (SubscribeTypeCategory?)subscribeType.Group.Value
                : null;
        }

        private SubscribeTypeCategory? ResolveSubscribeTypeCategory(Nationality? nationality)
        {
            var subscribeFor = NationalityClassificationHelper.ResolveSubscribeFor(nationality);
            return subscribeFor switch
            {
                SubscribeForEnum.Egyptian => SubscribeTypeCategory.Egyptian,
                SubscribeForEnum.Gulf => SubscribeTypeCategory.Arab,
                SubscribeForEnum.NonArab => SubscribeTypeCategory.Foreign,
                _ => null
            };
        }

        private string? ValidateRequestedSubscription(int subscribeId, SubscribeTypeCategory? targetGroup)
        {
            if (!targetGroup.HasValue)
                return "The student's target residency group could not be resolved.";

            var subscribe = _subscribeRepository.GetById(subscribeId);
            if (subscribe == null)
                return "The selected subscription plan was not found.";

            var subscribeGroup = ResolveSubscribeTypeCategory(subscribe);
            if (!subscribeGroup.HasValue)
                return "The selected subscription plan does not have a configured residency group.";

            if (subscribeGroup.Value != targetGroup.Value)
                return "The selected subscription plan does not match the student's new residence.";

            return null;
        }

        private SubscribeTypeCategory? ResolveSubscribeTypeCategory(Subscribe? subscribe)
        {
            if (subscribe == null)
                return null;

            if (subscribe.SubscribeType?.Group.HasValue == true)
                return (SubscribeTypeCategory?)subscribe.SubscribeType.Group.Value;

            if (!subscribe.SubscribeTypeId.HasValue)
                return null;

            var subscribeType = _subscribeTypeRepository.GetById(subscribe.SubscribeTypeId.Value);
            return subscribeType?.Group.HasValue == true
                ? (SubscribeTypeCategory?)subscribeType.Group.Value
                : null;
        }

        private static bool IsEgyptianNationality(Nationality? nationality)
            => NationalityClassificationHelper.IsEgyptian(nationality);
    }


}
