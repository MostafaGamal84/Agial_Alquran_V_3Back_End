using AutoMapper;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.DTO.CircleDto;
using Orbits.GeneralProject.DTO.LockUpDtos;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.Repositroy.Base;
using System.Linq.Expressions;

namespace Orbits.GeneralProject.BLL.UsersForGroupsService
{
    public class UsersForGroupsBLL : BaseBLL, IUsersForGroupsBLL
    {
        private readonly IMapper _mapper;
        private readonly IRepository<User> _UserRepo;
        private readonly IRepository<Nationality> _nationalityRepo;
        private readonly IRepository<Governorate> _governorateRepo;
        private readonly IRepository<ManagerCircle> _managerCircleRepo;
        private readonly IRepository<Circle> _circleRepo;

        public UsersForGroupsBLL(IMapper mapper, IRepository<User> UserRepo, IRepository<Nationality> nationalityRepo, IRepository<Governorate> governorateRepo, IRepository<ManagerCircle> managerCircleRepo, IRepository<Circle> circleRepo) : base(mapper)
        {
            _mapper = mapper;
            _UserRepo = UserRepo;
            _nationalityRepo = nationalityRepo;
            _governorateRepo = governorateRepo;
            _managerCircleRepo = managerCircleRepo;
            _circleRepo = circleRepo;
        }

        //   public IResponse<PagedResultDto<UserLockUpDto>> ManagerRequestTeacherAndStudent(
        //FilteredResultRequestDto pagedDto, int userTypeId, int userId, int? managerId, int? teacherId)
        //   {
        //       var output = new Response<PagedResultDto<UserLockUpDto>>();
        //       var searchWord = pagedDto.SearchTerm?.Trim();
        //       var me = _UserRepo.GetById(userId);
        //       if (me == null) return output.AppendError(MessageCodes.NotFound);

        //       var sw = searchWord?.ToLower();

        //       // Build ONE predicate that includes:
        //       // - target user type (userTypeId)
        //       // - role-based restrictions (branch/manager/teacher)
        //       // - optional text search
        //       Expression<Func<User, bool>> predicate = x =>
        //           x.UserTypeId == userTypeId
        //           // role-based restriction (applies only when the logged-in role matches)
        //           && (!(me.UserTypeId == (int)UserTypesEnum.BranchLeader) || x.BranchId == me.BranchId )
        //           && (!(me.UserTypeId == (int)UserTypesEnum.Manager) || x.ManagerId == me.Id)
        //             && (!(me.UserTypeId == (int)UserTypesEnum.Manager) || x.ManagerId == me.Id || x.TeacherId == teacherId)
        //           && (!(me.UserTypeId == (int)UserTypesEnum.Teacher) || x.TeacherId == me.Id)
        //           // optional search (grouped to avoid &&/|| precedence issues)
        //           && (
        //               string.IsNullOrEmpty(sw) ||
        //               (x.FullName != null && x.FullName.ToLower().Contains(sw)) ||
        //               (x.Mobile != null && x.Mobile.ToLower().Contains(sw)) ||
        //               (x.Email != null && x.Email.ToLower().Contains(sw)) ||
        //               (x.Nationality != null && x.Nationality.Name != null && x.Nationality.Name.ToLower().Contains(sw)) ||
        //               (x.Governorate != null && x.Governorate.Name != null && x.Governorate.Name.ToLower().Contains(sw))
        //           );

        //       // IMPORTANT: pass the predicate to GetPagedList so filtering happens before paging
        //       var paged = GetPagedList<UserLockUpDto, User, int>(
        //           pagedDto,
        //           _UserRepo,
        //           x => x.Id,               // positional key selector
        //           searchExpression: predicate,
        //           sortDirection: "DESC",
        //           disableFilter: true,
        //           excluededColumns: null
        //       );

        //       //// If you want NotFound when there are no items:
        //       //if (paged == null || paged.Items == null || paged.Items.Count == 0)
        //       //    return output.CreateResponse(MessageCodes.NotFound);

