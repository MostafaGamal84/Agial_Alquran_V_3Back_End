using AutoMapper;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Helpers;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Enums;
using Orbits.GeneralProject.DTO;
using Orbits.GeneralProject.Repositroy.Base;

namespace Orbits.GeneralProject.BLL.AcademicLookupService
{
    public class AcademicLookupBLL : BaseBLL, IAcademicLookupBLL
    {
        private const int AcademicSchoolSystemTypeId = (int)EducationSystemType.AcademicSchool;
        private const int BothSystemsTypeId = (int)EducationSystemType.Both;

        private readonly IRepository<AcademicSubject> _academicSubjectRepository;
        private readonly IRepository<AcademicCircle> _academicCircleRepository;
        private readonly IRepository<AcademicCircleStudent> _academicCircleStudentRepository;
        private readonly IRepository<AcademicManagerCircle> _academicManagerCircleRepository;
        private readonly IRepository<AcademicManagerTeacher> _academicManagerTeacherRepository;
        private readonly IRepository<AcademicManagerStudent> _academicManagerStudentRepository;
        private readonly IRepository<User> _userRepository;

        public AcademicLookupBLL(
            IMapper mapper,
            IRepository<AcademicSubject> academicSubjectRepository,
            IRepository<AcademicCircle> academicCircleRepository,
            IRepository<AcademicCircleStudent> academicCircleStudentRepository,
            IRepository<AcademicManagerCircle> academicManagerCircleRepository,
            IRepository<AcademicManagerTeacher> academicManagerTeacherRepository,
            IRepository<AcademicManagerStudent> academicManagerStudentRepository,
            IRepository<User> userRepository) : base(mapper)
        {
            _academicSubjectRepository = academicSubjectRepository;
            _academicCircleRepository = academicCircleRepository;
            _academicCircleStudentRepository = academicCircleStudentRepository;
            _academicManagerCircleRepository = academicManagerCircleRepository;
            _academicManagerTeacherRepository = academicManagerTeacherRepository;
            _academicManagerStudentRepository = academicManagerStudentRepository;
            _userRepository = userRepository;
        }

