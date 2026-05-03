using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.Helpers;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.BLL.Validation.CircleValidation;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Enums;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO.CircleDto;
using Orbits.GeneralProject.DTO.LockUpDtos;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.StudentSubscribDtos;
using Orbits.GeneralProject.Repositroy.Base;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Orbits.GeneralProject.BLL.StudentSubscribeService
{
    public class StudentSubscribeBLL : BaseBLL, IStudentSubscribeBLL
    {
        private readonly IMapper _mapper;
        private readonly IRepository<User> _UserRepo;
        private readonly IRepository<StudentSubscribe> _StudentSubscribeRepo;
        private readonly IRepository<StudentSubscribeHistory> _StudentSubscribeHistoryRepo;
        private readonly IRepository<StudentPayment> _StudentPaymentRepo;
        private readonly IRepository<Subscribe> _SubscribeRepo;
        private readonly IRepository<SubscribeType> _SubscribeTypeRepo;
        private readonly IRepository<CircleReport> _CircleReportRepo;
        private readonly IRepository<Nationality> _nationalityRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<ManagerStudent> _managerStudentRepo;
        private readonly ILogger<StudentSubscribeBLL> _logger;


        public StudentSubscribeBLL(IMapper mapper, IRepository<User> UserRepo, IRepository<StudentSubscribe> studentSubscribeRepo, IRepository<StudentSubscribeHistory> studentSubscribeHistoryRepo, IRepository<Subscribe> subscribeRepo, IRepository<SubscribeType> subscribeTypeRepo, IRepository<StudentPayment> studentPaymentRepo, IRepository<CircleReport> circleReportRepo, IRepository<Nationality> nationalityRepo, IUnitOfWork unitOfWork, IRepository<ManagerStudent> managerStudentRepo, ILogger<StudentSubscribeBLL> logger) : base(mapper)
        {
            _mapper = mapper;
            _UserRepo = UserRepo;
            _StudentSubscribeRepo = studentSubscribeRepo;
            _StudentSubscribeHistoryRepo = studentSubscribeHistoryRepo;
            _SubscribeRepo = subscribeRepo;
            _SubscribeTypeRepo = subscribeTypeRepo;
            _StudentPaymentRepo = studentPaymentRepo;
            _CircleReportRepo = circleReportRepo;
            _nationalityRepo = nationalityRepo;
            _unitOfWork = unitOfWork;
            _managerStudentRepo = managerStudentRepo;
            _logger = logger;
        }




        public IResponse<PagedResultDto<ViewStudentSubscribeReDto>> GetStudents(
     FilteredResultRequestDto pagedDto, int userId,int? studentId, int? nationalityId)
        {
            var output = new Response<PagedResultDto<ViewStudentSubscribeReDto>>();
            var searchWord = pagedDto.SearchTerm?.Trim();
            var me = _UserRepo.GetById(userId);
            if (me == null) return output.AppendError(MessageCodes.NotFound);

            var sw = searchWord?.ToLower();
            var residentGroup = ResidentGroupFilterHelper.Parse(pagedDto?.ResidentGroup);
            var residentIdsFilter = ResidentGroupFilterHelper.ResolveResidentIds(_nationalityRepo.GetAll(), residentGroup);
            bool applyResidentFilter = residentIdsFilter != null;
            var managerStudentsQuery = _managerStudentRepo.GetAll();

            // Build ONE predicate that includes:
            // - target user type (userTypeId)
            // - role-based restrictions (branch/manager/teacher)
            // - optional text search
            Expression<Func<User, bool>> predicate = x =>
                x.UserTypeId == (int)UserTypesEnum.Student
                // role-based restriction (applies only when the logged-in role matches)
                && (!(studentId.HasValue && studentId.Value > 0) || x.Id == studentId.Value)
                && (!(nationalityId.HasValue && nationalityId.Value > 0) || x.NationalityId == nationalityId.Value)
                && (!(me.UserTypeId == (int)UserTypesEnum.BranchLeader) || x.BranchId == me.BranchId)
                && (!(me.UserTypeId == (int)UserTypesEnum.Manager) || managerStudentsQuery.Any(ms => ms.ManagerId == me.Id && ms.StudentId == x.Id))
                && (!(me.UserTypeId == (int)UserTypesEnum.Teacher) || x.TeacherId == me.Id)
                && (!applyResidentFilter || (x.ResidentId.HasValue && residentIdsFilter!.Contains(x.ResidentId.Value)))
                // optional search (grouped to avoid &&/|| precedence issues)
                && (
                    string.IsNullOrEmpty(sw) ||
                    (x.FullName != null && x.FullName.ToLower().Contains(sw)) ||
                    (x.Mobile != null && x.Mobile.ToLower().Contains(sw)) ||
                    (x.Email != null && x.Email.ToLower().Contains(sw)) ||
                    (x.Nationality != null && x.Nationality.Name != null && x.Nationality.Name.ToLower().Contains(sw)) ||
                    (x.Governorate != null && x.Governorate.Name != null && x.Governorate.Name.ToLower().Contains(sw))
                );

            // IMPORTANT: pass the predicate to GetPagedList so filtering happens before paging
            var paged = GetPagedList<ViewStudentSubscribeReDto, User, int>(
                pagedDto,
                _UserRepo,
                x => x.Id,               // positional key selector
                searchExpression: predicate,
                sortDirection: "DESC",
                disableFilter: true,
                excluededColumns: null
            );

            //// If you want NotFound when there are no items:
            //if (paged == null || paged.Items == null || paged.Items.Count == 0)
            //    return output.CreateResponse(MessageCodes.NotFound);

            return output.CreateResponse(paged);
        }

        public IResponse<PagedResultDto<ViewStudentSubscribeReDto>> GetStudentSubscribesWithPayment(
    FilteredResultRequestDto pagedDto,  int? studentId, int? nationalityId)
        {
            var output = new Response<PagedResultDto<ViewStudentSubscribeReDto>>();
            var searchWord = pagedDto.SearchTerm?.Trim();

            var sw = searchWord?.ToLower();
            var residentGroup = ResidentGroupFilterHelper.Parse(pagedDto?.ResidentGroup);
            var residentIdsFilter = ResidentGroupFilterHelper.ResolveResidentIds(_nationalityRepo.GetAll(), residentGroup);
            bool applyResidentFilter = residentIdsFilter != null;
            var managerStudentsQuery = _managerStudentRepo.GetAll();

            // Build ONE predicate that includes:
            // - target user type (userTypeId)
            // - role-based restrictions (branch/manager/teacher)
            // - optional text search
            Expression<Func<StudentSubscribe, bool>> predicate = x =>
                x.StudentPaymentId != null
               
                // role-based restriction (applies only when the logged-in role matches)
                && (!(studentId.HasValue && studentId.Value > 0) || x.StudentId == studentId.Value)
                && (!(nationalityId.HasValue && nationalityId.Value > 0) || (x.Student != null && x.Student.NationalityId == nationalityId.Value))
                && (!applyResidentFilter || (x.Student != null && x.Student.ResidentId.HasValue && residentIdsFilter!.Contains(x.Student.ResidentId.Value)))
               
                // optional search (grouped to avoid &&/|| precedence issues)
                && (
                    string.IsNullOrEmpty(sw) 
                    //(x.FullName != null && x.FullName.ToLower().Contains(sw)) ||
                    //(x.Mobile != null && x.Mobile.ToLower().Contains(sw)) ||
                    //(x.Email != null && x.Email.ToLower().Contains(sw)) ||
                    //(x.Nationality != null && x.Nationality.Name != null && x.Nationality.Name.ToLower().Contains(sw)) ||
                    //(x.Governorate != null && x.Governorate.Name != null && x.Governorate.Name.ToLower().Contains(sw))
                );

            // IMPORTANT: pass the predicate to GetPagedList so filtering happens before paging
            var paged = GetPagedList<ViewStudentSubscribeReDto, StudentSubscribe, int>(
                pagedDto,
                _StudentSubscribeRepo,
                x => x.Id,               // positional key selector
                searchExpression: predicate,
                sortDirection: "DESC",
                disableFilter: true,
                excluededColumns: null
            );

            PopulateMonthlyConsumption(paged?.Items);

            //// If you want NotFound when there are no items:
            //if (paged == null || paged.Items == null || paged.Items.Count == 0)
            //    return output.CreateResponse(MessageCodes.NotFound);

            return output.CreateResponse(paged);
        }

        public IResponse<PagedResultDto<ViewStudentSubscribeReDto>> GetActiveStudentsBySubscribe(
            FilteredResultRequestDto pagedDto,
            int userId,
            int subscribeId,
            int? nationalityId)
        {
            var output = new Response<PagedResultDto<ViewStudentSubscribeReDto>>();

            if (subscribeId <= 0)
            {
                return output.CreateResponse(new PagedResultDto<ViewStudentSubscribeReDto>(0, new List<ViewStudentSubscribeReDto>()));
            }

            var me = _UserRepo.GetById(userId);
            if (me == null)
            {
                return output.AppendError(MessageCodes.NotFound);
            }

            var searchWord = pagedDto.SearchTerm?.Trim();
            var sw = searchWord?.ToLower();
            var residentGroup = ResidentGroupFilterHelper.Parse(pagedDto?.ResidentGroup);
            var residentIdsFilter = ResidentGroupFilterHelper.ResolveResidentIds(_nationalityRepo.GetAll(), residentGroup);
            bool applyResidentFilter = residentIdsFilter != null;
            var managerStudentsQuery = _managerStudentRepo.GetAll();

            var baseQuery = _StudentSubscribeRepo
                .GetAll()
                .AsNoTracking()
                .Where(x =>
                    x.StudentId.HasValue &&
                    x.StudentSubscribeId.HasValue &&
                    x.Student != null &&
                    x.Student.IsDeleted == false &&
                    x.Student.UserTypeId == (int)UserTypesEnum.Student &&
                    (x.StudentPayment == null || x.StudentPayment.IsCancelled != true) &&
                    (!(nationalityId.HasValue && nationalityId.Value > 0) || x.Student.NationalityId == nationalityId.Value) &&
                    (!(me.UserTypeId == (int)UserTypesEnum.BranchLeader) || x.Student.BranchId == me.BranchId) &&
                    (!(me.UserTypeId == (int)UserTypesEnum.Manager) || managerStudentsQuery.Any(ms => ms.ManagerId == me.Id && ms.StudentId == x.StudentId)) &&
                    (!(me.UserTypeId == (int)UserTypesEnum.Teacher) || x.Student.TeacherId == me.Id) &&
                    (!applyResidentFilter || (x.Student.ResidentId.HasValue && residentIdsFilter!.Contains(x.Student.ResidentId.Value))) &&
                    (
                        string.IsNullOrEmpty(sw) ||
                        (x.Student.FullName != null && x.Student.FullName.ToLower().Contains(sw)) ||
                        (x.Student.Mobile != null && x.Student.Mobile.ToLower().Contains(sw)) ||
                        (x.Student.Email != null && x.Student.Email.ToLower().Contains(sw))
                    ));

            var latestSubscriptionIdsQuery = baseQuery
                .GroupBy(x => x.StudentId!.Value)
                .Select(group => group
                    .OrderByDescending(x => x.CreatedAt ?? DateTime.MinValue)
                    .ThenByDescending(x => x.Id)
                    .Select(x => x.Id)
                    .First());

            var latestSubscriptionsQuery = _StudentSubscribeRepo
                .GetAll()
                .AsNoTracking()
                .Include(x => x.Student)
                .Include(x => x.StudentSubscribeNavigation)
                .Include(x => x.StudentSubscribeType)
                .Where(x =>
                    latestSubscriptionIdsQuery.Contains(x.Id) &&
                    x.StudentSubscribeId == subscribeId);

            int totalCount = latestSubscriptionsQuery.Count();

            var items = latestSubscriptionsQuery
                .OrderBy(x => x.Student != null ? x.Student.FullName : string.Empty)
                .ThenByDescending(x => x.CreatedAt ?? DateTime.MinValue)
                .Skip(pagedDto.SkipCount)
                .Take(pagedDto.MaxResultCount)
                .ToList();

            var mappedItems = _mapper.Map<List<ViewStudentSubscribeReDto>>(items);
            return output.CreateResponse(new PagedResultDto<ViewStudentSubscribeReDto>(totalCount, mappedItems));
        }

        public IResponse<PagedResultDto<StudentSubscribeHistoryReDto>> GetStudentSubscribeHistory(
            FilteredResultRequestDto pagedDto,
            int? studentId)
        {
            var output = new Response<PagedResultDto<StudentSubscribeHistoryReDto>>();
            var searchWord = pagedDto.SearchTerm?.Trim();
            var sw = searchWord?.ToLower();

            pagedDto.SortBy = string.IsNullOrWhiteSpace(pagedDto.SortBy)
                ? nameof(StudentSubscribeHistory.CreatedAt)
                : pagedDto.SortBy;

            try
            {
                Expression<Func<StudentSubscribeHistory, bool>> predicate = x =>
                    (!(studentId.HasValue && studentId.Value > 0) || x.StudentId == studentId.Value)
                    && (
                        string.IsNullOrEmpty(sw)
                        || (x.ActionType != null && x.ActionType.ToLower().Contains(sw))
                        || (x.OldPlanName != null && x.OldPlanName.ToLower().Contains(sw))
                        || (x.NewPlanName != null && x.NewPlanName.ToLower().Contains(sw))
                        || (x.CreatedByUser != null && x.CreatedByUser.FullName != null && x.CreatedByUser.FullName.ToLower().Contains(sw))
                    );

                var paged = GetPagedList<StudentSubscribeHistoryReDto, StudentSubscribeHistory, int>(
                    pagedDto,
                    _StudentSubscribeHistoryRepo,
                    x => x.Id,
                    predicate,
                    sortDirection: "DESC",
                    disableFilter: true,
                    excluededColumns: null,
                    x => x.CreatedByUser);

                return output.CreateResponse(paged);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Student subscription history could not be loaded for StudentId: {StudentId}",
                    studentId);

                return output.CreateResponse(new PagedResultDto<StudentSubscribeHistoryReDto>(0, new List<StudentSubscribeHistoryReDto>()));
            }
        }

        public async Task<IResponse<bool>> AddAsync(AddStudentSubscribeDto model, int? userId)
        {
            var output = new Response<bool>();
            if (model?.StudentId == null || model.StudentSubscribeId == null)
            {
                return output.AppendError(
                    MessageCodes.InputValidationError,
                    nameof(AddStudentSubscribeDto.StudentSubscribeId),
                    "StudentId and StudentSubscribeId are required.");
            }

            var subscribe = _SubscribeRepo.GetById(model.StudentSubscribeId.Value);
            if (subscribe == null)
            {
                return output.AppendError(MessageCodes.NotFound);
            }

            var student = _UserRepo.GetById(model.StudentId.Value);
            if (student == null)
            {
                return output.AppendError(MessageCodes.NotFound);
            }

            var subscribeGroup = ResolveSubscribeGroup(subscribe);
            if (!subscribeGroup.HasValue)
            {
                return output.AppendError(
                    MessageCodes.InputValidationError,
                    nameof(AddStudentSubscribeDto.StudentSubscribeId),
                    "Subscribe type group is not configured for this plan.");
            }

            var (Amount, Currency) = ResolvePaymentDetails(subscribe, subscribeGroup.Value);

            var currentStudentSubscribe = _StudentSubscribeRepo
                .Where(x => x.StudentId == model.StudentId.Value)
                .OrderByDescending(x => x.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();

            bool isMonthlyRenewal = IsMonthlyRenewalAction(model.ActionType);

            if (ShouldUpdateCurrentMonthSubscription(currentStudentSubscribe))
            {
                if (isMonthlyRenewal)
                {
                    _logger.LogInformation(
                        "Monthly renewal skipped because the student already has a subscription for the current Cairo month. StudentId: {StudentId}, CurrentSubscriptionId: {CurrentSubscriptionId}, PaymentId: {PaymentId}",
                        currentStudentSubscribe!.StudentId,
                        currentStudentSubscribe.Id,
                        currentStudentSubscribe.StudentPaymentId);

                    return output.CreateResponse(data: true);
                }

                await UpdateCurrentMonthSubscriptionAsync(
                    currentStudentSubscribe!,
                    subscribe,
                    Amount,
                    Currency,
                    userId,
                    model.ActionType);

                return output.CreateResponse(data: true);
            }

            var studentPayment = new StudentPayment
            {
                CreatedAt = BusinessDateTime.UtcNow,
                CreatedBy = userId,
                StudentId = model.StudentId.Value,
                StudentSubscribeId = model.StudentSubscribeId.Value,
                Amount = Amount,
                CurrencyId = Currency,
                PayStatue = false,
                IsCancelled = false,
            };
            await _StudentPaymentRepo.AddAsync(studentPayment);

            var studentSubscribe = new StudentSubscribe
            {
                CreatedAt = BusinessDateTime.UtcNow,
                CreatedBy = userId,
                StudentId = model.StudentId.Value,
                StudentSubscribeId = model.StudentSubscribeId.Value,
                RemainingMinutes = subscribe.Minutes,
                StudentSubscribeTypeId = subscribe.SubscribeTypeId,
                PayStatus = false,
                StudentPayment = studentPayment
            };

            await _StudentSubscribeRepo.AddAsync(studentSubscribe);
            await _unitOfWork.CommitAsync();

            await TryCreateHistoryAsync(
                CreateSubscriptionCreatedHistoryEntry(
                    studentSubscribe,
                    subscribe,
                    Amount,
                    Currency,
                    userId,
                    model.ActionType));

            return output.CreateResponse(data: true);
        }

        public async Task<IResponse<RepairStudentSubscriptionsResultDto>> RepairMissingInvoicesAsync(
            int? studentId,
            int? userId)
        {
            var output = new Response<RepairStudentSubscriptionsResultDto>();
            var result = new RepairStudentSubscriptionsResultDto();
            var now = BusinessDateTime.UtcNow;

            var activeSubscriptions = _StudentSubscribeRepo
                .GetAll()
                .Where(x =>
                    x.StudentId.HasValue &&
                    x.StudentSubscribeId.HasValue &&
                    x.Student != null &&
                    x.Student.IsDeleted == false &&
                    (!studentId.HasValue || x.StudentId == studentId.Value))
                .Select(x => new
                {
                    x.Id,
                    StudentId = x.StudentId!.Value,
                    x.CreatedAt
                })
                .ToList();

            if (activeSubscriptions.Count == 0)
            {
                return output.CreateResponse(result);
            }

            var latestSubscriptionIds = activeSubscriptions
                .GroupBy(x => x.StudentId)
                .Select(group => group
                    .OrderByDescending(x => x.CreatedAt ?? DateTime.MinValue)
                    .ThenByDescending(x => x.Id)
                    .Select(x => x.Id)
                    .First())
                .ToList();

            var latestSubscriptions = _StudentSubscribeRepo
                .GetAll()
                .Where(x => latestSubscriptionIds.Contains(x.Id))
                .ToList();

            result.ProcessedSubscriptionsCount = latestSubscriptions.Count;

            foreach (var subscription in latestSubscriptions)
            {
                try
                {
                    bool hasValidPayment = subscription.StudentPaymentId.HasValue &&
                        _StudentPaymentRepo.GetById(subscription.StudentPaymentId.Value) != null;

                    if (hasValidPayment)
                    {
                        result.SkippedSubscriptionsCount++;
                        continue;
                    }

                    if (!subscription.StudentSubscribeId.HasValue || !subscription.StudentId.HasValue)
                    {
                        result.SkippedSubscriptionsCount++;
                        continue;
                    }

                    var subscribe = _SubscribeRepo.GetById(subscription.StudentSubscribeId.Value);
                    if (subscribe == null)
                    {
                        result.SkippedSubscriptionsCount++;
                        _logger.LogWarning(
                            "Subscription invoice repair skipped because subscribe plan was not found. SubscriptionRecordId: {SubscriptionRecordId}, StudentId: {StudentId}, SubscribeId: {SubscribeId}",
                            subscription.Id,
                            subscription.StudentId,
                            subscription.StudentSubscribeId);
                        continue;
                    }

                    var subscribeGroup = ResolveSubscribeGroup(subscribe);
                    if (!subscribeGroup.HasValue)
                    {
                        result.SkippedSubscriptionsCount++;
                        _logger.LogWarning(
                            "Subscription invoice repair skipped because subscribe type group is missing. SubscriptionRecordId: {SubscriptionRecordId}, StudentId: {StudentId}, SubscribeId: {SubscribeId}",
                            subscription.Id,
                            subscription.StudentId,
                            subscription.StudentSubscribeId);
                        continue;
                    }

                    var (amount, currency) = ResolvePaymentDetails(subscribe, subscribeGroup.Value);

                    var studentPayment = new StudentPayment
                    {
                        CreatedAt = now,
                        CreatedBy = userId,
                        StudentId = subscription.StudentId.Value,
                        StudentSubscribeId = subscription.StudentSubscribeId.Value,
                        Amount = amount,
                        CurrencyId = currency,
                        PayStatue = false,
                        IsCancelled = false,
                    };

                    await _StudentPaymentRepo.AddAsync(studentPayment);

                    subscription.StudentPaymentId = null;
                    subscription.StudentPayment = studentPayment;
                    subscription.PayStatus = false;
                    subscription.CreatedAt = now;
                    subscription.ModefiedAt = now;
                    subscription.ModefiedBy = userId;

                    _StudentSubscribeRepo.Update(subscription);
                    await _unitOfWork.CommitAsync();

                    result.RepairedSubscriptionsCount++;
                }
                catch (Exception ex)
                {
                    result.SkippedSubscriptionsCount++;
                    _logger.LogError(
                        ex,
                        "Subscription invoice repair failed. SubscriptionRecordId: {SubscriptionRecordId}, StudentId: {StudentId}, SubscribeId: {SubscribeId}",
                        subscription.Id,
                        subscription.StudentId,
                        subscription.StudentSubscribeId);
                }
            }

            return output.CreateResponse(result);
        }

        private SubscribeTypeCategory? ResolveSubscribeGroup(Subscribe subscribe)
        {
            var category = ConvertGroup(subscribe.SubscribeType?.Group);
            if (category.HasValue)
            {
                return category;
            }

            if (!subscribe.SubscribeTypeId.HasValue)
            {
                return null;
            }

            var subscribeType = _SubscribeTypeRepo.GetById(subscribe.SubscribeTypeId.Value);
            return ConvertGroup(subscribeType?.Group);
        }

        private static SubscribeTypeCategory? ConvertGroup(int? groupValue)
            => groupValue.HasValue ? (SubscribeTypeCategory?)groupValue.Value : null;

        private void PopulateMonthlyConsumption(IReadOnlyList<ViewStudentSubscribeReDto>? items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            var subscriptionMonths = items
                .Where(x => x.StudentId.HasValue && x.StartDate.HasValue)
                .Select(x =>
                {
                    var cairoStart = BusinessDateTime.ToCairo(x.StartDate!.Value);
                    var (monthStartUtc, monthEndUtc) = BusinessDateTime.GetCairoMonthRangeUtc(cairoStart.Year, cairoStart.Month);

                    return new
                    {
                        StudentId = x.StudentId!.Value,
                        MonthKey = BuildMonthlyConsumptionKey(x.StudentId.Value, cairoStart.Year, cairoStart.Month),
                        MonthStartUtc = monthStartUtc,
                        MonthEndUtc = monthEndUtc
                    };
                })
                .GroupBy(x => x.MonthKey)
                .Select(x => x.First())
                .ToList();

            if (subscriptionMonths.Count == 0)
            {
                return;
            }

            var studentIds = subscriptionMonths
                .Select(x => x.StudentId)
                .Distinct()
                .ToList();

            var requestedMonthKeys = subscriptionMonths
                .Select(x => x.MonthKey)
                .ToHashSet(StringComparer.Ordinal);

            var overallStartUtc = subscriptionMonths.Min(x => x.MonthStartUtc);
            var overallEndUtc = subscriptionMonths.Max(x => x.MonthEndUtc);

            var reports = _CircleReportRepo
                .GetAll()
                .AsNoTracking()
                .Where(x =>
                    x.StudentId.HasValue &&
                    studentIds.Contains(x.StudentId.Value) &&
                    x.CreationTime >= overallStartUtc &&
                    x.CreationTime < overallEndUtc &&
                    x.IsDeleted == false)
                .Select(x => new
                {
                    StudentId = x.StudentId!.Value,
                    x.CreationTime,
                    x.Minutes,
                    x.AttendStatueId
                })
                .ToList();

            var consumptionLookup = reports
                .Select(x =>
                {
                    var cairoCreationTime = BusinessDateTime.ToCairo(x.CreationTime);
                    int consumedMinutes = ResolveConsumedMinutes(x.AttendStatueId, x.Minutes);

                    return new
                    {
                        MonthKey = BuildMonthlyConsumptionKey(x.StudentId, cairoCreationTime.Year, cairoCreationTime.Month),
                        ConsumedMinutes = consumedMinutes
                    };
                })
                .Where(x => requestedMonthKeys.Contains(x.MonthKey))
                .GroupBy(x => x.MonthKey)
                .ToDictionary(
                    x => x.Key,
                    x =>
                    {
                        int consumedMinutes = x.Sum(y => y.ConsumedMinutes);
                        int sessionsCount = x.Count(y => y.ConsumedMinutes > 0);
                        decimal consumedHours = Math.Round(consumedMinutes / 60m, 2, MidpointRounding.AwayFromZero);

                        return new
                        {
                            ConsumedMinutes = consumedMinutes,
                            ConsumedSessionsCount = sessionsCount,
                            ConsumedHours = consumedHours
                        };
                    },
                    StringComparer.Ordinal);

            foreach (var item in items)
            {
                if (!item.StudentId.HasValue || !item.StartDate.HasValue)
                {
                    continue;
                }

                var cairoStart = BusinessDateTime.ToCairo(item.StartDate.Value);
                var monthKey = BuildMonthlyConsumptionKey(item.StudentId.Value, cairoStart.Year, cairoStart.Month);

                if (!consumptionLookup.TryGetValue(monthKey, out var usage))
                {
                    item.ConsumedMinutes = 0;
                    item.ConsumedSessionsCount = 0;
                    item.ConsumedHours = 0m;
                    continue;
                }

                item.ConsumedMinutes = usage.ConsumedMinutes;
                item.ConsumedSessionsCount = usage.ConsumedSessionsCount;
                item.ConsumedHours = usage.ConsumedHours;
            }
        }

        private static int ResolveConsumedMinutes(int? attendStatusId, double? minutes)
        {
            if (!CountsForConsumption(attendStatusId) || !minutes.HasValue)
            {
                return 0;
            }

            return Convert.ToInt32(minutes.Value);
        }

        private static bool CountsForConsumption(int? attendStatusId)
        {
            return attendStatusId == 1 || attendStatusId == 3;
        }

        private static string BuildMonthlyConsumptionKey(int studentId, int year, int month)
            => $"{studentId}:{year:D4}-{month:D2}";

        private static (decimal Amount, int Currency) ResolvePaymentDetails(Subscribe subscribe, SubscribeTypeCategory group)
        {
            decimal price = subscribe.Price;
            int currencyId = group switch
            {
                SubscribeTypeCategory.Egyptian => (int)CurrencyEnum.EGP,
                SubscribeTypeCategory.Arab => (int)CurrencyEnum.SAR,
                SubscribeTypeCategory.Foreign => (int)CurrencyEnum.USD,
                _ => throw new ArgumentOutOfRangeException(nameof(group), group, "Unsupported subscription group")
            };

            return (Math.Round(price, 2, MidpointRounding.AwayFromZero), currencyId);
        }

        private bool ShouldUpdateCurrentMonthSubscription(StudentSubscribe? currentStudentSubscribe)
        {
            if (currentStudentSubscribe?.CreatedAt == null)
            {
                return false;
            }

            return BusinessDateTime.IsInSameCairoMonth(
                currentStudentSubscribe.CreatedAt.Value,
                BusinessDateTime.UtcNow);
        }

        private static bool IsMonthlyRenewalAction(string? actionType)
            => string.Equals(actionType?.Trim(), "MonthlyRenewal", StringComparison.OrdinalIgnoreCase);

        private async Task UpdateCurrentMonthSubscriptionAsync(
            StudentSubscribe currentStudentSubscribe,
            Subscribe newSubscribe,
            decimal amount,
            int currency,
            int? userId,
            string? actionType = null)
        {
            var now = BusinessDateTime.UtcNow;
            var oldSubscribe = currentStudentSubscribe.StudentSubscribeId.HasValue
                ? _SubscribeRepo.GetById(currentStudentSubscribe.StudentSubscribeId.Value)
                : null;

            int currentRemainingMinutes = Math.Max(0, currentStudentSubscribe.RemainingMinutes ?? 0);
            int oldTotalMinutes = Math.Max(currentRemainingMinutes, oldSubscribe?.Minutes ?? 0);
            int usedMinutes = Math.Max(0, oldTotalMinutes - currentRemainingMinutes);
            int newTotalMinutes = Math.Max(0, newSubscribe.Minutes ?? 0);
            int newRemainingMinutes = Math.Max(0, newTotalMinutes - usedMinutes);
            bool? previousStudentPayStatus = currentStudentSubscribe.PayStatus;

            int? previousSubscribeId = currentStudentSubscribe.StudentSubscribeId;
            int? previousSubscribeTypeId = currentStudentSubscribe.StudentSubscribeTypeId;
            int? paymentId = currentStudentSubscribe.StudentPaymentId;

            currentStudentSubscribe.StudentSubscribeId = newSubscribe.Id;
            currentStudentSubscribe.StudentSubscribeTypeId = newSubscribe.SubscribeTypeId;
            currentStudentSubscribe.StudentSubscribeNavigation = newSubscribe;
            currentStudentSubscribe.StudentSubscribeType = newSubscribe.SubscribeType
                ?? (newSubscribe.SubscribeTypeId.HasValue
                    ? _SubscribeTypeRepo.GetById(newSubscribe.SubscribeTypeId.Value)
                    : null);
            currentStudentSubscribe.RemainingMinutes = newRemainingMinutes;
            currentStudentSubscribe.ModefiedAt = now;
            currentStudentSubscribe.ModefiedBy = userId;

            StudentPayment? currentPayment = null;
            decimal? previousAmount = null;
            int? previousCurrencyId = null;
            bool? previousPaymentStatus = null;
            bool? newPaymentStatus = currentStudentSubscribe.PayStatus;
            decimal amountPaidBeforeChange = 0m;
            decimal remainingAmountAfterChange = amount;
            if (paymentId.HasValue)
            {
                currentPayment = _StudentPaymentRepo.GetById(paymentId.Value);
                previousAmount = currentPayment?.Amount;
                previousCurrencyId = currentPayment?.CurrencyId;
                previousPaymentStatus = currentPayment?.PayStatue;
            }

            if (currentPayment != null)
            {
                amountPaidBeforeChange = currentPayment.PayStatue == true
                    ? Math.Max(0m, currentPayment.Amount ?? 0m)
                    : 0m;

                currentPayment.StudentSubscribeId = newSubscribe.Id;
                currentPayment.StudentSubscribe = newSubscribe;
                currentPayment.CurrencyId = currency;
                currentPayment.IsCancelled = false;
                currentPayment.ModefiedAt = now;
                currentPayment.ModefiedBy = userId;

                if (currentPayment.PayStatue == true && amount > amountPaidBeforeChange)
                {
                    remainingAmountAfterChange = amount - amountPaidBeforeChange;
                    currentPayment.Amount = remainingAmountAfterChange;
                    currentPayment.PayStatue = false;
                    currentPayment.PaymentDate = null;
                    currentPayment.ReceiptPath = null;
                    currentStudentSubscribe.PayStatus = false;
                }
                else
                {
                    remainingAmountAfterChange = amount;
                    currentPayment.Amount = amount;
                    currentStudentSubscribe.PayStatus = currentPayment.PayStatue;
                }

                newPaymentStatus = currentPayment.PayStatue;
            }
            else
            {
                _logger.LogWarning(
                    "Current-month student subscription update found no linked payment. StudentId: {StudentId}, StudentSubscribeId: {StudentSubscribeId}, RequestedSubscribeId: {RequestedSubscribeId}",
                    currentStudentSubscribe.StudentId,
                    currentStudentSubscribe.Id,
                    newSubscribe.Id);
            }

            _StudentSubscribeRepo.Update(currentStudentSubscribe);
            if (currentPayment != null)
            {
                _StudentPaymentRepo.Update(currentPayment);
            }

            await _unitOfWork.CommitAsync();

            await TryCreateHistoryAsync(
                CreateSubscriptionUpdatedHistoryEntry(
                    currentStudentSubscribe,
                    oldSubscribe,
                    newSubscribe,
                    currentRemainingMinutes,
                    newRemainingMinutes,
                    usedMinutes,
                    previousAmount,
                    amount,
                    amountPaidBeforeChange,
                    currentPayment != null ? remainingAmountAfterChange : amount,
                    previousCurrencyId,
                    currency,
                    previousPaymentStatus ?? previousStudentPayStatus,
                    newPaymentStatus ?? currentStudentSubscribe.PayStatus,
                    userId,
                    now,
                    actionType));

            _logger.LogInformation(
                "Student subscription changed in-place for current month. StudentId: {StudentId}, PaymentId: {PaymentId}, OldSubscribeId: {OldSubscribeId}, NewSubscribeId: {NewSubscribeId}, OldSubscribeTypeId: {OldSubscribeTypeId}, NewSubscribeTypeId: {NewSubscribeTypeId}, UsedMinutes: {UsedMinutes}, OldRemainingMinutes: {OldRemainingMinutes}, NewRemainingMinutes: {NewRemainingMinutes}, OldAmount: {OldAmount}, NewAmount: {NewAmount}, RemainingAmountAfterChange: {RemainingAmountAfterChange}, AmountPaidBeforeChange: {AmountPaidBeforeChange}, OldCurrencyId: {OldCurrencyId}, NewCurrencyId: {NewCurrencyId}, PreviousPaymentStatus: {PreviousPaymentStatus}, NewPaymentStatus: {NewPaymentStatus}, ChangedBy: {ChangedBy}, ChangedAt: {ChangedAt}",
                currentStudentSubscribe.StudentId,
                paymentId,
                previousSubscribeId,
                newSubscribe.Id,
                previousSubscribeTypeId,
                newSubscribe.SubscribeTypeId,
                usedMinutes,
                currentRemainingMinutes,
                newRemainingMinutes,
                previousAmount,
                amount,
                remainingAmountAfterChange,
                amountPaidBeforeChange,
                previousCurrencyId,
                currency,
                previousPaymentStatus,
                newPaymentStatus,
                userId.HasValue ? userId.Value : "System",
                now);
        }

        private StudentSubscribeHistory CreateSubscriptionCreatedHistoryEntry(
            StudentSubscribe studentSubscribe,
            Subscribe subscribe,
            decimal amount,
            int currency,
            int? userId,
            string? actionType = null)
        {
            return new StudentSubscribeHistory
            {
                CreatedAt = studentSubscribe.CreatedAt ?? BusinessDateTime.UtcNow,
                CreatedBy = userId,
                StudentId = studentSubscribe.StudentId,
                StudentSubscribeRecordId = studentSubscribe.Id,
                ActionType = ResolveHistoryActionType(actionType, "Created"),
                NewSubscribeId = subscribe.Id,
                NewPlanName = BuildPlanName(subscribe),
                NewRemainingMinutes = studentSubscribe.RemainingMinutes,
                UsedMinutes = 0,
                NewAmount = amount,
                AmountPaidBeforeChange = 0m,
                RemainingAmountAfterChange = amount,
                NewCurrencyId = currency,
                NewPayStatus = studentSubscribe.PayStatus,
                IsDeleted = false
            };
        }

        private StudentSubscribeHistory CreateSubscriptionUpdatedHistoryEntry(
            StudentSubscribe studentSubscribe,
            Subscribe? oldSubscribe,
            Subscribe newSubscribe,
            int oldRemainingMinutes,
            int newRemainingMinutes,
            int usedMinutes,
            decimal? oldAmount,
            decimal newAmount,
            decimal amountPaidBeforeChange,
            decimal remainingAmountAfterChange,
            int? oldCurrencyId,
            int newCurrencyId,
            bool? oldPayStatus,
            bool? newPayStatus,
            int? userId,
            DateTime changedAt,
            string? actionType = null)
        {
            return new StudentSubscribeHistory
            {
                CreatedAt = changedAt,
                CreatedBy = userId,
                StudentId = studentSubscribe.StudentId,
                StudentSubscribeRecordId = studentSubscribe.Id,
                ActionType = ResolveHistoryActionType(actionType, "Updated"),
                OldSubscribeId = oldSubscribe?.Id,
                OldPlanName = BuildPlanName(oldSubscribe),
                NewSubscribeId = newSubscribe.Id,
                NewPlanName = BuildPlanName(newSubscribe),
                OldRemainingMinutes = oldRemainingMinutes,
                NewRemainingMinutes = newRemainingMinutes,
                UsedMinutes = usedMinutes,
                OldAmount = oldAmount,
                NewAmount = newAmount,
                AmountPaidBeforeChange = amountPaidBeforeChange,
                RemainingAmountAfterChange = remainingAmountAfterChange,
                OldCurrencyId = oldCurrencyId,
                NewCurrencyId = newCurrencyId,
                OldPayStatus = oldPayStatus,
                NewPayStatus = newPayStatus,
                IsDeleted = false
            };
        }

        private static string ResolveHistoryActionType(string? actionType, string defaultActionType)
            => string.IsNullOrWhiteSpace(actionType) ? defaultActionType : actionType.Trim();

        private string? BuildPlanName(Subscribe? subscribe)
        {
            if (subscribe == null)
            {
                return null;
            }

            string? subscribeTypeName = subscribe.SubscribeType?.Name;
            if (string.IsNullOrWhiteSpace(subscribeTypeName) && subscribe.SubscribeTypeId.HasValue)
            {
                subscribeTypeName = _SubscribeTypeRepo.GetById(subscribe.SubscribeTypeId.Value)?.Name;
            }

            if (!string.IsNullOrWhiteSpace(subscribeTypeName) && !string.IsNullOrWhiteSpace(subscribe.Name))
            {
                return $"{subscribeTypeName} ( {subscribe.Name} )";
            }

            return !string.IsNullOrWhiteSpace(subscribe.Name)
                ? subscribe.Name
                : subscribeTypeName;
        }

        private async Task TryCreateHistoryAsync(StudentSubscribeHistory historyEntry)
        {
            try
            {
                await _StudentSubscribeHistoryRepo.AddAsync(historyEntry);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Student subscription history could not be persisted. StudentId: {StudentId}, ActionType: {ActionType}, SubscriptionRecordId: {SubscriptionRecordId}",
                    historyEntry.StudentId,
                    historyEntry.ActionType,
                    historyEntry.StudentSubscribeRecordId);
            }
        }

    }
}