        //       return output.CreateResponse(paged);
        //   }
        public IResponse<PagedResultDto<UserLockUpDto>> GetUsersForSelects(
     FilteredResultRequestDto pagedDto,
     int userTypeId,
     int userId,
     int? managerId,
     int? teacherId,
     int? branchId = null,
     int? nationalityId = null
 )
        {
            var output = new Response<PagedResultDto<UserLockUpDto>>();
            var searchWord = pagedDto.SearchTerm?.Trim();
            // --- NEW: read Filter without adding any helper method ---
            bool? Inactive = null; // true => IsActive=true, false => IsActive=false
            var f = pagedDto.Filter?.Trim();
            if (!string.IsNullOrWhiteSpace(f))
            {
                var fl = f.ToLowerInvariant();
                if (fl.Contains("inactive=true")) Inactive = true;
                if (fl.Contains("inactive=false")) Inactive = false;
                pagedDto.Filter = null;
            }
            var me = _UserRepo.GetById(userId);
            if (me == null) return output.AppendError(MessageCodes.NotFound);

            var myType = (UserTypesEnum)(me.UserTypeId ?? 0);

            bool isAdmin = myType == UserTypesEnum.Admin;
            bool isBranchLeader = myType == UserTypesEnum.BranchLeader;
            bool isManager = myType == UserTypesEnum.Manager;
            bool isTeacher = myType == UserTypesEnum.Teacher;

            bool targetIsBranchLeader = userTypeId == (int)UserTypesEnum.BranchLeader;
            bool targetIsManager = userTypeId == (int)UserTypesEnum.Manager;
            bool targetIsTeacher = userTypeId == (int)UserTypesEnum.Teacher;
            bool targetIsStudent = userTypeId == (int)UserTypesEnum.Student;

            // Treat 0 as null (no value) for all incoming filters
            int? safeBranchId = (branchId.HasValue && branchId.Value > 0) ? branchId : null;
            int? safeManagerId = (managerId.HasValue && managerId.Value > 0) ? managerId : null;
            int? safeTeacherId = (teacherId.HasValue && teacherId.Value > 0) ? teacherId : null;
            int? safeNationalityId = (nationalityId.HasValue && nationalityId.Value > 0) ? nationalityId : null;
            int? myBranchId = (me.BranchId.HasValue && me.BranchId.Value > 0) ? me.BranchId : null;

            Func<UserTypesEnum, int> rank = t => t switch
            {
                UserTypesEnum.Admin => 100,
                UserTypesEnum.BranchLeader => 80,
                UserTypesEnum.Manager => 60,
                UserTypesEnum.Teacher => 40,
                UserTypesEnum.Student => 20,
                _ => 0
            };

            var targetType = (UserTypesEnum)userTypeId;
            if (!isAdmin && rank(targetType) >= rank(myType))
                return output.AppendError(MessageCodes.FailedToFetchData);

            string? sw = searchWord?.ToLower();

            // ---------- Security + search predicate ----------
            Expression<Func<User, bool>> predicate;

            if (isAdmin)
            {
                predicate = x =>
                    x.UserTypeId == userTypeId
                    // Admin optional narrowing:
                    && (!targetIsManager || !safeBranchId.HasValue || x.BranchId == safeBranchId.Value)
                    && (!targetIsTeacher || !safeManagerId.HasValue || x.ManagerId == safeManagerId.Value)
                    && (!targetIsStudent ||
                        (!safeTeacherId.HasValue || x.TeacherId == safeTeacherId.Value) &&
                        (!safeManagerId.HasValue || x.ManagerId == safeManagerId.Value) &&
                        (!safeNationalityId.HasValue || x.NationalityId == safeNationalityId.Value)) &&
                        (!Inactive.HasValue || x.Inactive == Inactive.Value)
                    && (
                        string.IsNullOrEmpty(sw) ||
                        (x.FullName != null && x.FullName.ToLower().Contains(sw)) ||
                        (x.Mobile != null && x.Mobile.ToLower().Contains(sw)) ||
                        (x.Email != null && x.Email.ToLower().Contains(sw)) ||
                        (x.Nationality != null && x.Nationality.Name != null && x.Nationality.Name.ToLower().Contains(sw)) ||
                        (x.Governorate != null && x.Governorate.Name != null && x.Governorate.Name.ToLower().Contains(sw))
                    );
                    
                    
            }
            else
            {
                // Non-admin scope restrictions (branch/ownership)
                predicate = x =>
                    x.UserTypeId == userTypeId
                    // Branch leaders (and managers when targeting managers) see only their branch
                    && (!isBranchLeader || !myBranchId.HasValue || x.BranchId == myBranchId.Value)
                    && (!targetIsManager || !isManager || !myBranchId.HasValue || x.BranchId == myBranchId.Value)
                    // Managers see only what they own
                    && (!targetIsTeacher || !isManager || x.ManagerId == me.Id)
                    && (!targetIsStudent || !isManager || x.ManagerId == me.Id)
                    // Teachers see their students
                    && (!targetIsStudent || !isTeacher || x.TeacherId == me.Id)
                    // Explicit filters if provided
                    && (!targetIsStudent || !safeManagerId.HasValue || x.ManagerId == safeManagerId.Value)
                    && (!targetIsStudent || !safeTeacherId.HasValue || x.TeacherId == safeTeacherId.Value)
                    && (!targetIsStudent || !safeNationalityId.HasValue || x.NationalityId == safeNationalityId.Value)
                    && (!targetIsTeacher || !safeManagerId.HasValue || x.ManagerId == safeManagerId.Value)
                    // Search
                    && (
                        string.IsNullOrEmpty(sw) ||
                        (x.FullName != null && x.FullName.ToLower().Contains(sw)) ||
                        (x.Mobile != null && x.Mobile.ToLower().Contains(sw)) ||
                        (x.Email != null && x.Email.ToLower().Contains(sw)) ||
                        (x.Nationality != null && x.Nationality.Name != null && x.Nationality.Name.ToLower().Contains(sw)) ||
                        (x.Governorate != null && x.Governorate.Name != null && x.Governorate.Name.ToLower().Contains(sw))
                    );
            }

            // -------- Fetch paged users (base list) --------
            var paged = GetPagedList<UserLockUpDto, User, int>(
                pagedDto,
                _UserRepo,
                x => x.Id,
                searchExpression: predicate,
                sortDirection: "DESC",
                disableFilter: true,
                excluededColumns: null
            );

            // ============================================
            // Target: Managers -> attach Teachers/Students + ManagerCircles
            // ============================================
            if (targetIsManager && paged.Items?.Any() == true)
            {
                var managerIds = paged.Items.Select(m => m.Id).ToList();

                // 1) Teachers & Students (one shot)
                var relatedUsers = _UserRepo
                    .Where(u => u.ManagerId.HasValue
                                && managerIds.Contains(u.ManagerId.Value)
                                && (u.UserTypeId == (int)UserTypesEnum.Teacher
                                    || u.UserTypeId == (int)UserTypesEnum.Student))
                    .Select(u => new
                    {
                        u.Id,
                        u.FullName,
                        u.Email,
                        u.Mobile,
                        u.SecondMobile,
                        Nationality = u.Nationality != null ? u.Nationality.Name : null,
                        u.NationalityId,
                        Governorate = u.Governorate != null ? u.Governorate.Name : null,
                        u.GovernorateId,
                        u.BranchId,
                        u.ManagerId,
                        u.UserTypeId
                    })
                    .ToList();

                var teachersByManager = relatedUsers
                    .Where(u => u.UserTypeId == (int)UserTypesEnum.Teacher)
                    .GroupBy(u => u.ManagerId!.Value)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(u => new UserLockUpDto
                        {
                            Id = u.Id,
                            FullName = u.FullName,
                            Email = u.Email,
                            Mobile = u.Mobile,
                            SecondMobile = u.SecondMobile,
                            Nationality = u.Nationality,
                            NationalityId = u.NationalityId,
                            Governorate = u.Governorate,
                            GovernorateId = u.GovernorateId,
                            BranchId = u.BranchId
                        }).ToList()
                    );

                var studentsByManager = relatedUsers
                    .Where(u => u.UserTypeId == (int)UserTypesEnum.Student)
                    .GroupBy(u => u.ManagerId!.Value)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(u => new UserLockUpDto
                        {
                            Id = u.Id,
                            FullName = u.FullName,
                            Email = u.Email,
                            Mobile = u.Mobile,
                            SecondMobile = u.SecondMobile,
                            Nationality = u.Nationality,
                            NationalityId = u.NationalityId,
                            Governorate = u.Governorate,
                            GovernorateId = u.GovernorateId,
                            BranchId = u.BranchId
                        }).ToList()
                    );