        public async Task<IResponse<List<LookupDto>>> GetSubjectsAsync()
        {
            var output = new Response<List<LookupDto>>();
            var items = _academicSubjectRepository.GetAll()
                .OrderBy(x => x.DisplayOrder ?? int.MaxValue)
                .ThenBy(x => x.Name)
                .Select(x => new LookupDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();

            return output.CreateResponse(items);
        }

        public async Task<IResponse<List<LookupDto>>> GetManagersAsync(int userId)
        {
            var output = new Response<List<LookupDto>>();
            var me = _userRepository.GetById(userId);
            if (me == null)
            {
                return output.AppendError(BLL.Constants.MessageCodes.NotFound);
            }

            if (!EducationSystemTypeHelper.CanAccessAcademicModule(me.UserTypeId, me.EducationSystemTypeId))
            {
                return output.CreateResponse(new List<LookupDto>());
            }

            var userType = (UserTypesEnum)(me.UserTypeId ?? 0);
            IQueryable<User> query = _userRepository.Where(x => false);

            if (userType == UserTypesEnum.Admin)
            {
                query = _userRepository.Where(x =>
                    x.UserTypeId == (int)UserTypesEnum.Manager
                    && (x.EducationSystemTypeId == AcademicSchoolSystemTypeId || x.EducationSystemTypeId == BothSystemsTypeId));
            }
            else if (userType == UserTypesEnum.BranchLeader)
            {
                var branchQuery = _userRepository.Where(x =>
                    x.UserTypeId == (int)UserTypesEnum.Manager
                    && x.BranchId == me.BranchId
                    && (x.EducationSystemTypeId == AcademicSchoolSystemTypeId || x.EducationSystemTypeId == BothSystemsTypeId));

                query = branchQuery.Any()
                    ? branchQuery
                    : _userRepository.Where(x =>
                        x.UserTypeId == (int)UserTypesEnum.Manager
                        && (x.EducationSystemTypeId == AcademicSchoolSystemTypeId || x.EducationSystemTypeId == BothSystemsTypeId));
            }
            else if (userType == UserTypesEnum.Manager)
            {
                query = _userRepository.Where(x =>
                    x.Id == me.Id
                    && (x.EducationSystemTypeId == AcademicSchoolSystemTypeId || x.EducationSystemTypeId == BothSystemsTypeId));
            }
            else if (userType == UserTypesEnum.Teacher)
            {
                var managerIds = _academicManagerTeacherRepository
                    .Where(x => x.TeacherId == me.Id && x.ManagerId.HasValue)
                    .Select(x => x.ManagerId!.Value)
                    .Distinct()
                    .ToList();

                query = _userRepository.Where(x =>
                    managerIds.Contains(x.Id)
                    && (x.EducationSystemTypeId == AcademicSchoolSystemTypeId || x.EducationSystemTypeId == BothSystemsTypeId));
            }
            else if (userType == UserTypesEnum.Student)
            {
                var managerIds = _academicManagerStudentRepository
                    .Where(x => x.StudentId == me.Id && x.ManagerId.HasValue)
                    .Select(x => x.ManagerId!.Value)
                    .Distinct()
                    .ToList();

                query = _userRepository.Where(x =>
                    managerIds.Contains(x.Id)
                    && (x.EducationSystemTypeId == AcademicSchoolSystemTypeId || x.EducationSystemTypeId == BothSystemsTypeId));
            }

            return output.CreateResponse(ToLookupList(query));
        }

        public async Task<IResponse<List<LookupDto>>> GetTeachersAsync(int userId, int? managerId)
        {
            var output = new Response<List<LookupDto>>();
            var me = _userRepository.GetById(userId);
            if (me == null)
            {
                return output.AppendError(BLL.Constants.MessageCodes.NotFound);
            }

            if (!EducationSystemTypeHelper.CanAccessAcademicModule(me.UserTypeId, me.EducationSystemTypeId))
            {
                return output.CreateResponse(new List<LookupDto>());
            }

            var userType = (UserTypesEnum)(me.UserTypeId ?? 0);
            IQueryable<User> query;

            if (userType == UserTypesEnum.Teacher)
            {
                query = _userRepository.Where(x =>
                    x.Id == me.Id
                    && (x.EducationSystemTypeId == AcademicSchoolSystemTypeId || x.EducationSystemTypeId == BothSystemsTypeId));
            }
            else if (userType == UserTypesEnum.Manager)
            {
                var teacherIds = _academicManagerTeacherRepository
                    .Where(x => x.ManagerId == me.Id && x.TeacherId.HasValue)
                    .Select(x => x.TeacherId!.Value)
                    .Distinct()
                    .ToList();

                query = _userRepository.Where(x =>
                    teacherIds.Contains(x.Id)
                    && (x.EducationSystemTypeId == AcademicSchoolSystemTypeId || x.EducationSystemTypeId == BothSystemsTypeId));
            }
            else
            {
                query = _userRepository.Where(x =>
                    x.UserTypeId == (int)UserTypesEnum.Teacher
                    && (x.EducationSystemTypeId == AcademicSchoolSystemTypeId || x.EducationSystemTypeId == BothSystemsTypeId));

                if (userType == UserTypesEnum.BranchLeader)
                {
                    query = query.Where(x => x.BranchId == me.BranchId);
                }

                if (managerId.HasValue && managerId.Value > 0)
                {
                    var teacherIds = _academicManagerTeacherRepository
                        .Where(x => x.ManagerId == managerId.Value && x.TeacherId.HasValue)
                        .Select(x => x.TeacherId!.Value)
                        .Distinct()
                        .ToList();

                    query = query.Where(x => teacherIds.Contains(x.Id));
                }
            }

            return output.CreateResponse(ToLookupList(query));
        }

        public async Task<IResponse<List<LookupDto>>> GetTeachersForAssignmentAsync(int userId)
        {
            var output = new Response<List<LookupDto>>();
            var me = _userRepository.GetById(userId);
            if (me == null)
            {
                return output.AppendError(BLL.Constants.MessageCodes.NotFound);
            }

            if (!EducationSystemTypeHelper.CanAccessAcademicModule(me.UserTypeId, me.EducationSystemTypeId))
            {
                return output.CreateResponse(new List<LookupDto>());
            }

            var query = BuildAssignableUsersQuery(me, UserTypesEnum.Teacher);
            return output.CreateResponse(ToLookupList(query));
        }

        public async Task<IResponse<List<LookupDto>>> GetCirclesAsync(int userId, int? managerId, int? teacherId)
        {
            var output = new Response<List<LookupDto>>();
            var me = _userRepository.GetById(userId);
            if (me == null)
            {
                return output.AppendError(BLL.Constants.MessageCodes.NotFound);
            }

            if (!EducationSystemTypeHelper.CanAccessAcademicModule(me.UserTypeId, me.EducationSystemTypeId))
            {
                return output.CreateResponse(new List<LookupDto>());
            }

            var userType = (UserTypesEnum)(me.UserTypeId ?? 0);
            IQueryable<AcademicCircle> query = _academicCircleRepository.GetAll();

            if (userType == UserTypesEnum.BranchLeader)
            {
                query = query.Where(x => x.BranchId == me.BranchId);
            }
            else if (userType == UserTypesEnum.Manager)
            {
                var circleIds = _academicManagerCircleRepository
                    .Where(x => x.ManagerId == me.Id && x.AcademicCircleId.HasValue)
                    .Select(x => x.AcademicCircleId!.Value)
                    .Distinct()
                    .ToList();

                query = query.Where(x => circleIds.Contains(x.Id));
            }
            else if (userType == UserTypesEnum.Teacher)
            {
                query = query.Where(x => x.TeacherId == me.Id);
            }
            else if (userType == UserTypesEnum.Student)
            {
                var circleIds = _academicCircleStudentRepository
                    .Where(x => x.StudentId == me.Id && x.AcademicCircleId.HasValue)
                    .Select(x => x.AcademicCircleId!.Value)
                    .Distinct()
                    .ToList();

                query = query.Where(x => circleIds.Contains(x.Id));
            }

            if (managerId.HasValue && managerId.Value > 0)
            {
                var managedCircleIds = _academicManagerCircleRepository
                    .Where(x => x.ManagerId == managerId.Value && x.AcademicCircleId.HasValue)
                    .Select(x => x.AcademicCircleId!.Value)
                    .Distinct()
                    .ToList();

                query = query.Where(x => managedCircleIds.Contains(x.Id));
            }

            if (teacherId.HasValue && teacherId.Value > 0)
            {
                query = query.Where(x => x.TeacherId == teacherId.Value);
            }

            var items = query
                .OrderBy(x => x.Name)
                .Select(x => new LookupDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();

            return output.CreateResponse(items);
        }

        public async Task<IResponse<List<LookupDto>>> GetStudentsAsync(int userId, int? circleId)
        {
            var output = new Response<List<LookupDto>>();
            var me = _userRepository.GetById(userId);
            if (me == null)
            {
                return output.AppendError(BLL.Constants.MessageCodes.NotFound);
            }

            if (!EducationSystemTypeHelper.CanAccessAcademicModule(me.UserTypeId, me.EducationSystemTypeId))
            {
                return output.CreateResponse(new List<LookupDto>());
            }

            if (!circleId.HasValue || circleId.Value <= 0)
            {
                return output.CreateResponse(GetAccessibleStudents(me));
            }

            var circle = _academicCircleRepository.GetById(circleId.Value);
            if (circle == null || circle.IsDeleted)
            {
                return output.CreateResponse(new List<LookupDto>());
            }

            var userType = (UserTypesEnum)(me.UserTypeId ?? 0);
            var isAllowed = userType == UserTypesEnum.Admin
                || (userType == UserTypesEnum.BranchLeader && circle.BranchId == me.BranchId)
                || (userType == UserTypesEnum.Manager && _academicManagerCircleRepository.Any(x => x.ManagerId == me.Id && x.AcademicCircleId == circle.Id))
                || (userType == UserTypesEnum.Teacher && circle.TeacherId == me.Id)
                || (userType == UserTypesEnum.Student && _academicCircleStudentRepository.Any(x => x.StudentId == me.Id && x.AcademicCircleId == circle.Id));

            if (!isAllowed)
            {
                return output.CreateResponse(new List<LookupDto>());
            }

            var studentIds = _academicCircleStudentRepository
                .Where(x => x.AcademicCircleId == circle.Id && x.StudentId.HasValue)
                .Select(x => x.StudentId!.Value)
                .Distinct()
                .ToList();

            var query = _userRepository.Where(x =>
                studentIds.Contains(x.Id)
                && (x.EducationSystemTypeId == AcademicSchoolSystemTypeId || x.EducationSystemTypeId == BothSystemsTypeId));

            return output.CreateResponse(ToLookupList(query));
        }

        public async Task<IResponse<List<LookupDto>>> GetStudentsForAssignmentAsync(int userId)
        {
            var output = new Response<List<LookupDto>>();
            var me = _userRepository.GetById(userId);
            if (me == null)
            {
                return output.AppendError(BLL.Constants.MessageCodes.NotFound);
            }

            if (!EducationSystemTypeHelper.CanAccessAcademicModule(me.UserTypeId, me.EducationSystemTypeId))
            {
                return output.CreateResponse(new List<LookupDto>());
            }

            var query = BuildAssignableUsersQuery(me, UserTypesEnum.Student);
            return output.CreateResponse(ToLookupList(query));
        }

        private static List<LookupDto> ToLookupList(IQueryable<User> query)
        {
            return query
                .OrderBy(x => x.FullName)
                .Select(x => new LookupDto
                {
                    Id = x.Id,
                    Name = x.FullName
                })
                .ToList();
        }

        private IQueryable<User> BuildAssignableUsersQuery(User currentUser, UserTypesEnum userType)
        {
            var query = _userRepository.Where(x =>
                x.UserTypeId == (int)userType
                && (x.EducationSystemTypeId == AcademicSchoolSystemTypeId || x.EducationSystemTypeId == BothSystemsTypeId));

            var currentUserType = (UserTypesEnum)(currentUser.UserTypeId ?? 0);
            if (currentUserType == UserTypesEnum.Admin)
            {
                return query;
            }

            if (currentUserType == UserTypesEnum.BranchLeader || currentUserType == UserTypesEnum.Manager)
            {
                var branchScopedQuery = query.Where(x => x.BranchId == currentUser.BranchId);
                return branchScopedQuery.Any()
                    ? branchScopedQuery
                    : query;
            }

            return _userRepository.Where(x => false);
        }

        private List<LookupDto> GetAccessibleStudents(User currentUser)
        {
            var currentUserType = (UserTypesEnum)(currentUser.UserTypeId ?? 0);

            if (currentUserType == UserTypesEnum.Admin)
            {
                return ToLookupList(_userRepository.Where(x =>
                    x.UserTypeId == (int)UserTypesEnum.Student
                    && (x.EducationSystemTypeId == AcademicSchoolSystemTypeId || x.EducationSystemTypeId == BothSystemsTypeId)));
            }

            if (currentUserType == UserTypesEnum.BranchLeader)
            {
                var branchScopedQuery = _userRepository.Where(x =>
                    x.UserTypeId == (int)UserTypesEnum.Student
                    && x.BranchId == currentUser.BranchId
                    && (x.EducationSystemTypeId == AcademicSchoolSystemTypeId || x.EducationSystemTypeId == BothSystemsTypeId));

                return branchScopedQuery.Any()
                    ? ToLookupList(branchScopedQuery)
                    : ToLookupList(_userRepository.Where(x =>
                        x.UserTypeId == (int)UserTypesEnum.Student
                        && (x.EducationSystemTypeId == AcademicSchoolSystemTypeId || x.EducationSystemTypeId == BothSystemsTypeId)));
            }

            if (currentUserType == UserTypesEnum.Manager)
            {
                var studentIds = _academicManagerStudentRepository
                    .Where(x => x.ManagerId == currentUser.Id && x.StudentId.HasValue)
                    .Select(x => x.StudentId!.Value)
                    .Distinct()
                    .ToList();

                var relationScopedQuery = _userRepository.Where(x =>
                    studentIds.Contains(x.Id)
                    && (x.EducationSystemTypeId == AcademicSchoolSystemTypeId || x.EducationSystemTypeId == BothSystemsTypeId));

                return relationScopedQuery.Any()
                    ? ToLookupList(relationScopedQuery)
                    : ToLookupList(_userRepository.Where(x =>
                        x.UserTypeId == (int)UserTypesEnum.Student
                        && (x.EducationSystemTypeId == AcademicSchoolSystemTypeId || x.EducationSystemTypeId == BothSystemsTypeId)));
            }

            if (currentUserType == UserTypesEnum.Teacher)
            {
                var studentIds = _academicCircleStudentRepository
                    .Where(x =>
                        x.StudentId.HasValue
                        && x.AcademicCircle != null
                        && x.AcademicCircle.TeacherId == currentUser.Id)
                    .Select(x => x.StudentId!.Value)
                    .Distinct()
                    .ToList();

                var circleScopedQuery = _userRepository.Where(x =>
                    studentIds.Contains(x.Id)
                    && (x.EducationSystemTypeId == AcademicSchoolSystemTypeId || x.EducationSystemTypeId == BothSystemsTypeId));

                return circleScopedQuery.Any()
                    ? ToLookupList(circleScopedQuery)
                    : ToLookupList(_userRepository.Where(x =>
                        x.UserTypeId == (int)UserTypesEnum.Student
                        && (x.EducationSystemTypeId == AcademicSchoolSystemTypeId || x.EducationSystemTypeId == BothSystemsTypeId)));
            }

            if (currentUserType == UserTypesEnum.Student)
            {
                return ToLookupList(_userRepository.Where(x =>
                    x.Id == currentUser.Id
                    && (x.EducationSystemTypeId == AcademicSchoolSystemTypeId || x.EducationSystemTypeId == BothSystemsTypeId)));
            }

            return new List<LookupDto>();
        }
    }
}
