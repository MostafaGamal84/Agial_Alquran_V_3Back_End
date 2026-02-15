using AutoMapper;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.BLL.Validation.CircleValidation;
using Orbits.GeneralProject.BLL.Validation.UserValidation;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO;
using Orbits.GeneralProject.DTO.CircleDto;
using Orbits.GeneralProject.DTO.ManagerDto;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.UserDto;
using Orbits.GeneralProject.DTO.UserDtos;
using Orbits.GeneralProject.Repositroy.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.BLL.CircleService
{
    public class CircleBLL : BaseBLL, ICircleBLL
    {
        private readonly IRepository<Circle> _circleRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<ManagerCircle> _managerCircleRepository;
        private readonly IRepository<ManagerStudent> _managerStudentRepository;
        private readonly IRepository<CircleDay> _circleDayRepository;
        private readonly IRepository<Day> _dayRepository;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        private const int DefaultUpcomingTake = 4;

        private static readonly TimeZoneInfo BusinessTimeZone = ResolveBusinessTimeZone();

        private static readonly IReadOnlyDictionary<int, DayOfWeek> DayOfWeekLookup = new Dictionary<int, DayOfWeek>
        {
            { (int)DaysEnum.Saturday, DayOfWeek.Saturday },
            { (int)DaysEnum.Sunday, DayOfWeek.Sunday },
            { (int)DaysEnum.Monday, DayOfWeek.Monday },
            { (int)DaysEnum.Tuesday, DayOfWeek.Tuesday },
            { (int)DaysEnum.Wednesday, DayOfWeek.Wednesday },
            { (int)DaysEnum.Thursday, DayOfWeek.Thursday },
            { (int)DaysEnum.Friday, DayOfWeek.Friday }
        };

        private static readonly IReadOnlyDictionary<int, string> ArabicDayNameLookup = new Dictionary<int, string>
        {
            { (int)DaysEnum.Saturday, "السبت" },
            { (int)DaysEnum.Sunday, "الأحد" },
            { (int)DaysEnum.Monday, "الاثنين" },
            { (int)DaysEnum.Tuesday, "الثلاثاء" },
            { (int)DaysEnum.Wednesday, "الأربعاء" },
            { (int)DaysEnum.Thursday, "الخميس" },
            { (int)DaysEnum.Friday, "الجمعة" }
        };
        public CircleBLL(IMapper mapper, IRepository<Circle> circleRepository,
             IUnitOfWork unitOfWork,
             IHostEnvironment hostEnvironment, IRepository<ManagerCircle> managerCircleRepository, IRepository<User> userRepository, IRepository<ManagerStudent> managerStudentRepository, IRepository<CircleDay> circleDayRepository, IRepository<Day> dayRepository) : base(mapper)
        {
            _circleRepository = circleRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _managerCircleRepository = managerCircleRepository;
            _userRepository = userRepository;
            _managerStudentRepository = managerStudentRepository;
            _circleDayRepository = circleDayRepository;
            _dayRepository = dayRepository;
        }




        public IResponse<PagedResultDto<CircleDto>> GetDeletedPagedList(FilteredResultRequestDto pagedDto)
        {
            var output = new Response<PagedResultDto<CircleDto>>();
            string? searchWordLower = pagedDto.SearchTerm?.Trim()?.ToLower();

            Expression<Func<Circle, bool>> predicate = c =>
                c.IsDeleted == true
                && (searchWordLower == null || (c.Name != null && c.Name.ToLower().Contains(searchWordLower)));

            var page = GetPagedList<CircleDto, Circle, int>(
                pagedDto,
                repository: _circleRepository,
                orderExpression: x => x.Id,
                searchExpression: predicate,
                sortDirection: "DESC",
                disableFilter: true,
                excluededColumns: null,
                includeProperties: c => c.ManagerCircles, c => c.CircleDays, c => c.Users
            );

            PopulateCircleDayMetadata(page?.Items);

            return output.CreateResponse(page);
        }

        public IResponse<PagedResultDto<CircleDto>> GetPagedList(
     FilteredResultRequestDto pagedDto,
     int? managerId,
     int? teacherId,
     int userId)
        {
            var output = new Response<PagedResultDto<CircleDto>>();

            // Caller
            var me = _userRepository.GetById(userId);
            if (me == null) return output.AppendError(MessageCodes.NotFound);

            var userType = (UserTypesEnum)(me.UserTypeId ?? 0);
            string? searchWordLower = pagedDto.SearchTerm?.Trim()?.ToLower();

            // Explicit filters from query (?managerId=1220&teacherId=55); treat 0 as no filter
            int? mId = (managerId.HasValue && managerId.Value > 0) ? managerId.Value : (int?)null;
            int? tId = (teacherId.HasValue && teacherId.Value > 0) ? teacherId.Value : (int?)null;

            // Role defaults (used only when no explicit filter for that role)
            bool applyDefaultManager = (userType == UserTypesEnum.Manager) && !mId.HasValue;
            bool applyDefaultTeacher = (userType == UserTypesEnum.Teacher) && !tId.HasValue;
            bool applyStudentRestriction = (userType == UserTypesEnum.Student);
            bool applyBranchRestriction = me.BranchId.HasValue;

            // Predicate
            Expression<Func<Circle, bool>> combinedExpr = c =>
                // text search
                (searchWordLower == null || (c.Name != null && c.Name.ToLower().Contains(searchWordLower)))
                &&
                // role restrictions
                (!applyBranchRestriction || (c.BranchId.HasValue && c.BranchId == me.BranchId))
                &&
                (!applyStudentRestriction || (me.CircleId != null && c.Id == me.CircleId.Value))
                &&
                (!applyDefaultManager || c.ManagerCircles.Any(mc => mc.ManagerId == userId))
                &&
                (!applyDefaultTeacher || c.TeacherId == userId)
                &&
                // explicit filters from query
                (!mId.HasValue || c.ManagerCircles.Any(mc => mc.ManagerId == mId.Value))
                &&
                (!tId.HasValue || c.TeacherId == tId.Value);

            // Includes
            var includes = new List<Expression<Func<Circle, object>>>
            {
                c => c.ManagerCircles,
                c => c.CircleDays
            };
            if (userType != UserTypesEnum.Student)
                includes.Add(c => c.Users);

            // Page
            var page = GetPagedList<CircleDto, Circle, int>(
                pagedDto,
                repository: _circleRepository,
                orderExpression: x => x.Id,
                searchExpression: combinedExpr,
                sortDirection: "DESC",
                disableFilter: true,
                excluededColumns: null,
                includeProperties: includes.ToArray()
            );

            PopulateCircleDayMetadata(page?.Items);

            // Post-shape Students per effective role
            switch (userType)
            {
                case UserTypesEnum.Manager:
                    {
                        var managedStudentIds = _managerStudentRepository
                            .GetAll()
                            .Where(ms => ms.ManagerId == userId && ms.StudentId.HasValue)
                            .Select(ms => ms.StudentId!.Value)
                            .Distinct()
                            .ToHashSet();

                        foreach (var c in page.Items)
                            if (c.Students != null)
                                c.Students = c.Students
                                    .Where(s => managedStudentIds.Contains(s.Id) && s.CircleId == c.Id)
                                    .ToList();
                        break;
                    }
                case UserTypesEnum.Teacher:
                    {
                        var effectiveTeacherId = tId ?? userId; // use query teacherId if given, else current user
                        foreach (var c in page.Items)
                            if (c.Students != null)
                                c.Students = c.Students.Where(s => s.TeacherId == effectiveTeacherId).ToList();
                        break;
                    }
                case UserTypesEnum.Student:
                    {
                        foreach (var c in page.Items)
                        {
                            c.Students = new List<UserReturnDto>();
                            if (c.Managers != null)
                                c.Managers = c.Managers
                                    .Where(m => m.CircleId == (me.CircleId ?? 0))
                                    .ToList();
                        }
                        break;
                    }
                    // Admin/others: leave as-is
            }

            return output.CreateResponse(page);
        }


        public async Task<IResponse<CircleDto>> GetByIdAsync(int id, int userId)
        {
            var output = new Response<CircleDto>();

            var currentUser = await _userRepository.GetByIdAsync(userId);
            if (currentUser == null)
                return output.AppendError(MessageCodes.NotFound);

            var userType = (UserTypesEnum)(currentUser.UserTypeId ?? 0);

            var query = _circleRepository
                .GetAll()
                .Include(c => c.Teacher)
                .Include(c => c.ManagerCircles)
                    .ThenInclude(mc => mc.Manager)
                .Include(c => c.CircleDays)
                .Include(c => c.Users)
                .Where(c => c.IsDeleted != true);

            if (currentUser.BranchId.HasValue)
            {
                query = query.Where(c => c.BranchId.HasValue && c.BranchId == currentUser.BranchId);
            }

            switch (userType)
            {
                case UserTypesEnum.Manager:
                    query = query.Where(c => c.ManagerCircles.Any(mc => mc.ManagerId == userId));
                    break;
                case UserTypesEnum.Teacher:
                    query = query.Where(c => c.TeacherId == userId);
                    break;
                case UserTypesEnum.Student:
                    query = query.Where(c => c.Users.Any(u => u.Id == userId && u.IsDeleted != true));
                    break;
            }

            var circle = await query.FirstOrDefaultAsync(c => c.Id == id);
            if (circle == null)
                return output.AppendError(MessageCodes.NotFound);

            var dto = _mapper.Map<CircleDto>(circle);

            switch (userType)
            {
                case UserTypesEnum.Manager:
                    if (dto.Students != null)
                    {
                        var managedStudentIds = _managerStudentRepository
                            .GetAll()
                            .Where(ms => ms.ManagerId == userId && ms.StudentId.HasValue)
                            .Select(ms => ms.StudentId!.Value)
                            .Distinct()
                            .ToHashSet();

                        dto.Students = dto.Students
                            .Where(s => managedStudentIds.Contains(s.Id) && s.CircleId == dto.Id)
                            .ToList();
                    }
                    break;
                case UserTypesEnum.Teacher:
                    if (dto.Students != null)
                        dto.Students = dto.Students.Where(s => s.TeacherId == userId).ToList();
                    break;
                case UserTypesEnum.Student:
                    if (dto.Students != null)
                        dto.Students = dto.Students.Where(s => s.Id == userId).ToList();
                    if (dto.Managers != null)
                        dto.Managers = dto.Managers.Where(m => m.CircleId == (currentUser.CircleId ?? 0)).ToList();
                    break;
            }

            PopulateCircleDayMetadata(new List<CircleDto> { dto });

            return output.CreateResponse(dto);
        }


        public async Task<IResponse<IEnumerable<UpcomingCircleDto>>> GetUpcomingAsync(
            int userId,
            int? managerId = null,
            int? teacherId = null,
            int take = DefaultUpcomingTake)
        {
            var output = new Response<IEnumerable<UpcomingCircleDto>>();

            var currentUser = await _userRepository.GetByIdAsync(userId);
            if (currentUser == null)
                return output.AppendError(MessageCodes.NotFound);

            var userType = (UserTypesEnum)(currentUser.UserTypeId ?? 0);

            int? explicitManagerId = managerId.HasValue && managerId.Value > 0 ? managerId.Value : (int?)null;
            int? explicitTeacherId = teacherId.HasValue && teacherId.Value > 0 ? teacherId.Value : (int?)null;

            int effectiveTake = take > 0 ? take : DefaultUpcomingTake;

            var query = _circleRepository
                .GetAll()
                .Include(c => c.Teacher)
                .Include(c => c.ManagerCircles)
                    .ThenInclude(mc => mc.Manager)
                .Include(c => c.CircleDays)
                .Where(c => c.IsDeleted != true);

            query = ApplyUpcomingCircleFilters(query, userType, userId, currentUser.BranchId, explicitManagerId, explicitTeacherId);

            var circles = await query.ToListAsync();

            if (circles.Count == 0)
            {
                return output.CreateResponse(new List<UpcomingCircleDto>());
            }

            DateTime referenceUtc = DateTime.UtcNow;
            DateTime referenceBusinessTime = ConvertFromUtc(referenceUtc);

            var schedulesLookup = await BuildCircleSchedulesAsync(circles);

            var dayIds = schedulesLookup
                .Values
                .SelectMany(schedule => schedule ?? Array.Empty<CircleDayDto>())
                .Where(schedule => schedule != null && schedule.DayId > 0)
                .Select(schedule => schedule.DayId)
                .Distinct()
                .ToList();

            var dayLookup = await BuildDayNameLookupAsync(dayIds);

            var projected = circles
                .Select(circle =>
                {
                    schedulesLookup.TryGetValue(circle.Id, out var schedules);
                    return BuildUpcomingCircleDto(
                        circle,
                        schedules ?? Array.Empty<CircleDayDto>(),
                        referenceBusinessTime,
                        dayLookup);
                })
                .Where(dto => dto.NextOccurrenceDate.HasValue)
                .ToList();

            if (projected.Count == 0)
            {
                return output.CreateResponse(new List<UpcomingCircleDto>());
            }

            NormalizeSequentialOccurrences(projected);

            var results = projected
                .OrderBy(dto => dto.NextOccurrenceDate)
                .ThenBy(dto => dto.Id)
                .Take(effectiveTake)
                .ToList();

            return output.CreateResponse(results);
        }

        private static IQueryable<Circle> ApplyUpcomingCircleFilters(
            IQueryable<Circle> query,
            UserTypesEnum userType,
            int userId,
            int? branchId,
            int? explicitManagerId,
            int? explicitTeacherId)
        {
            if (query == null)
                return query;

            if (branchId.HasValue)
            {
                query = query.Where(c => c.BranchId.HasValue && c.BranchId == branchId.Value);
            }

            if (explicitManagerId.HasValue)
            {
                query = query.Where(c => c.ManagerCircles.Any(mc =>
                    mc.ManagerId == explicitManagerId.Value));
            }
            else if (userType == UserTypesEnum.Manager)
            {
                query = query.Where(c => c.ManagerCircles.Any(mc =>
                    mc.ManagerId == userId));
            }

            if (explicitTeacherId.HasValue)
            {
                query = query.Where(c => c.TeacherId == explicitTeacherId.Value);
            }
            else if (userType == UserTypesEnum.Teacher)
            {
                query = query.Where(c => c.TeacherId == userId);
            }

            if (userType == UserTypesEnum.Student)
            {
                query = query.Where(c => c.Users.Any(u => u.Id == userId && u.IsDeleted != true));
            }

            return query;
        }

        private UpcomingCircleDto BuildUpcomingCircleDto(
            Circle circle,
            IEnumerable<CircleDayDto> schedules,
            DateTime referenceTime,
            IReadOnlyDictionary<int, string?> dayNameLookup)
        {
            var scheduleList = schedules?
                .Where(cd => cd != null && cd.DayId > 0)
                .Select(cd => new CircleDayDto
                {
                    DayId = cd.DayId,
                    Time = cd.Time,
                    DayName = cd.DayName
                })
                .ToList() ?? new List<CircleDayDto>();

            PopulateCircleDayNames(scheduleList, dayNameLookup);

            var (nextDayId, nextOccurrence) = CalculateNextOccurrence(referenceTime, scheduleList);

            var managers = circle.ManagerCircles?
                .Where(mc => mc.ManagerId.HasValue && mc.Manager != null && mc.Manager.IsDeleted != true)
                .Select(mc => new ManagerCirclesDto
                {
                    ManagerId = mc.ManagerId,
                    Manager = mc.Manager?.FullName,
                    CircleId = circle.Id,
                    Circle = circle.Name
                })
                .ToList() ?? new List<ManagerCirclesDto>();

            var dayIds = scheduleList.Select(s => s.DayId).Distinct().ToList();

            return new UpcomingCircleDto
            {
                Id = circle.Id,
                Name = circle.Name,
                NextDayId = nextDayId,
                NextDayName = ResolveArabicDayName(nextDayId, dayNameLookup),
                DayIds = dayIds,
                DayNames = ResolveDayNames(dayIds, dayNameLookup),
                NextOccurrenceDate = nextOccurrence,
                StartTime = nextOccurrence?.TimeOfDay,
                TeacherId = circle.TeacherId,
                TeacherName = circle.Teacher?.FullName,
                Managers = managers,
                Days = scheduleList
            };
        }

        private static (int? NextDayId, DateTime? NextOccurrence) CalculateNextOccurrence(DateTime referenceTime, IEnumerable<CircleDayDto> schedules)
        {
            if (schedules == null)
                return (null, null);

            DateTime? bestDate = null;
            int? bestDayId = null;

            foreach (var schedule in schedules)
            {
                if (!schedule.Time.HasValue)
                    continue;

                var candidate = CalculateNextOccurrenceForDay(referenceTime, schedule.DayId, schedule.Time);
                if (!candidate.HasValue)
                    continue;

                if (!bestDate.HasValue || candidate.Value < bestDate.Value)
                {
                    bestDate = candidate.Value;
                    bestDayId = schedule.DayId;
                }
            }

            return (bestDayId, bestDate);
        }

        private static DateTime? CalculateNextOccurrenceForDay(DateTime referenceTime, int dayId, TimeSpan? startTime)
        {
            if (!DayOfWeekLookup.TryGetValue(dayId, out var targetDay))
                return null;

            int currentDay = (int)referenceTime.DayOfWeek;
            int targetDayValue = (int)targetDay;

            int daysToAdd = (targetDayValue - currentDay + 7) % 7;
            DateTime nextDate = referenceTime.Date.AddDays(daysToAdd);

            if (startTime.HasValue)
            {
                nextDate = nextDate.Add(startTime.Value);

                if (nextDate <= referenceTime)
                {
                    nextDate = nextDate.AddDays(7);
                }
            }

            return nextDate;
        }

        private static DateTime ConvertFromUtc(DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }

            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, BusinessTimeZone);
        }

        private static TimeZoneInfo ResolveBusinessTimeZone()
        {
            string[] preferredTimeZoneIds = new[]
            {
                "Asia/Riyadh",
                "Arab Standard Time"
            };

            foreach (var timeZoneId in preferredTimeZoneIds)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return TimeZoneInfo.Local;
        }

        private static string? ResolveDayName(int? dayId, IReadOnlyDictionary<int, string?>? dayNameLookup = null)
        {
            if (!dayId.HasValue)
                return null;

            if (dayNameLookup != null && dayNameLookup.TryGetValue(dayId.Value, out var displayName))
            {
                if (!string.IsNullOrWhiteSpace(displayName))
                    return displayName;
            }

            if (Enum.IsDefined(typeof(DaysEnum), dayId.Value))
                return ((DaysEnum)dayId.Value).ToString();

            return null;
        }

        private static string? ResolveArabicDayName(int? dayId, IReadOnlyDictionary<int, string?>? dayNameLookup = null)
        {
            if (!dayId.HasValue)
                return null;

            if (ArabicDayNameLookup.TryGetValue(dayId.Value, out var arabicName))
            {
                return arabicName;
            }

            return ResolveDayName(dayId, dayNameLookup);
        }

        private static ICollection<string> ResolveDayNames(IEnumerable<int> dayIds, IReadOnlyDictionary<int, string?>? dayNameLookup = null)
        {
            if (dayIds == null)
                return new List<string>();

            var results = new List<string>();

            foreach (var dayId in dayIds.Distinct())
            {
                var displayName = ResolveDayName(dayId, dayNameLookup);
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    results.Add(displayName);
                }
            }

            return results;
        }

        private static void PopulateCircleDayNames(ICollection<CircleDayDto>? days, IReadOnlyDictionary<int, string?> dayLookup)
        {
            if (days == null || days.Count == 0)
                return;

            foreach (var day in days)
            {
                var name = ResolveDayName(day.DayId, dayLookup);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    day.DayName = name;
                }
            }
        }

        private Task<IReadOnlyDictionary<int, IReadOnlyCollection<CircleDayDto>>> BuildCircleSchedulesAsync(IEnumerable<Circle> circles)
        {
            var result = new Dictionary<int, IReadOnlyCollection<CircleDayDto>>();

            if (circles == null)
            {
                return Task.FromResult<IReadOnlyDictionary<int, IReadOnlyCollection<CircleDayDto>>>(result);
            }

            var circleList = circles
                .Where(c => c != null)
                .ToList();

            if (circleList.Count == 0)
            {
                return Task.FromResult<IReadOnlyDictionary<int, IReadOnlyCollection<CircleDayDto>>>(result);
            }

            foreach (var circle in circleList)
            {
                var schedules = new List<CircleDayDto>();

                if (circle.CircleDays != null)
                {
                    schedules.AddRange(
                        circle.CircleDays
                            .Where(cd => cd != null && cd.DayId.HasValue && cd.DayId.Value > 0 && cd.IsDeleted != true)
                            .Select(cd => new CircleDayDto
                            {
                                DayId = cd.DayId!.Value,
                                Time = cd.Time,
                                DayName = Enum.GetName(typeof(DaysEnum), cd.DayId!.Value)
                            }));
                }

                var normalized = schedules
                    .Where(s => s.DayId > 0)
                    .GroupBy(s => new { s.DayId, TimeTicks = s.Time?.Ticks ?? -1L })
                    .Select(group =>
                    {
                        var first = group.First();
                        return new CircleDayDto
                        {
                            DayId = first.DayId,
                            Time = first.Time,
                            DayName = first.DayName
                        };
                    })
                    .ToList();

                result[circle.Id] = normalized;
            }

            return Task.FromResult<IReadOnlyDictionary<int, IReadOnlyCollection<CircleDayDto>>>(result);
        }

        private async Task<IReadOnlyDictionary<int, string?>> BuildDayNameLookupAsync(IEnumerable<int> dayIds)
        {
            if (dayIds == null)
            {
                return new Dictionary<int, string?>();
            }

            var ids = dayIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
            {
                return new Dictionary<int, string?>();
            }

            var records = await _dayRepository
                .Where(d => ids.Contains(d.Id) && d.IsDeleted != true)
                .Select(d => new { d.Id, d.NameOfDay })
                .ToListAsync();

            return records.ToDictionary(d => d.Id, d => d.NameOfDay);
        }

        private IReadOnlyDictionary<int, string?> BuildDayNameLookup(IEnumerable<int>? dayIds)
        {
            var distinctIds = dayIds?
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (distinctIds == null || distinctIds.Count == 0)
                return new Dictionary<int, string?>();

            var records = _dayRepository
                .Where(d => distinctIds.Contains(d.Id) && d.IsDeleted != true)
                .Select(d => new { d.Id, d.NameOfDay })
                .ToList();

            return records.ToDictionary(d => d.Id, d => d.NameOfDay);
        }


        private async Task<List<CircleDayRequestDto>> FilterValidCircleDaysAsync(IEnumerable<CircleDayRequestDto>? days)
        {
            if (days == null)
                return new List<CircleDayRequestDto>();

            var sanitized = days
                .Where(d => d != null && d.DayId > 0)
                .GroupBy(d => d.DayId)
                .Select(group =>
                {
                    var first = group.First();
                    return new CircleDayRequestDto
                    {
                        DayId = group.Key,
                        Time = first.Time
                    };
                })
                .ToList();

            if (sanitized.Count == 0)
                return new List<CircleDayRequestDto>();

            var ids = sanitized
                .Select(x => x.DayId)
                .ToList();

        

            return sanitized.ToList();
        }

        private static List<CircleDay> BuildCircleDayLinks(int circleId, IEnumerable<CircleDayRequestDto> days, int userId, DateTime? createdAt = null)
        {
            var timestamp = createdAt ?? DateTime.UtcNow;

            return days
                .Select(day => new CircleDay
                {
                    CircleId = circleId,
                    DayId = day.DayId,
                    Time = day.Time
                  
                })
                .ToList();
        }

        private void SyncCircleDayLinks(Circle entity, IReadOnlyCollection<CircleDayRequestDto> incomingDays, int userId)
        {
            var existingCircleDays = entity.CircleDays?.ToList() ?? new List<CircleDay>();
            var incomingLookup = incomingDays
                .Where(day => day.DayId > 0)
                .ToDictionary(day => day.DayId, day => day);

            var toRemove = existingCircleDays
                .Where(cd => !cd.DayId.HasValue || !incomingLookup.ContainsKey(cd.DayId.Value))
                .ToList();

            if (toRemove.Count > 0)
            {
                _circleDayRepository.DeleteRange(toRemove);

                if (entity.CircleDays != null)
                {
                    foreach (var remove in toRemove)
                    {
                        entity.CircleDays.Remove(remove);
                    }
                }
            }

            foreach (var circleDay in existingCircleDays)
            {
                if (circleDay.DayId.HasValue && incomingLookup.TryGetValue(circleDay.DayId.Value, out var incoming))
                {
                    circleDay.Time = incoming.Time;
                  
                }
            }

            var existingDayIds = new HashSet<int>(
                existingCircleDays
                    .Where(cd => cd.DayId.HasValue)
                    .Select(cd => cd.DayId!.Value));

            var toAdd = incomingLookup
                .Where(kvp => !existingDayIds.Contains(kvp.Key))
                .Select(kvp => kvp.Value)
                .ToList();

            if (toAdd.Count > 0)
            {
                var newLinks = BuildCircleDayLinks(entity.Id, toAdd, userId);

                _circleDayRepository.Add(newLinks);

                if (entity.CircleDays == null)
                {
                    entity.CircleDays = new List<CircleDay>(newLinks);
                }
                else
                {
                    foreach (var link in newLinks)
                    {
                        entity.CircleDays.Add(link);
                    }
                }
            }
        }

        private void PopulateCircleDayMetadata(IEnumerable<CircleDto>? circles)
        {
            if (circles == null)
                return;

            var circleList = circles
                .Where(c => c.Days != null && c.Days.Any())
                .ToList();

            if (circleList.Count == 0)
                return;

            var dayLookup = BuildDayNameLookup(
                circleList
                    .SelectMany(c => c.Days!)
                    .Select(d => d.DayId)
                    .Distinct());

            foreach (var circle in circleList)
            {
                if (circle.Days == null)
                    continue;

                PopulateCircleDayNames(circle.Days, dayLookup);
            }
     
        }

        private static void NormalizeSequentialOccurrences(IList<UpcomingCircleDto> circles)
        {
            if (circles == null || circles.Count == 0)
                return;

            var ordered = circles
                .Where(c => c.NextOccurrenceDate.HasValue)
                .OrderBy(c => c.NextOccurrenceDate)
                .ThenBy(c => c.Id)
                .ToList();

            var occurrenceTracker = new Dictionary<(string NameKey, int DayKey, int TeacherKey, long? TimeKey), int>();

            foreach (var circle in ordered)
            {
                if (!circle.NextDayId.HasValue)
                    continue;

                string nameKey = circle.Name?.Trim().ToLowerInvariant() ?? string.Empty;
                int dayKey = circle.NextDayId.Value;
                int teacherKey = circle.TeacherId ?? 0;
                long? timeKey = circle.StartTime?.Ticks;
                var key = (NameKey: nameKey, DayKey: dayKey, TeacherKey: teacherKey, TimeKey: timeKey);

                if (occurrenceTracker.TryGetValue(key, out var seenCount))
                {
                    circle.NextOccurrenceDate = circle.NextOccurrenceDate!.Value.AddDays(7 * seenCount);
                    occurrenceTracker[key] = seenCount + 1;
                }
                else
                {
                    occurrenceTracker[key] = 1;
                }
            }
        }


        public async Task<IResponse<bool>> AddAsync(CreateCircleDto model, int userId)
        {
            var output = new Response<bool>();

            // 1) Validate DTO
            var validator = new CircleValidation();
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                return output.AppendErrors(validationResult.Errors);

            var currentUser = await _userRepository.GetByIdAsync(userId);
            if (currentUser == null) return output.AppendError(MessageCodes.NotFound);

            // 2) Name unique?
            if (await _circleRepository.AnyAsync(x => x.Name!.Trim().ToLower() == model.Name!.Trim().ToLower()))
                return output.CreateResponse(MessageCodes.NameAlreadyExists);

            bool hasIncomingDays = model.Days != null && model.Days.Count > 0;
            var validDays = await FilterValidCircleDaysAsync(model.Days);

            if (!hasIncomingDays)
            {
                return output.AppendError(MessageCodes.InputValidationError, nameof(model.Days), CircleValidationResponseConstants.DaysRequired);
            }

            if (validDays.Count == 0)
            {
                return output.AppendError(MessageCodes.InputValidationError, nameof(model.Days), CircleValidationResponseConstants.DayRequired);
            }


            // 4) Map & create the circle
            var entity = _mapper.Map<CreateCircleDto, Circle>(model);
            entity.CreatedBy = userId;
            entity.CreatedAt = DateTime.UtcNow;
            entity.IsDeleted = false;
            var branchId = model.BranchId ?? currentUser.BranchId;
            if (branchId.HasValue)
            {
                entity.BranchId = branchId;
            }


            // 4a) Save circle to get the generated Id
            var created = await _circleRepository.AddAsync(entity);
            await _unitOfWork.CommitAsync(); // after this, created.Id is available
            if (currentUser.UserTypeId == (int)UserTypesEnum.Manager)
            {
                model.Managers ??= new List<int>();
                if (!model.Managers.Contains(userId))
                {
                    model.Managers.Add(userId);
                }
            }

            // 5) Add managers (now that we have circle Id)
            if (model.Managers != null && model.Managers.Count > 0)
            {


                // Or create directly:
                created.ManagerCircles = model.Managers
                    .Distinct()
                    .Select(mgrId => new ManagerCircle
                    {
                        CircleId = created.Id,
                        ManagerId = mgrId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId
                    })
                    .ToList();
            }

            // 6) Link students to this circle (set Users.CircleId = created.Id)
            if (model.StudentsIds != null && model.StudentsIds.Count > 0)
            {
                
                var students =  _userRepository
                    .Where(u =>
                        model.StudentsIds.Contains(u.Id) &&
                        !u.IsDeleted  &&
                u.UserTypeId == (int)UserTypesEnum.Student);


                foreach (var s in students)
                {
                    s.CircleId = created.Id;
                    s.ModefiedAt = DateTime.UtcNow;
                    s.ModefiedBy = userId;
                }
            }

            if (validDays.Count > 0)
            {
                var circleDays = BuildCircleDayLinks(created.Id, validDays, userId);
                created.CircleDays = circleDays;
                _circleDayRepository.Add(circleDays);
            }

            // 7) Persist managers + students updates
            await _unitOfWork.CommitAsync();

            // await _unitOfWork.CommitTransactionAsync();

            return output.CreateResponse(data: true);
        }

        public async Task<IResponse<bool>> Update(UpdateCircleDto dto, int userId)
        {
            var output = new Response<bool>();
            var validator = new CircleUpdateValidation();
            var vr = validator.Validate(dto);
            if (!vr.IsValid) return output.AppendErrors(vr.Errors);

            bool nameExists = await _circleRepository.AnyAsync(
                x => x.Name!.Trim().ToLower() == dto.Name!.Trim().ToLower() && x.Id != dto.Id);
            if (nameExists) return output.CreateResponse(MessageCodes.NameAlreadyExists);

            var entity = _circleRepository.GetById(dto.Id);
            if (entity == null) return output.AppendError(MessageCodes.NotFound);
            var User = await _userRepository.GetByIdAsync(userId);
            if (User == null) return output.AppendError(MessageCodes.NotFound);

            if (dto.Days != null)
            {
                var validDays = await FilterValidCircleDaysAsync(dto.Days);

                if (validDays.Count == 0)
                {
                    return output.AppendError(MessageCodes.InputValidationError, nameof(dto.Days), CircleValidationResponseConstants.DayRequired);
                }

                SyncCircleDayLinks(entity, validDays, userId);
            }

            // Update managers (replace all with incoming if provided)
            if (dto.Managers != null && dto.Managers.Count > 0)
            {


                // Clean & distinct (ignore zeros if they sometimes sneak in)
                var targetIds = dto.Managers.Where(id => id > 0).Distinct().ToList();
                var targetSet = new HashSet<int>(targetIds);

                // Delete existing links for this circle where ManagerId is in dto.Managers
                if (entity.ManagerCircles?.Any() == true)
                {
                    var toDelete = entity.ManagerCircles
                       .Where(x => x.CircleId == dto.Id && targetSet.Contains(x.ManagerId.Value))
                       .ToList();
                    if (User.UserTypeId == (int)UserTypesEnum.Admin)
                    {
                         toDelete = entity.ManagerCircles
                        .Where(x => x.CircleId == dto.Id )
                        .ToList();
                    }
                   

                    if (toDelete.Count > 0)
                        _managerCircleRepository.DeleteRange(toDelete);
                }

                // Recreate links for provided managers
                if (targetIds.Count > 0)
                    _managerCircleRepository.Add(addCirclesManagers(dto.Id, targetIds,userId));
            }

            // ===== Students sync =====
            // If dto.StudentsIds == null => leave students untouched.
            // If dto.StudentsIds is provided (even empty) => treat as the new source of truth.
            if (dto.StudentsIds != null)
            {
                var incomingIds = dto.StudentsIds.Distinct().ToList();

                var currentStudents =  _userRepository.Where(u =>
                    u.CircleId == dto.Id && !u.IsDeleted &&
                    u.UserTypeId == (int)UserTypesEnum.Student);

                var currentIds = currentStudents.Select(s => s.Id).ToList();

                var toAdd = incomingIds.Except(currentIds).ToList();
                var toRemove = currentIds.Except(incomingIds).ToList();

                if (toAdd.Count > 0)
                {
                    var addStudents =  _userRepository.Where(u =>
                        toAdd.Contains(u.Id) && !u.IsDeleted &&
                        u.UserTypeId == (int)UserTypesEnum.Student);

                    foreach (var s in addStudents)
                    {
                        s.CircleId = dto.Id;
                        s.ModefiedAt = DateTime.UtcNow;
                        s.ModefiedBy = userId;
                    }
                }

                foreach (var s in currentStudents.Where(s => toRemove.Contains(s.Id)))
                {
                    s.CircleId = null;
                    s.ModefiedAt = DateTime.UtcNow;
                    s.ModefiedBy = userId;
                }   
            
        }

            // Update circle fields
            entity.ModefiedAt = DateTime.UtcNow;
            entity.ModefiedBy = userId;
            _mapper.Map(dto, entity);
            var branchId = dto.BranchId ?? User.BranchId;
            if (branchId.HasValue)
            {
                entity.BranchId = branchId;
            }

            await _unitOfWork.CommitAsync();
            return output.CreateResponse(data: true);
        }

       

        public async Task<IResponse<bool>> DeleteAsync(int id ,int userId)
        {
            Response<bool> output = new Response<bool>();

            Circle entity = await _circleRepository.GetByIdAsync(id);
            if (entity == null)
                return output.AppendError(MessageCodes.NotFound);
            entity.ModefiedBy = userId;
            entity.ModefiedAt = DateTime.UtcNow;
            entity.IsDeleted = true;
            await _unitOfWork.CommitAsync();
            return output.CreateResponse(data: true);

        }
        private List<ManagerCircle> addCirclesManagers(int circleId, List<int>? managerIdS,int userId)
        {
            List<ManagerCircle> ManagerCirclelist = new List<ManagerCircle>();
            foreach (var managerId in managerIdS!)
            {
                ManagerCirclelist.Add(new ManagerCircle
                {
                    CircleId = circleId,
                    ManagerId = managerId,
                    ModefiedAt = DateTime.UtcNow,
                    ModefiedBy= userId
                    
                });
            }
            return ManagerCirclelist;
        }
    }

    }