                // 2) ManagerCircles (many-to-many)
                var mcQ = _managerCircleRepo
                    .Where(mc => mc.ManagerId.HasValue && managerIds.Contains(mc.ManagerId.Value));
                var cQ = _circleRepo.Where(c => true);

                var circlesFlat = (from mc in mcQ
                                   join c in cQ on mc.CircleId equals c.Id
                                   select new
                                   {
                                       mc.ManagerId,
                                       mc.CircleId,
                                       CircleName = c.Name
                                   })
                                   .ToList()
                                   .Select(x => (x.ManagerId!.Value, x.CircleId!.Value, x.CircleName))
                                   .ToList();

                var circlesByManager = circlesFlat
                    .GroupBy(x => x.Item1) // ManagerId
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => new ManagerCirclesDto
                        {
                            ManagerId = x.Item1,
                            CircleId = x.Item2,
                            Circle = x.Item3
                        }).ToList()
                    );

                // 3) Attach
                foreach (var m in paged.Items)
                {
                    m.Teachers = teachersByManager.TryGetValue(m.Id, out var tlist) ? tlist : new List<UserLockUpDto>();
                    m.Students = studentsByManager.TryGetValue(m.Id, out var slist) ? slist : new List<UserLockUpDto>();

                    if (circlesByManager.TryGetValue(m.Id, out var cm))
                    {
                        foreach (var c in cm) c.Manager = m.FullName;
                        m.ManagerCircles = cm;
                    }
                    else
                    {
                        m.ManagerCircles = new List<ManagerCirclesDto>();
                    }
                }
            }

            // ============================================
            // Target: Teachers -> attach Students (by TeacherId) + Manager (Id & Name)
            // ============================================
            if (targetIsTeacher && paged.Items?.Any() == true)
            {
                var teacherIds = paged.Items.Select(t => t.Id).ToList();

                // (A) Students of these teachers
                var studentsRaw = _UserRepo
                    .Where(u => u.UserTypeId == (int)UserTypesEnum.Student
                                && u.TeacherId.HasValue
                                && teacherIds.Contains(u.TeacherId.Value))
                    .Select(u => new
                    {
                        u.Id,
                        u.FullName,
                        u.Email,
                        u.Mobile,
                        u.SecondMobile,
                        Nationality = u.Nationality != null ? u.Nationality.Name : null,
                        u.NationalityId,
                        Governorate = u.Governorate != null ? u.Governorate.Name : null,
                        u.GovernorateId,
                        u.BranchId,
                        u.TeacherId
                    })
                    .ToList();

                var studentsByTeacher = studentsRaw
                    .GroupBy(s => s.TeacherId!.Value)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(s => new UserLockUpDto
                        {
                            Id = s.Id,
                            FullName = s.FullName,
                            Email = s.Email,
                            Mobile = s.Mobile,
                            SecondMobile = s.SecondMobile,
                            Nationality = s.Nationality,
                            NationalityId = s.NationalityId,
                            Governorate = s.Governorate,
                            GovernorateId = s.GovernorateId,
                            BranchId = s.BranchId
                        }).ToList()
                    );

                // (B) Teachers' ManagerId
                var teacherManagerPairs = _UserRepo
                    .Where(u => teacherIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.ManagerId })
                    .ToList()
                    .ToDictionary(x => x.Id, x => x.ManagerId);

                var managerIds = teacherManagerPairs.Values
                    .Where(mid => mid.HasValue && mid.Value > 0)
                    .Select(mid => mid!.Value)
                    .Distinct()
                    .ToList();

                // (C) Manager names
                var managersLite = (managerIds.Count == 0)
                    ? new Dictionary<int, string>()
                    : _UserRepo
                        .Where(u => managerIds.Contains(u.Id))
                        .Select(u => new { u.Id, u.FullName })
                        .ToList()
                        .ToDictionary(m => m.Id, m => m.FullName);

                // (D) Attach
                foreach (var t in paged.Items)
                {
                    t.Students = studentsByTeacher.TryGetValue(t.Id, out var list)
                        ? list
                        : new List<UserLockUpDto>();

                    int? mid = null;
                    if (teacherManagerPairs.TryGetValue(t.Id, out var maybeMid))
                        mid = maybeMid;

                    t.ManagerId = (mid.HasValue && mid.Value > 0) ? mid : null;
                    t.ManagerName = (mid.HasValue && managersLite.TryGetValue(mid.Value, out var mname))
                        ? mname
                        : null;
                }
            }

            // ============================================
            // Target: Students -> attach Teacher (Id & Name) + Manager (Id & Name)
            // ============================================
            if (targetIsStudent && paged.Items?.Any() == true)
            {
                var studentIds = paged.Items.Select(s => s.Id).ToList();

                // (A) The student -> TeacherId/ManagerId links
                var studentLinks = _UserRepo
                    .Where(u => studentIds.Contains(u.Id))
                    .Select(u => new
                    {
                        u.Id,
                        u.TeacherId,
                        u.ManagerId
                    })
                    .ToList()
                    .ToDictionary(x => x.Id, x => new { x.TeacherId, x.ManagerId });

                var teacherIds = studentLinks.Values
                    .Where(v => v.TeacherId.HasValue && v.TeacherId.Value > 0)
                    .Select(v => v.TeacherId!.Value)
                    .Distinct()
                    .ToList();

                var managerIds = studentLinks.Values
                    .Where(v => v.ManagerId.HasValue && v.ManagerId.Value > 0)
                    .Select(v => v.ManagerId!.Value)
                    .Distinct()
                    .ToList();

                // (B) Teacher names in one shot
                var teachersMap = (teacherIds.Count == 0)
                    ? new Dictionary<int, string>()
                    : _UserRepo
                        .Where(u => teacherIds.Contains(u.Id))
                        .Select(u => new { u.Id, u.FullName })
                        .ToList()
                        .ToDictionary(t => t.Id, t => t.FullName);

                // (C) Manager names in one shot
                var managersMap = (managerIds.Count == 0)
                    ? new Dictionary<int, string>()
                    : _UserRepo
                        .Where(u => managerIds.Contains(u.Id))
                        .Select(u => new { u.Id, u.FullName })
                        .ToList()
                        .ToDictionary(m => m.Id, m => m.FullName);

                // (D) Attach to each student row
                foreach (var s in paged.Items)
                {
                    s.TeacherId = null;
                    s.TeacherName = null;
                    s.ManagerId = null;
                    s.ManagerName = null;

                    if (studentLinks.TryGetValue(s.Id, out var link))
                    {
                        if (link.TeacherId.HasValue && link.TeacherId.Value > 0)
                        {
                            s.TeacherId = link.TeacherId.Value;
                            if (teachersMap.TryGetValue(link.TeacherId.Value, out var tname))
                                s.TeacherName = tname;
                        }

                        if (link.ManagerId.HasValue && link.ManagerId.Value > 0)
                        {
                            s.ManagerId = link.ManagerId.Value;
                            if (managersMap.TryGetValue(link.ManagerId.Value, out var mname))
                                s.ManagerName = mname;
                        }
                    }
                }
            }

            return output.CreateResponse(paged);
        }

    }
}
