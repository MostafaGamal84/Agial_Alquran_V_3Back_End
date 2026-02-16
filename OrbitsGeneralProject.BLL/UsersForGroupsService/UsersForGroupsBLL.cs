using AutoMapper;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.Helpers;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.DTO.CircleDto;
using Orbits.GeneralProject.DTO.LockUpDtos;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.Repositroy.Base;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

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
        private readonly IRepository<ManagerTeacher> _managerTeacherRepo;
        private readonly IRepository<ManagerStudent> _managerStudentRepo;

        public UsersForGroupsBLL(IMapper mapper, IRepository<User> UserRepo, IRepository<Nationality> nationalityRepo, IRepository<Governorate> governorateRepo, IRepository<ManagerCircle> managerCircleRepo, IRepository<Circle> circleRepo, IRepository<ManagerTeacher> managerTeacherRepo, IRepository<ManagerStudent> managerStudentRepo) : base(mapper)
        {
            _mapper = mapper;
            _UserRepo = UserRepo;
            _nationalityRepo = nationalityRepo;
            _governorateRepo = governorateRepo;
            _managerCircleRepo = managerCircleRepo;
            _circleRepo = circleRepo;
            _managerTeacherRepo = managerTeacherRepo;
            _managerStudentRepo = managerStudentRepo;
        }


        public IResponse<PagedResultDto<UserLockUpDto>> GetUsersForSelects(
     FilteredResultRequestDto pagedDto,
     int userTypeId,
     int userId,
     int? managerId,
     int? teacherId,
     int? branchId = null,
     int? nationalityId = null,
     bool includeRelations = false,
     int? targetUserId = null
 )
      {
            var output = new Response<PagedResultDto<UserLockUpDto>>();
            var searchWord = pagedDto.SearchTerm?.Trim();
            // --- NEW: read Filter without adding any helper method ---
            bool? Inactive = null; // true => IsActive=true, false => IsActive=false
            bool includeManagerRelations = includeRelations; // keep previous behaviour unless explicitly disabled
            bool includeTeacherAndStudentRelations = includeRelations;
            bool lookupOnly = false; // when true, return Id/FullName only for lightweight lookups
            var f = pagedDto.Filter?.Trim();
            if (!string.IsNullOrWhiteSpace(f))
            {
                var fl = f.ToLowerInvariant();
                if (fl.Contains("inactive=true")) Inactive = true;
                if (fl.Contains("inactive=false")) Inactive = false;
                if (fl.Contains("includemanagerrelations=false") || fl.Contains("includemanagers=false") || fl.Contains("includerelations=false"))
                {
                    includeManagerRelations = false;
                    includeTeacherAndStudentRelations = false;
                }
                if (fl.Contains("includemanagerrelations=true") || fl.Contains("includemanagers=true") || fl.Contains("includerelations=true"))
                {
                    includeManagerRelations = true;
                    includeTeacherAndStudentRelations = true;
                }
                if (fl.Contains("lookuponly=true") || fl.Contains("lookup=true") || fl.Contains("idsonly=true"))
                {
                    lookupOnly = true;
                    pagedDto.MaxResultCount = 3000000;
                    includeManagerRelations = false; // skip heavy joins for lookup-only mode
                    includeTeacherAndStudentRelations = false;
                }
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
            var residentGroup = ResidentGroupFilterHelper.Parse(pagedDto?.ResidentGroup);
            var residentIdsFilter = ResidentGroupFilterHelper.ResolveResidentIds(_nationalityRepo.GetAll(), residentGroup);
            bool applyResidentFilter = residentIdsFilter != null;

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
            var managerTeachersQuery = _managerTeacherRepo.GetAll();
            var managerStudentsQuery = _managerStudentRepo.GetAll();

            // ---------- Security + search predicate ----------
            Expression<Func<User, bool>> predicate;

            if (isAdmin)
            {
                predicate = x =>
                    x.UserTypeId == userTypeId
                    && (!targetUserId.HasValue || x.Id == targetUserId.Value)
                    // Admin optional narrowing:
                    && (!targetIsManager || !safeBranchId.HasValue || x.BranchId == safeBranchId.Value)
                    && (!targetIsTeacher || !safeManagerId.HasValue || managerTeachersQuery.Any(mt => mt.ManagerId == safeManagerId.Value && mt.TeacherId == x.Id))
                    && (!targetIsStudent ||
                        (!safeTeacherId.HasValue || x.TeacherId == safeTeacherId.Value) &&
                        (!safeManagerId.HasValue ||
                            managerStudentsQuery.Any(ms => ms.ManagerId == safeManagerId.Value && ms.StudentId == x.Id)) &&
                        (!safeNationalityId.HasValue || x.NationalityId == safeNationalityId.Value) &&
                        (!applyResidentFilter || (x.ResidentId.HasValue && residentIdsFilter!.Contains(x.ResidentId.Value)))) &&
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
                    && (!targetUserId.HasValue || x.Id == targetUserId.Value)
                    // Branch leaders (and managers when targeting managers) see only their branch
                    && (!isBranchLeader || !myBranchId.HasValue || x.BranchId == myBranchId.Value)
                    && (!targetIsManager || !isManager || !myBranchId.HasValue || x.BranchId == myBranchId.Value)
                    // Managers see only what they own
                    && (!targetIsTeacher || !isManager || managerTeachersQuery.Any(mt => mt.ManagerId == me.Id && mt.TeacherId == x.Id))
                    && (!targetIsStudent || !isManager ||
                        managerStudentsQuery.Any(ms => ms.ManagerId == me.Id && ms.StudentId == x.Id))
                    // Teachers see their students
                    && (!targetIsStudent || !isTeacher || x.TeacherId == me.Id)
                    // Explicit filters if provided
                    && (!targetIsStudent || !safeManagerId.HasValue ||
                        managerStudentsQuery.Any(ms => ms.ManagerId == safeManagerId.Value && ms.StudentId == x.Id))
                    && (!targetIsStudent || !safeTeacherId.HasValue || x.TeacherId == safeTeacherId.Value)
                    && (!targetIsStudent || !safeNationalityId.HasValue || x.NationalityId == safeNationalityId.Value)
                    && (!targetIsStudent || !applyResidentFilter || (x.ResidentId.HasValue && residentIdsFilter!.Contains(x.ResidentId.Value)))
                    && (!targetIsTeacher || !safeManagerId.HasValue || managerTeachersQuery.Any(mt => mt.ManagerId == safeManagerId.Value && mt.TeacherId == x.Id))
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
            if (lookupOnly)
            {
                var lookupQuery = _UserRepo
                    .DisableFilter()
                    .Where(predicate)
                    .AsNoTracking();

                var lookupTotal = lookupQuery.Count();
                var lookupItems = lookupQuery
                    .OrderByDescending(x => x.Id)
                    .Skip(pagedDto.SkipCount)
                    .Take(pagedDto.MaxResultCount)
                    .Select(x => new UserLockUpDto
                    {
                        Id = x.Id,
                        FullName = x.FullName
                    })
                    .ToList();

                var lookupResult = new PagedResultDto<UserLockUpDto>
                {
                    Items = lookupItems,
                    TotalCount = lookupTotal
                };

                return output.CreateResponse(lookupResult);
            }

            // Fast path for lightweight table view: project only scalar fields with AsNoTracking
            // to avoid loading navigation properties / tracking overhead.
            if (!includeManagerRelations && !includeTeacherAndStudentRelations)
            {
                var baseQuery = _UserRepo
                    .DisableFilter()
                    .Where(predicate)
                    .AsNoTracking();

                var totalCount = baseQuery.Count();
                var items = baseQuery
                    .OrderByDescending(x => x.Id)
                    .Skip(pagedDto.SkipCount)
                    .Take(pagedDto.MaxResultCount)
                    .Select(x => new UserLockUpDto
                    {
                        Id = x.Id,
                        FullName = x.FullName,
                        Email = x.Email,
                        Mobile = x.Mobile,
                        SecondMobile = x.SecondMobile,
                        Nationality = x.Nationality != null ? x.Nationality.Name : null,
                        NationalityId = x.NationalityId,
                        Resident = x.Resident != null ? x.Resident.Name : null,
                        ResidentId = x.ResidentId,
                        Governorate = x.Governorate != null ? x.Governorate.Name : null,
                        GovernorateId = x.GovernorateId,
                        BranchId = x.BranchId,
                        TeacherId = x.TeacherId,
                        ManagerId = managerStudentsQuery.Where(ms => ms.StudentId == x.Id && ms.ManagerId.HasValue).Select(ms => ms.ManagerId).FirstOrDefault(),
                        Inactive = x.Inactive
                    })
                    .ToList();

                var lightweight = new PagedResultDto<UserLockUpDto>
                {
                    Items = items,
                    TotalCount = totalCount
                };

                return output.CreateResponse(lightweight);
            }

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
            // Target: BranchLeader -> attach Managers (with their teachers/students/circles)
            // ============================================
            if (targetIsBranchLeader && includeManagerRelations && paged.Items?.Any() == true)
            {
                var branchIds = paged.Items
                    .Where(bl => bl.BranchId.HasValue)
                    .Select(bl => bl.BranchId!.Value)
                    .Distinct()
                    .ToList();

                // Load all managers that belong to the target branch leaders' branches
                var managerRecords = _UserRepo
                    .Where(u => u.UserTypeId == (int)UserTypesEnum.Manager
                                && u.BranchId.HasValue
                                && branchIds.Contains(u.BranchId.Value))
                    .AsNoTracking()
                    .Select(u => new
                    {
                        u.Id,
                        u.FullName,
                        u.Email,
                        u.Mobile,
                        u.SecondMobile,
                        Nationality = u.Nationality != null ? u.Nationality.Name : null,
                        u.NationalityId,
                        Resident = u.Resident != null ? u.Resident.Name : null,
                        u.ResidentId,
                        Governorate = u.Governorate != null ? u.Governorate.Name : null,
                        u.GovernorateId,
                        u.BranchId
                    })
                    .ToList();

                var managerIds = managerRecords.Select(m => m.Id).ToList();

                // Teachers for these managers (and any unassigned teachers in the same branch)
                var branchTeachers = (branchIds.Count == 0)
                    ? new List<UserLockUpDto>()
                    : _UserRepo
                        .Where(u => u.UserTypeId == (int)UserTypesEnum.Teacher
                                    && u.BranchId.HasValue
                                    && branchIds.Contains(u.BranchId.Value))
                        .AsNoTracking()
                        .Select(u => new UserLockUpDto
                        {
                            Id = u.Id,
                            FullName = u.FullName,
                            Email = u.Email,
                            Mobile = u.Mobile,
                            SecondMobile = u.SecondMobile,
                            Nationality = u.Nationality != null ? u.Nationality.Name : null,
                            NationalityId = u.NationalityId,
                            Resident = u.Resident != null ? u.Resident.Name : null,
                            ResidentId = u.ResidentId,
                            Governorate = u.Governorate != null ? u.Governorate.Name : null,
                            GovernorateId = u.GovernorateId,
                            BranchId = u.BranchId,
                            ManagerId = managerTeachersQuery.Where(mt => mt.TeacherId == u.Id && mt.ManagerId.HasValue).Select(mt => mt.ManagerId).FirstOrDefault()
                        })
                        .ToList();

                var teachersByManager = branchTeachers
                    .Where(u => u.ManagerId != null && managerIds.Contains((int)u.ManagerId))
                    .GroupBy(u => (int)u.ManagerId)
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
                            Resident = u.Resident,
                            ResidentId = u.ResidentId,
                            Governorate = u.Governorate,
                            GovernorateId = u.GovernorateId,
                            BranchId = u.BranchId
                        }).ToList()
                    );

                var teachersWithoutManagerByBranch = branchTeachers
                    .Where(u => !managerTeachersQuery.Any(mt => mt.TeacherId == u.Id && mt.ManagerId.HasValue))
                    .GroupBy(u => (int)u.BranchId)
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
                            Resident = u.Resident,
                            ResidentId = u.ResidentId,
                            Governorate = u.Governorate,
                            GovernorateId = u.GovernorateId,
                            BranchId = u.BranchId
                        }).ToList()
                    );

                // Students for these managers
                var studentsByManager = (managerIds.Count == 0)
                    ? new Dictionary<int, List<UserLockUpDto>>()
                    : _UserRepo
                        .Where(u => managerStudentsQuery.Any(ms => ms.StudentId == u.Id && ms.ManagerId.HasValue
                                    && managerIds.Contains(ms.ManagerId.Value))
                                    && u.UserTypeId == (int)UserTypesEnum.Student
                                    && u.BranchId.HasValue
                                    && branchIds.Contains(u.BranchId.Value))
                        .AsNoTracking()
                        .Select(u => new
                        {
                            u.Id,
                            u.FullName,
                            u.Email,
                            u.Mobile,
                            u.SecondMobile,
                            Nationality = u.Nationality != null ? u.Nationality.Name : null,
                            u.NationalityId,
                            Resident = u.Resident != null ? u.Resident.Name : null,
                            u.ResidentId,
                            Governorate = u.Governorate != null ? u.Governorate.Name : null,
                            u.GovernorateId,
                            u.BranchId,
                            ManagerId = managerTeachersQuery.Where(mt => mt.TeacherId == u.Id && mt.ManagerId.HasValue).Select(mt => mt.ManagerId).FirstOrDefault()
                        })
                        .ToList()
                        .GroupBy(u => managerStudentsQuery.Where(ms => ms.StudentId == u.Id && ms.ManagerId.HasValue).Select(ms => ms.ManagerId!.Value).FirstOrDefault())
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
                                Resident = u.Resident,
                                ResidentId = u.ResidentId,
                                Governorate = u.Governorate,
                                GovernorateId = u.GovernorateId,
                                BranchId = u.BranchId
                            }).ToList()
                        );

                // Manager circles
                var circlesQuery = _circleRepo
                    .Where(c => !myBranchId.HasValue || (c.BranchId.HasValue && c.BranchId == myBranchId.Value))
                    .AsNoTracking();
                var circlesByManager = (managerIds.Count == 0)
                    ? new Dictionary<int, List<ManagerCirclesDto>>()
                    : (from mc in _managerCircleRepo.Where(mc => mc.ManagerId.HasValue && managerIds.Contains(mc.ManagerId.Value)).AsNoTracking()
                       join c in circlesQuery on mc.CircleId equals c.Id
                       select new
                       {
                           mc.ManagerId,
                           mc.CircleId,
                           CircleName = c.Name
                       })
                       .AsEnumerable()
                       .GroupBy(x => x.ManagerId!.Value)
                       .ToDictionary(
                           g => g.Key,
                           g => g.Select(x => new ManagerCirclesDto
                           {
                               ManagerId = x.ManagerId,
                               CircleId = x.CircleId,
                               Circle = x.CircleName
                           }).ToList()
                       );

                // Build manager DTOs grouped by branch
                var managersByBranch = managerRecords
                    .GroupBy(m => m.BranchId!.Value)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(m => new UserLockUpDto
                        {
                            Id = m.Id,
                            FullName = m.FullName,
                            Email = m.Email,
                            Mobile = m.Mobile,
                            SecondMobile = m.SecondMobile,
                            Nationality = m.Nationality,
                            NationalityId = m.NationalityId,
                            Resident = m.Resident,
                            ResidentId = m.ResidentId,
                            Governorate = m.Governorate,
                            GovernorateId = m.GovernorateId,
                            BranchId = m.BranchId,
                            Teachers = teachersByManager.TryGetValue(m.Id, out var tl) ? tl : new List<UserLockUpDto>(),
                            Students = studentsByManager.TryGetValue(m.Id, out var sl) ? sl : new List<UserLockUpDto>(),
                            ManagerCircles = circlesByManager.TryGetValue(m.Id, out var cl) ? cl : new List<ManagerCirclesDto>()
                        }).ToList()
                    );

                foreach (var bl in paged.Items)
                {
                    var branchManagers = (bl.BranchId.HasValue && managersByBranch.TryGetValue(bl.BranchId.Value, out var mgrs))
                        ? mgrs
                        : new List<UserLockUpDto>();

                    bl.Managers = branchManagers;
                    var branchTeachersWithoutManager = (bl.BranchId.HasValue && teachersWithoutManagerByBranch.TryGetValue(bl.BranchId.Value, out var tm))
                        ? tm
                        : new List<UserLockUpDto>();

                    bl.Teachers = branchManagers
                        .SelectMany(m => m.Teachers ?? new List<UserLockUpDto>())
                        .Concat(branchTeachersWithoutManager)
                        .ToList();
                    bl.Students = branchManagers
                        .SelectMany(m => m.Students ?? new List<UserLockUpDto>())
                        .ToList();
                }
            }
            else if (targetIsBranchLeader && paged.Items?.Any() == true)
            {
                foreach (var bl in paged.Items)
                {
                    bl.Managers = new List<UserLockUpDto>();
                    bl.Teachers = new List<UserLockUpDto>();
                    bl.Students = new List<UserLockUpDto>();
                }
            }

            // ============================================
            // Target: Managers -> attach Teachers/Students + ManagerCircles
            // ============================================
            if (targetIsManager && includeManagerRelations && paged.Items?.Any() == true)
            {
                var managerIds = paged.Items.Select(m => m.Id).ToList();

                // 1) Teachers & Students (one shot)
                var usersQ = _UserRepo.DisableFilter(); // مهم: ده IQueryable<User>

                var teachersRaw = (from mt in managerTeachersQuery
                                   join u in usersQ on mt.TeacherId equals u.Id
                                   where mt.ManagerId.HasValue
                                         && managerIds.Contains(mt.ManagerId.Value)
                                         && u.UserTypeId == (int)UserTypesEnum.Teacher
                                   select new
                                   {
                                       u.Id,
                                       u.FullName,
                                       u.Email,
                                       u.Mobile,
                                       u.SecondMobile,
                                       Nationality = u.Nationality != null ? u.Nationality.Name : null,
                                       u.NationalityId,
                                       Resident = u.Resident != null ? u.Resident.Name : null,
                                       u.ResidentId,
                                       Governorate = u.Governorate != null ? u.Governorate.Name : null,
                                       u.GovernorateId,
                                       u.BranchId,
                                       ManagerId = mt.ManagerId,
                                       u.UserTypeId
                                   })
                                  .AsNoTracking()
                                  .ToList();

                var studentsRaw = (from ms in managerStudentsQuery
                                   join u in usersQ on ms.StudentId equals u.Id
                                   where ms.ManagerId.HasValue
                                         && managerIds.Contains(ms.ManagerId.Value)
                                         && u.UserTypeId == (int)UserTypesEnum.Student
                                   select new
                                   {
                                       u.Id,
                                       u.FullName,
                                       u.Email,
                                       u.Mobile,
                                       u.SecondMobile,
                                       Nationality = u.Nationality != null ? u.Nationality.Name : null,
                                       u.NationalityId,
                                       Resident = u.Resident != null ? u.Resident.Name : null,
                                       u.ResidentId,
                                       Governorate = u.Governorate != null ? u.Governorate.Name : null,
                                       u.GovernorateId,
                                       u.BranchId,
                                       ManagerId = ms.ManagerId,
                                       u.UserTypeId
                                   })
                                  .AsNoTracking()
                                  .ToList();

                var relatedUsers = teachersRaw.Concat(studentsRaw).ToList();

                var teachersByManager = relatedUsers
                    .Where(u => u.UserTypeId == (int)UserTypesEnum.Teacher)
                    .Where(u => u.ManagerId.HasValue)
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
                            Resident = u.Resident,
                            ResidentId = u.ResidentId,
                            Governorate = u.Governorate,
                            GovernorateId = u.GovernorateId,
                            BranchId = u.BranchId
                        }).ToList()
                    );

                var studentsByManager = relatedUsers
                    .Where(u => u.UserTypeId == (int)UserTypesEnum.Student)
                    .Where(u => u.ManagerId.HasValue)
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
                            Resident = u.Resident,
                            ResidentId = u.ResidentId,
                            Governorate = u.Governorate,
                            GovernorateId = u.GovernorateId,
                            BranchId = u.BranchId
                        }).ToList()
                    );

                // 2) ManagerCircles (many-to-many)
                var mcQ = _managerCircleRepo
                    .Where(mc => mc.ManagerId.HasValue && managerIds.Contains(mc.ManagerId.Value))
                    .AsNoTracking();
                var cQ = _circleRepo
                    .Where(c => !myBranchId.HasValue || (c.BranchId.HasValue && c.BranchId == myBranchId.Value))
                    .AsNoTracking();

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
            else if (targetIsManager && paged.Items?.Any() == true)
            {
                // Preserve response shape when related data is skipped
                foreach (var m in paged.Items)
                {
                    m.Teachers = new List<UserLockUpDto>();
                    m.Students = new List<UserLockUpDto>();
                    m.ManagerCircles = new List<ManagerCirclesDto>();
                }
            }

            // ============================================
            // Target: Teachers -> attach Students (by TeacherId) + Manager (Id & Name)
            // ============================================
            if (targetIsTeacher && includeTeacherAndStudentRelations && paged.Items?.Any() == true)
            {
                var teacherIds = paged.Items.Select(t => t.Id).ToList();

                // (A) Students of these teachers
                var studentsRaw = _UserRepo
                    .Where(u => u.UserTypeId == (int)UserTypesEnum.Student
                                && u.TeacherId.HasValue
                                && teacherIds.Contains(u.TeacherId.Value))
                    .AsNoTracking()
                    .Select(u => new
                    {
                        u.Id,
                        u.FullName,
                        u.Email,
                        u.Mobile,
                        u.SecondMobile,
                        Nationality = u.Nationality != null ? u.Nationality.Name : null,
                        u.NationalityId,
                        Resident = u.Resident != null ? u.Resident.Name : null,
                        u.ResidentId,
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
                            Resident = s.Resident,
                            ResidentId = s.ResidentId,
                            Governorate = s.Governorate,
                            GovernorateId = s.GovernorateId,
                            BranchId = s.BranchId
                        }).ToList()
                    );

                // (B) Teachers' Managers (support multi-manager + stable fallback)
                var teacherManagerPairs = (from mt in managerTeachersQuery
                                           where mt.TeacherId.HasValue
                                                 && mt.ManagerId.HasValue
                                                 && teacherIds.Contains(mt.TeacherId.Value)
                                           select new
                                           {
                                               TeacherId = mt.TeacherId.Value,
                                               ManagerId = mt.ManagerId.Value,
                                               mt.Id
                                           })
                                          .AsNoTracking()
                                          .ToList();

                var managerIds = teacherManagerPairs
                    .Select(x => x.ManagerId)
                    .Distinct()
                    .ToList();

                // (C) Manager names
                var managersLite = (managerIds.Count == 0)
                    ? new Dictionary<int, string>()
                    : _UserRepo
                        .Where(u => managerIds.Contains(u.Id))
                        .AsNoTracking()
                        .Select(u => new { u.Id, u.FullName })
                        .ToList()
                        .ToDictionary(m => m.Id, m => m.FullName);

                // (D) Attach
                foreach (var t in paged.Items)
                {
                    t.Students = studentsByTeacher.TryGetValue(t.Id, out var list)
                        ? list
                        : new List<UserLockUpDto>();

                    var teacherManagers = teacherManagerPairs
                        .Where(x => x.TeacherId == t.Id)
                        .OrderByDescending(x => x.Id)
                        .ToList();

                    t.ManagerIds = teacherManagers
                        .Select(x => x.ManagerId)
                        .Distinct()
                        .ToList();

                    t.ManagerNames = t.ManagerIds
                        .Where(mid => managersLite.ContainsKey(mid))
                        .Select(mid => managersLite[mid])
                        .Distinct()
                        .ToList();

                    var latestManager = teacherManagers.FirstOrDefault();
                    t.ManagerId = latestManager?.ManagerId;
                    t.ManagerName = (latestManager != null && managersLite.TryGetValue(latestManager.ManagerId, out var mname))
                        ? mname
                        : null;
                }
            }

            // ============================================
            // Target: Students -> attach Teacher (Id & Name) + Manager (Id & Name)
            // ============================================
            if (targetIsStudent && includeTeacherAndStudentRelations && paged.Items?.Any() == true)
            {
                var studentIds = paged.Items.Select(s => s.Id).ToList();

                // (A) The student -> TeacherId link + Managers (support multi-manager + stable fallback)
                var studentTeacherLinks = _UserRepo
                    .Where(u => studentIds.Contains(u.Id))
                    .AsNoTracking()
                    .Select(u => new
                    {
                        u.Id,
                        u.TeacherId
                    })
                    .ToList()
                    .ToDictionary(x => x.Id, x => x.TeacherId);

                var studentManagerPairs = (from ms in managerStudentsQuery
                                           where ms.StudentId.HasValue
                                                 && ms.ManagerId.HasValue
                                                 && studentIds.Contains(ms.StudentId.Value)
                                           select new
                                           {
                                               StudentId = ms.StudentId.Value,
                                               ManagerId = ms.ManagerId.Value,
                                               ms.Id
                                           })
                                          .AsNoTracking()
                                          .ToList();

                var teacherIds = studentTeacherLinks.Values
                    .Where(v => v.HasValue && v.Value > 0)
                    .Select(v => v!.Value)
                    .Distinct()
                    .ToList();

                var managerIds = studentManagerPairs
                    .Select(v => v.ManagerId)
                    .Distinct()
                    .ToList();

                // (B) Teacher names in one shot
                var teachersMap = (teacherIds.Count == 0)
                    ? new Dictionary<int, string>()
                    : _UserRepo
                        .Where(u => teacherIds.Contains(u.Id))
                        .AsNoTracking()
                        .Select(u => new { u.Id, u.FullName })
                        .ToList()
                        .ToDictionary(t => t.Id, t => t.FullName);

                // (C) Manager names in one shot
                var managersMap = (managerIds.Count == 0)
                    ? new Dictionary<int, string>()
                    : _UserRepo
                        .Where(u => managerIds.Contains(u.Id))
                        .AsNoTracking()
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
                    s.ManagerIds = new List<int>();
                    s.ManagerNames = new List<string>();

                    int? linkedTeacherId = null;
                    if (studentTeacherLinks.TryGetValue(s.Id, out linkedTeacherId)
                        && linkedTeacherId.HasValue
                        && linkedTeacherId.Value > 0)
                    {
                        s.TeacherId = linkedTeacherId.Value;
                        if (teachersMap.TryGetValue(linkedTeacherId.Value, out var tname))
                            s.TeacherName = tname;
                    }

                    var studentManagers = studentManagerPairs
                        .Where(x => x.StudentId == s.Id)
                        .OrderByDescending(x => x.Id)
                        .ToList();

                    s.ManagerIds = studentManagers
                        .Select(x => x.ManagerId)
                        .Distinct()
                        .ToList();

                    s.ManagerNames = s.ManagerIds
                        .Where(mid => managersMap.ContainsKey(mid))
                        .Select(mid => managersMap[mid])
                        .Distinct()
                        .ToList();

                    var latestManager = studentManagers.FirstOrDefault();
                    s.ManagerId = latestManager?.ManagerId;
                    s.ManagerName = (latestManager != null && managersMap.TryGetValue(latestManager.ManagerId, out var mname))
                        ? mname
                        : null;
                }
            }

            return output.CreateResponse(paged);
        }


        public IResponse<PagedResultDto<UserLockUpDto>> GetDeletedUsersByType(FilteredResultRequestDto pagedDto, int userTypeId, int requesterId)
        {
            var output = new Response<PagedResultDto<UserLockUpDto>>();
            string? sw = pagedDto.SearchTerm?.Trim().ToLower();

            var requester = _UserRepo.GetById(requesterId);
            if (requester == null)
                return output.AppendError(MessageCodes.NotFound);

            var requesterType = (UserTypesEnum)(requester.UserTypeId ?? 0);
            var requesterBranchId = requester.BranchId;

            var deletedUsersQuery = _UserRepo
                .DisableFilter(nameof(Repositroy.Enums.DynamicFilters.IsDeleted))
                .Where(x => x.UserTypeId == userTypeId
                    && (
                        string.IsNullOrEmpty(sw)
                        || (x.FullName != null && x.FullName.ToLower().Contains(sw))
                        || (x.Mobile != null && x.Mobile.ToLower().Contains(sw))
                        || (x.Email != null && x.Email.ToLower().Contains(sw))
                    ));

            if (requesterType == UserTypesEnum.BranchLeader)
            {
                if (!requesterBranchId.HasValue)
                    return output.CreateResponse(new PagedResultDto<UserLockUpDto>
                    {
                        Items = new List<UserLockUpDto>(),
                        TotalCount = 0
                    });

                deletedUsersQuery = deletedUsersQuery.Where(x => x.BranchId == requesterBranchId.Value);
            }

            deletedUsersQuery = deletedUsersQuery.AsNoTracking();

            var totalCount = deletedUsersQuery.Count();
            var items = deletedUsersQuery
                .OrderByDescending(x => x.Id)
                .Skip(pagedDto.SkipCount)
                .Take(pagedDto.MaxResultCount)
                .Select(x => new UserLockUpDto
                {
                    Id = x.Id,
                    FullName = x.FullName,
                    Email = x.Email,
                    Mobile = x.Mobile,
                    SecondMobile = x.SecondMobile,
                    Nationality = x.Nationality != null ? x.Nationality.Name : null,
                    NationalityId = x.NationalityId,
                    Resident = x.Resident != null ? x.Resident.Name : null,
                    ResidentId = x.ResidentId,
                    Governorate = x.Governorate != null ? x.Governorate.Name : null,
                    GovernorateId = x.GovernorateId,
                    BranchId = x.BranchId,
                    TeacherId = x.TeacherId,
                    Inactive = x.Inactive
                })
                .ToList();

            var result = new PagedResultDto<UserLockUpDto>
            {
                Items = items,
                TotalCount = totalCount
            };

            return output.CreateResponse(result);
        }

        public IResponse<UserLockUpDto> GetUserDetails(int targetUserId, int requesterId)
        {
            var output = new Response<UserLockUpDto>();
            var targetUser = _UserRepo.GetById(targetUserId);
            if (targetUser == null)
            {
                return output.AppendError(MessageCodes.NotFound);
            }

            var pagedDto = new FilteredResultRequestDto
            {
                SkipCount = 0,
                MaxResultCount = 1
            };

            int? resolvedManagerId = null;
            var targetType = (UserTypesEnum)(targetUser.UserTypeId ?? 0);
            if (targetType == UserTypesEnum.Manager)
            {
                resolvedManagerId = targetUser.Id;
            }
            else if (targetType == UserTypesEnum.Student)
            {
                resolvedManagerId = _managerStudentRepo.GetAll()
                    .Where(ms => ms.StudentId == targetUser.Id && ms.ManagerId.HasValue)
                    .OrderByDescending(ms => ms.Id)
                    .Select(ms => ms.ManagerId)
                    .FirstOrDefault();
            }
            else if (targetType == UserTypesEnum.Teacher)
            {
                resolvedManagerId = _managerTeacherRepo.GetAll()
                    .Where(mt => mt.TeacherId == targetUser.Id && mt.ManagerId.HasValue)
                    .OrderByDescending(mt => mt.Id)
                    .Select(mt => mt.ManagerId)
                    .FirstOrDefault();
            }

            var detailsResponse = GetUsersForSelects(
                pagedDto,
                targetUser.UserTypeId ?? 0,
                requesterId,
                resolvedManagerId,
                targetUser.TeacherId,
                targetUser.BranchId,
                targetUser.NationalityId,
                includeRelations: true,
                targetUserId: targetUserId);

            if (!detailsResponse.IsSuccess || detailsResponse.Data?.Items == null || !detailsResponse.Data.Items.Any())
            {
                return output.AppendError(MessageCodes.NotFound);
            }

            return output.CreateResponse(detailsResponse.Data.Items.First());
        }

    }
}
