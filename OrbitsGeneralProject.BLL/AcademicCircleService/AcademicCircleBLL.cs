using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.Helpers;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.BLL.Validation.AcademicCircleValidation;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Enums;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO.AcademicCircleDtos;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.Repositroy.Base;

namespace Orbits.GeneralProject.BLL.AcademicCircleService
{
    public class AcademicCircleBLL : BaseBLL, IAcademicCircleBLL
    {
        private const int AcademicSchoolSystemTypeId = (int)EducationSystemType.AcademicSchool;
        private const int BothSystemsTypeId = (int)EducationSystemType.Both;

        private readonly IRepository<AcademicCircle> _academicCircleRepository;
        private readonly IRepository<AcademicCircleStudent> _academicCircleStudentRepository;
        private readonly IRepository<AcademicManagerCircle> _academicManagerCircleRepository;
        private readonly IRepository<AcademicManagerTeacher> _academicManagerTeacherRepository;
        private readonly IRepository<AcademicManagerStudent> _academicManagerStudentRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AcademicCircleBLL(
            IMapper mapper,
            IRepository<AcademicCircle> academicCircleRepository,
            IRepository<AcademicCircleStudent> academicCircleStudentRepository,
            IRepository<AcademicManagerCircle> academicManagerCircleRepository,
            IRepository<AcademicManagerTeacher> academicManagerTeacherRepository,
            IRepository<AcademicManagerStudent> academicManagerStudentRepository,
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork) : base(mapper)
        {
            _academicCircleRepository = academicCircleRepository;
            _academicCircleStudentRepository = academicCircleStudentRepository;
            _academicManagerCircleRepository = academicManagerCircleRepository;
            _academicManagerTeacherRepository = academicManagerTeacherRepository;
            _academicManagerStudentRepository = academicManagerStudentRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IResponse<AcademicCircleDto>> GetByIdAsync(int id, int userId)
        {
            var output = new Response<AcademicCircleDto>();
            var circle = await BuildScopedCirclesQuery(userId)
                .Include(x => x.Teacher)
                .Include(x => x.AcademicManagerCircles).ThenInclude(x => x.Manager)
                .Include(x => x.AcademicCircleStudents).ThenInclude(x => x.Student)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (circle == null)
            {
                return output.AppendError(MessageCodes.NotFound);
            }

            return output.CreateResponse(MapCircle(circle));
        }

        public IResponse<PagedResultDto<AcademicCircleDto>> GetPagedList(
            FilteredResultRequestDto pagedDto,
            int? managerId,
            int? teacherId,
            int userId)
        {
            var output = new Response<PagedResultDto<AcademicCircleDto>>();
            var searchTerm = pagedDto.SearchTerm?.Trim().ToLower();

            var query = BuildScopedCirclesQuery(userId)
                .Include(x => x.Teacher)
                .Include(x => x.AcademicManagerCircles).ThenInclude(x => x.Manager)
                .Include(x => x.AcademicCircleStudents).ThenInclude(x => x.Student)
                .AsQueryable();

            if (managerId.HasValue && managerId.Value > 0)
            {
                query = query.Where(x => x.AcademicManagerCircles.Any(mc => mc.ManagerId == managerId.Value));
            }

            if (teacherId.HasValue && teacherId.Value > 0)
            {
                query = query.Where(x => x.TeacherId == teacherId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(x =>
                    (x.Name != null && x.Name.ToLower().Contains(searchTerm))
                    || (x.Teacher != null && x.Teacher.FullName != null && x.Teacher.FullName.ToLower().Contains(searchTerm))
                    || x.AcademicManagerCircles.Any(mc => mc.Manager != null && mc.Manager.FullName != null && mc.Manager.FullName.ToLower().Contains(searchTerm))
                    || x.AcademicCircleStudents.Any(cs => cs.Student != null && cs.Student.FullName != null && cs.Student.FullName.ToLower().Contains(searchTerm)));
            }

            var totalCount = query.Count();
            var circles = query
                .OrderByDescending(x => x.CreatedAt ?? DateTime.MinValue)
                .Skip(pagedDto.SkipCount)
                .Take(pagedDto.MaxResultCount)
                .ToList();

            var result = new PagedResultDto<AcademicCircleDto>(totalCount, circles.Select(MapCircle).ToList());
            return output.CreateResponse(result);
        }

        public async Task<IResponse<bool>> AddAsync(CreateAcademicCircleDto model, int userId)
        {
            var output = new Response<bool>();
            var validationResult = new AcademicCircleValidation().Validate(model);
            if (!validationResult.IsValid)
            {
                return output.AppendErrors(validationResult.Errors);
            }

            var currentUser = _userRepository.GetById(userId);
            if (currentUser == null)
            {
                return output.AppendError(MessageCodes.NotFound);
            }

            if (!EducationSystemTypeHelper.CanAccessAcademicModule(currentUser.UserTypeId, currentUser.EducationSystemTypeId))
            {
                return output.AppendError(MessageCodes.BusinessValidationError, "المستخدم غير مفعل لنظام المواد الدراسية.");
            }

            var currentUserType = (UserTypesEnum)(currentUser.UserTypeId ?? 0);
            if (currentUserType == UserTypesEnum.Teacher || currentUserType == UserTypesEnum.Student)
            {
                return output.AppendError(MessageCodes.BusinessValidationError, "غير مسموح لك بإضافة حلقات المواد.");
            }

            var normalizedManagerIds = NormalizeIds(model.ManagerIds);
            var normalizedStudentIds = NormalizeIds(model.StudentIds);

            if (currentUserType == UserTypesEnum.Manager)
            {
                normalizedManagerIds = new List<int> { currentUser.Id };
            }

            var teacher = await ValidateTeacherAsync(model.TeacherId, currentUser);
            if (teacher == null)
            {
                return output.AppendError(MessageCodes.BusinessValidationError, nameof(model.TeacherId), "المعلم المختار غير صحيح.");
            }

            var managersValidation = await ValidateUsersByTypeAsync(normalizedManagerIds, UserTypesEnum.Manager, currentUser);
            if (!managersValidation.IsValid)
            {
                return output.AppendError(MessageCodes.BusinessValidationError, nameof(model.ManagerIds), managersValidation.Message!);
            }

            var studentsValidation = await ValidateUsersByTypeAsync(normalizedStudentIds, UserTypesEnum.Student, currentUser);
            if (!studentsValidation.IsValid)
            {
                return output.AppendError(MessageCodes.BusinessValidationError, nameof(model.StudentIds), studentsValidation.Message!);
            }

            var entity = new AcademicCircle
            {
                Name = model.Name?.Trim(),
                TeacherId = teacher.Id,
                BranchId = ResolveBranchId(model.BranchId, teacher.BranchId, currentUser.BranchId),
                CreatedAt = BusinessDateTime.UtcNow,
                CreatedBy = userId,
                IsDeleted = false
            };

            await _academicCircleRepository.AddAsync(entity);
            await _unitOfWork.CommitAsync();

            await SyncCircleRelationsAsync(entity.Id, normalizedManagerIds, normalizedStudentIds, userId);
            await RebuildDerivedRelationsAsync(userId);
            await _unitOfWork.CommitAsync();

            return output.CreateResponse(true);
        }

        public async Task<IResponse<bool>> UpdateAsync(UpdateAcademicCircleDto model, int userId)
        {
            var output = new Response<bool>();
            var validationResult = new AcademicCircleValidation().Validate(model);
            if (!validationResult.IsValid)
            {
                return output.AppendErrors(validationResult.Errors);
            }

            var circle = await BuildScopedCirclesQuery(userId).FirstOrDefaultAsync(x => x.Id == model.Id);
            if (circle == null)
            {
                return output.AppendError(MessageCodes.NotFound);
            }

            var currentUser = _userRepository.GetById(userId);
            if (currentUser == null)
            {
                return output.AppendError(MessageCodes.NotFound);
            }

            if (!EducationSystemTypeHelper.CanAccessAcademicModule(currentUser.UserTypeId, currentUser.EducationSystemTypeId))
            {
                return output.AppendError(MessageCodes.BusinessValidationError, "المستخدم غير مفعل لنظام المواد الدراسية.");
            }

            var currentUserType = (UserTypesEnum)(currentUser.UserTypeId ?? 0);
            if (currentUserType == UserTypesEnum.Teacher || currentUserType == UserTypesEnum.Student)
            {
                return output.AppendError(MessageCodes.BusinessValidationError, "غير مسموح لك بتعديل حلقات المواد.");
            }

            var normalizedManagerIds = NormalizeIds(model.ManagerIds);
            var normalizedStudentIds = NormalizeIds(model.StudentIds);

            if (currentUserType == UserTypesEnum.Manager)
            {
                normalizedManagerIds = new List<int> { currentUser.Id };
            }

            var teacher = await ValidateTeacherAsync(model.TeacherId, currentUser);
            if (teacher == null)
            {
                return output.AppendError(MessageCodes.BusinessValidationError, nameof(model.TeacherId), "المعلم المختار غير صحيح.");
            }

            var managersValidation = await ValidateUsersByTypeAsync(normalizedManagerIds, UserTypesEnum.Manager, currentUser);
            if (!managersValidation.IsValid)
            {
                return output.AppendError(MessageCodes.BusinessValidationError, nameof(model.ManagerIds), managersValidation.Message!);
            }

            var studentsValidation = await ValidateUsersByTypeAsync(normalizedStudentIds, UserTypesEnum.Student, currentUser);
            if (!studentsValidation.IsValid)
            {
                return output.AppendError(MessageCodes.BusinessValidationError, nameof(model.StudentIds), studentsValidation.Message!);
            }

            circle.Name = model.Name?.Trim();
            circle.TeacherId = teacher.Id;
            circle.BranchId = ResolveBranchId(model.BranchId, teacher.BranchId, currentUser.BranchId);
            circle.ModefiedAt = BusinessDateTime.UtcNow;
            circle.ModefiedBy = userId;

            await SyncCircleRelationsAsync(circle.Id, normalizedManagerIds, normalizedStudentIds, userId);
            await RebuildDerivedRelationsAsync(userId);
            await _unitOfWork.CommitAsync();

            return output.CreateResponse(true);
        }

        public async Task<IResponse<bool>> DeleteAsync(int id, int userId)
        {
            var output = new Response<bool>();
            var circle = await BuildScopedCirclesQuery(userId).FirstOrDefaultAsync(x => x.Id == id);
            if (circle == null)
            {
                return output.AppendError(MessageCodes.NotFound);
            }

            var currentUser = _userRepository.GetById(userId);
            var currentUserType = (UserTypesEnum)(currentUser?.UserTypeId ?? 0);
            if (currentUserType == UserTypesEnum.Teacher || currentUserType == UserTypesEnum.Student)
            {
                return output.AppendError(MessageCodes.BusinessValidationError, "غير مسموح لك بحذف حلقات المواد.");
            }

            circle.IsDeleted = true;
            circle.ModefiedAt = BusinessDateTime.UtcNow;
            circle.ModefiedBy = userId;

            var managerLinks = _academicManagerCircleRepository.Where(x => x.AcademicCircleId == id).ToList();
            var studentLinks = _academicCircleStudentRepository.Where(x => x.AcademicCircleId == id).ToList();
            _academicManagerCircleRepository.DeleteRange(managerLinks);
            _academicCircleStudentRepository.DeleteRange(studentLinks);

            await RebuildDerivedRelationsAsync(userId);
            await _unitOfWork.CommitAsync();

            return output.CreateResponse(true);
        }

        private IQueryable<AcademicCircle> BuildScopedCirclesQuery(int userId)
        {
            var me = _userRepository
                .Where(x => x.Id == userId)
                .Select(x => new { x.Id, x.UserTypeId, x.BranchId, x.EducationSystemTypeId })
                .FirstOrDefault();

            if (me == null || !EducationSystemTypeHelper.CanAccessAcademicModule(me.UserTypeId, me.EducationSystemTypeId))
            {
                return _academicCircleRepository.Where(x => false);
            }

            var userType = (UserTypesEnum)(me.UserTypeId ?? 0);
            var managerCircleQuery = _academicManagerCircleRepository.GetAll();
            var circleStudentQuery = _academicCircleStudentRepository.GetAll();

            return _academicCircleRepository.Where(x =>
                userType == UserTypesEnum.Admin
                || (userType == UserTypesEnum.BranchLeader && me.BranchId.HasValue && x.BranchId == me.BranchId)
                || (userType == UserTypesEnum.Manager && managerCircleQuery.Any(mc => mc.ManagerId == me.Id && mc.AcademicCircleId == x.Id))
                || (userType == UserTypesEnum.Teacher && x.TeacherId == me.Id)
                || (userType == UserTypesEnum.Student && circleStudentQuery.Any(cs => cs.StudentId == me.Id && cs.AcademicCircleId == x.Id)));
        }

        private AcademicCircleDto MapCircle(AcademicCircle circle)
        {
            return new AcademicCircleDto
            {
                Id = circle.Id,
                Name = circle.Name,
                TeacherId = circle.TeacherId,
                TeacherName = circle.Teacher?.FullName,
                BranchId = circle.BranchId,
                ManagerIds = circle.AcademicManagerCircles
                    .Where(x => x.ManagerId.HasValue)
                    .Select(x => x.ManagerId!.Value)
                    .Distinct()
                    .ToList(),
                ManagerNames = circle.AcademicManagerCircles
                    .Where(x => x.Manager != null && !string.IsNullOrWhiteSpace(x.Manager.FullName))
                    .Select(x => x.Manager!.FullName!)
                    .Distinct()
                    .ToList(),
                StudentIds = circle.AcademicCircleStudents
                    .Where(x => x.StudentId.HasValue)
                    .Select(x => x.StudentId!.Value)
                    .Distinct()
                    .ToList(),
                StudentNames = circle.AcademicCircleStudents
                    .Where(x => x.Student != null && !string.IsNullOrWhiteSpace(x.Student.FullName))
                    .Select(x => x.Student!.FullName!)
                    .Distinct()
                    .ToList(),
                StudentCount = circle.AcademicCircleStudents.Count
            };
        }

        private static List<int> NormalizeIds(IEnumerable<int>? ids)
        {
            return ids?
                .Where(x => x > 0)
                .Distinct()
                .ToList() ?? new List<int>();
        }

        private async Task<(bool IsValid, string? Message)> ValidateUsersByTypeAsync(
            IEnumerable<int> ids,
            UserTypesEnum userType,
            User currentUser)
        {
            var normalizedIds = ids.Distinct().ToList();
            if (!normalizedIds.Any())
            {
                return (false, $"يجب اختيار {ResolveLabel(userType)} واحد على الأقل.");
            }

            var users = _userRepository
                .Where(x =>
                    normalizedIds.Contains(x.Id)
                    && x.UserTypeId == (int)userType
                    && (x.EducationSystemTypeId == AcademicSchoolSystemTypeId || x.EducationSystemTypeId == BothSystemsTypeId))
                .ToList();

            if (users.Count != normalizedIds.Count)
            {
                return (false, $"بعض عناصر {ResolveLabel(userType)} المختارة غير صحيحة.");
            }

            if (currentUser.UserTypeId == (int)UserTypesEnum.BranchLeader && users.Any(x => x.BranchId != currentUser.BranchId))
            {
                return (false, $"يجب أن يكون {ResolveLabel(userType)} من نفس الفرع.");
            }

            return (true, null);
        }

        private async Task<User?> ValidateTeacherAsync(int? teacherId, User currentUser)
        {
            if (!teacherId.HasValue || teacherId.Value <= 0)
            {
                return null;
            }

            var teacher = _userRepository.GetById(teacherId.Value);
            if (teacher == null
                || teacher.UserTypeId != (int)UserTypesEnum.Teacher
                || !EducationSystemTypeHelper.SupportsAcademic(teacher.EducationSystemTypeId))
            {
                return null;
            }

            if (currentUser.UserTypeId == (int)UserTypesEnum.BranchLeader && teacher.BranchId != currentUser.BranchId)
            {
                return null;
            }

            return teacher;
        }

        private async Task SyncCircleRelationsAsync(
            int circleId,
            IReadOnlyCollection<int> managerIds,
            IReadOnlyCollection<int> studentIds,
            int userId)
        {
            var existingManagers = _academicManagerCircleRepository.Where(x => x.AcademicCircleId == circleId).ToList();
            var existingStudents = _academicCircleStudentRepository.Where(x => x.AcademicCircleId == circleId).ToList();

            _academicManagerCircleRepository.DeleteRange(existingManagers);
            _academicCircleStudentRepository.DeleteRange(existingStudents);

            foreach (var managerId in managerIds)
            {
                await _academicManagerCircleRepository.AddAsync(new AcademicManagerCircle
                {
                    AcademicCircleId = circleId,
                    ManagerId = managerId,
                    CreatedAt = BusinessDateTime.UtcNow,
                    CreatedBy = userId,
                    IsDeleted = false
                });
            }

            foreach (var studentId in studentIds)
            {
                await _academicCircleStudentRepository.AddAsync(new AcademicCircleStudent
                {
                    AcademicCircleId = circleId,
                    StudentId = studentId,
                    CreatedAt = BusinessDateTime.UtcNow,
                    CreatedBy = userId,
                    IsDeleted = false
                });
            }
        }

        private async Task RebuildDerivedRelationsAsync(int userId)
        {
            var existingManagerTeachers = _academicManagerTeacherRepository.GetAll().ToList();
            var existingManagerStudents = _academicManagerStudentRepository.GetAll().ToList();

            if (existingManagerTeachers.Count > 0)
            {
                _academicManagerTeacherRepository.DeleteRange(existingManagerTeachers);
            }

            if (existingManagerStudents.Count > 0)
            {
                _academicManagerStudentRepository.DeleteRange(existingManagerStudents);
            }

            var activeCircles = _academicCircleRepository.GetAll()
                .Select(x => new { x.Id, x.TeacherId })
                .ToList();

            var activeManagerCircles = _academicManagerCircleRepository.GetAll()
                .Select(x => new { x.ManagerId, x.AcademicCircleId })
                .ToList();

            var activeCircleStudents = _academicCircleStudentRepository.GetAll()
                .Select(x => new { x.StudentId, x.AcademicCircleId })
                .ToList();

            var managerTeacherPairs = (
                from managerCircle in activeManagerCircles
                join circle in activeCircles on managerCircle.AcademicCircleId equals circle.Id
                where managerCircle.ManagerId.HasValue && circle.TeacherId.HasValue
                select new { ManagerId = managerCircle.ManagerId.Value, TeacherId = circle.TeacherId.Value })
                .Distinct()
                .ToList();

            foreach (var pair in managerTeacherPairs)
            {
                await _academicManagerTeacherRepository.AddAsync(new AcademicManagerTeacher
                {
                    ManagerId = pair.ManagerId,
                    TeacherId = pair.TeacherId,
                    CreatedAt = BusinessDateTime.UtcNow,
                    CreatedBy = userId,
                    IsDeleted = false
                });
            }

            var managerStudentPairs = (
                from managerCircle in activeManagerCircles
                join studentLink in activeCircleStudents on managerCircle.AcademicCircleId equals studentLink.AcademicCircleId
                where managerCircle.ManagerId.HasValue && studentLink.StudentId.HasValue
                select new { ManagerId = managerCircle.ManagerId.Value, StudentId = studentLink.StudentId.Value })
                .Distinct()
                .ToList();

            foreach (var pair in managerStudentPairs)
            {
                await _academicManagerStudentRepository.AddAsync(new AcademicManagerStudent
                {
                    ManagerId = pair.ManagerId,
                    StudentId = pair.StudentId,
                    CreatedAt = BusinessDateTime.UtcNow,
                    CreatedBy = userId,
                    IsDeleted = false
                });
            }
        }

        private static int? ResolveBranchId(int? requestedBranchId, int? teacherBranchId, int? currentUserBranchId)
        {
            return requestedBranchId ?? teacherBranchId ?? currentUserBranchId;
        }

        private static string ResolveLabel(UserTypesEnum userType)
        {
            return userType switch
            {
                UserTypesEnum.Manager => "المشرف",
                UserTypesEnum.Teacher => "المعلم",
                UserTypesEnum.Student => "الطالب",
                _ => "المستخدم"
            };
        }
    }
}
