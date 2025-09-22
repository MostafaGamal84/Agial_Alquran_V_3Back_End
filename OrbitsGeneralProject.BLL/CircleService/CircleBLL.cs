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
        private readonly IRepository<CircleDay> _circleDayRepository;
        private readonly IRepository<Day> _dayRepository;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        private const int DefaultUpcomingTake = 4;

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
        public CircleBLL(IMapper mapper, IRepository<Circle> circleRepository,
             IUnitOfWork unitOfWork,
             IHostEnvironment hostEnvironment, IRepository<ManagerCircle> managerCircleRepository, IRepository<User> userRepository, IRepository<CircleDay> circleDayRepository, IRepository<Day> dayRepository) : base(mapper)
        {
            _circleRepository = circleRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _managerCircleRepository = managerCircleRepository;
            _userRepository = userRepository;
            _circleDayRepository = circleDayRepository;
            _dayRepository = dayRepository;
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

            // Predicate
            Expression<Func<Circle, bool>> combinedExpr = c =>
                // text search
                (searchWordLower == null || (c.Name != null && c.Name.ToLower().Contains(searchWordLower)))
                &&
                // role restrictions
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

            if (page?.Items != null && page.Items.Count > 0)
            {
                var dayLookup = BuildDayNameLookup(
                    page.Items
                        .Where(c => c.DayIds != null)
                        .SelectMany(c => c.DayIds!)
                        .Distinct());

                foreach (var circle in page.Items)
                {
                    if (circle.DayIds != null)
                    {
                        circle.DayNames = ResolveDayNames(circle.DayIds, dayLookup);
                    }
                }
            }

            // Post-shape Students per effective role
            switch (userType)
            {
                case UserTypesEnum.Manager:
                    {
                        var effectiveManagerId = mId ?? userId; // use query managerId if given, else current user
                        foreach (var c in page.Items)
                            if (c.Students != null)
                                c.Students = c.Students.Where(s => s.ManagerId == effectiveManagerId).ToList();
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
                            if (me.ManagerId.HasValue && c.Managers != null)
                                c.Managers = c.Managers
                                    .Where(m => m.ManagerId == me.ManagerId.Value && m.CircleId == (me.CircleId ?? 0))
                                    .ToList();
                        }
                        break;
                    }
                    // Admin/others: leave as-is
            }

            return output.CreateResponse(page);
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

            if (explicitManagerId.HasValue)
            {
                query = query.Where(c => c.ManagerCircles.Any(mc => mc.ManagerId == explicitManagerId.Value));
            }
            else if (userType == UserTypesEnum.Manager)
            {
                query = query.Where(c => c.ManagerCircles.Any(mc => mc.ManagerId == userId));
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
                query = query.Where(c => c.Users.Any(u => u.Id == userId));
            }

            var circles = await query.ToListAsync();

            if (circles.Count == 0)
            {
                return output.CreateResponse(new List<UpcomingCircleDto>());
            }

            DateTime referenceUtc = DateTime.UtcNow;

            var dayLookup = await BuildDayNameLookupAsync(circles);

            var projected = circles
                .Select(circle => BuildUpcomingCircleDto(circle, referenceUtc, dayLookup))
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

        private UpcomingCircleDto BuildUpcomingCircleDto(Circle circle, DateTime referenceUtc, IReadOnlyDictionary<int, string?> dayNameLookup)
        {
            var dayIds = circle.CircleDays?
                .Where(cd => cd.DayId.HasValue)
                .Select(cd => cd.DayId!.Value)
                .Distinct()
                .ToList() ?? new List<int>();

            var (nextDayId, nextOccurrence) = CalculateNextOccurrence(referenceUtc, dayIds, circle.StartTime);

            var managers = circle.ManagerCircles?
                .Where(mc => mc.ManagerId.HasValue && mc.Manager != null)
                .Select(mc => new ManagerCirclesDto
                {
                    ManagerId = mc.ManagerId,
                    Manager = mc.Manager?.FullName,
                    CircleId = circle.Id,
                    Circle = circle.Name
                })
                .ToList() ?? new List<ManagerCirclesDto>();

            return new UpcomingCircleDto
            {
                Id = circle.Id,
                Name = circle.Name,
                NextDayId = nextDayId,
                NextDayName = ResolveDayName(nextDayId, dayNameLookup),
                DayIds = dayIds,
                DayNames = ResolveDayNames(dayIds, dayNameLookup),
                NextOccurrenceDate = nextOccurrence,
                StartTime = circle.StartTime,
                TeacherId = circle.TeacherId,
                TeacherName = circle.Teacher?.FullName,
                Managers = managers
            };
        }

        private static (int? NextDayId, DateTime? NextOccurrence) CalculateNextOccurrence(DateTime referenceUtc, IEnumerable<int> dayIds, TimeSpan? startTime)
        {
            if (dayIds == null)
                return (null, null);

            DateTime? bestDate = null;
            int? bestDayId = null;

            foreach (var dayId in dayIds)
            {
                var candidate = CalculateNextOccurrenceForDay(referenceUtc, dayId, startTime);
                if (!candidate.HasValue)
                    continue;

                if (!bestDate.HasValue || candidate.Value < bestDate.Value)
                {
                    bestDate = candidate.Value;
                    bestDayId = dayId;
                }
            }

            return (bestDayId, bestDate);
        }

        private static DateTime? CalculateNextOccurrenceForDay(DateTime referenceUtc, int dayId, TimeSpan? startTime)
        {
            if (!DayOfWeekLookup.TryGetValue(dayId, out var targetDay))
                return null;

            int currentDay = (int)referenceUtc.DayOfWeek;
            int targetDayValue = (int)targetDay;

            int daysToAdd = (targetDayValue - currentDay + 7) % 7;
            DateTime nextDate = referenceUtc.Date.AddDays(daysToAdd);

            if (startTime.HasValue)
            {
                nextDate = nextDate.Add(startTime.Value);

                if (nextDate <= referenceUtc)
                {
                    nextDate = nextDate.AddDays(7);
                }
            }

            return nextDate;
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

        private async Task<IReadOnlyDictionary<int, string?>> BuildDayNameLookupAsync(IEnumerable<Circle> circles)
        {
            if (circles == null)
                return new Dictionary<int, string?>();

            var ids = circles
                .SelectMany(circle => circle.CircleDays?
                    .Where(cd => cd.DayId.HasValue)
                    .Select(cd => cd.DayId!.Value) ?? Enumerable.Empty<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                return new Dictionary<int, string?>();

            var records = await _dayRepository
                .Where(d => ids.Contains(d.Id) && d.IsDeleted != true)
                .Select(d => new { d.Id, d.NameOfDay })
                .ToListAsync();

            return records.ToDictionary(d => d.Id, d => d.NameOfDay);
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

            // 2) Name unique?
            if (await _circleRepository.AnyAsync(x => x.Name!.Trim().ToLower() == model.Name!.Trim().ToLower()))
                return output.CreateResponse(MessageCodes.NameAlreadyExists);

            List<int> validDayIds = new List<int>();
            if (model.DayIds != null && model.DayIds.Count > 0)
            {
                validDayIds = await _dayRepository
                    .Where(d => model.DayIds.Contains(d.Id) && d.IsDeleted != true)
                    .Select(d => d.Id)
                    .ToListAsync();

                if (validDayIds.Count == 0)
                {
                    return output.AppendError(MessageCodes.InputValidationError, nameof(model.DayIds), CircleValidationResponseConstants.DayRequired);
                }
            }
            else
            {
                return output.AppendError(MessageCodes.InputValidationError, nameof(model.DayIds), CircleValidationResponseConstants.DaysRequired);
            }


            // 4) Map & create the circle
            var entity = _mapper.Map<CreateCircleDto, Circle>(model);
            entity.CreatedBy = userId;
            entity.CreatedAt = DateTime.UtcNow;
            entity.IsDeleted = false;


            // 4a) Save circle to get the generated Id
            var created = await _circleRepository.AddAsync(entity);
            var User = await _userRepository.GetByIdAsync(userId);
            await _unitOfWork.CommitAsync(); // after this, created.Id is available
            if (User.UserTypeId == (int)UserTypesEnum.Manager)
            {
                model.Managers.Add(userId);
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

            if (validDayIds.Count > 0)
            {
                var circleDays = validDayIds
                    .Distinct()
                    .Select(dayId => new CircleDay
                    {
                        CircleId = created.Id,
                        DayId = dayId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId
                    })
                    .ToList();

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

            if (dto.DayIds != null)
            {
                var incomingDayIds = await _dayRepository
                    .Where(d => dto.DayIds.Contains(d.Id) && d.IsDeleted != true)
                    .Select(d => d.Id)
                    .ToListAsync();

                if (incomingDayIds.Count == 0)
                {
                    return output.AppendError(MessageCodes.InputValidationError, nameof(dto.DayIds), CircleValidationResponseConstants.DayRequired);
                }

                var existingCircleDays = entity.CircleDays?.ToList() ?? new List<CircleDay>();

                var toRemove = existingCircleDays
                    .Where(cd => !cd.DayId.HasValue || !incomingDayIds.Contains(cd.DayId.Value))
                    .ToList();

                if (toRemove.Count > 0)
                {
                    _circleDayRepository.DeleteRange(toRemove);
                }

                var existingDayIds = new HashSet<int>(existingCircleDays
                    .Where(cd => cd.DayId.HasValue)
                    .Select(cd => cd.DayId!.Value));

                var toAdd = incomingDayIds
                    .Where(dayId => !existingDayIds.Contains(dayId))
                    .Select(dayId => new CircleDay
                    {
                        CircleId = dto.Id,
                        DayId = dayId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId
                    })
                    .ToList();

                if (toAdd.Count > 0)
                {
                    _circleDayRepository.Add(toAdd);
                }
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
