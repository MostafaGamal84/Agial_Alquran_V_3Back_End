using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.DTO.Dashboard;
using Orbits.GeneralProject.Repositroy.Base;
using System.Globalization;
using System.Linq;

namespace Orbits.GeneralProject.BLL.DashboardService
{
    public class DashboardBLL : BaseBLL, IDashboardBLL
    {
        private static readonly Dictionary<int, string> CurrencyLabels = new()
        {
            { (int)CurrencyEnum.EGP, "EGP" },
            { (int)CurrencyEnum.SAR, "SAR" },
            { (int)CurrencyEnum.USD, "USD" }
        };

        private static readonly string DefaultCurrencyCode = CurrencyLabels[(int)CurrencyEnum.EGP];

        private readonly IRepository<StudentPayment> _studentPaymentRepository;
        private readonly IRepository<User> _UserRepository;
        private readonly IRepository<CircleReport> _circleReportRepository;
        private readonly IRepository<Circle> _circleRepository;
        private readonly IRepository<TeacherSallary> _teacherSalaryRepository;
        private readonly IRepository<ManagerSallary> _managerSalaryRepository;
        private readonly IRepository<Subscribe> _subscribeRepository;
        private readonly IRepository<SubscribeType> _subscribeTypeRepository;

        public DashboardBLL(
            IMapper mapper,
            IRepository<StudentPayment> studentPaymentRepository,
            IRepository<User> UserRepository,
            IRepository<CircleReport> circleReportRepository,
            IRepository<Circle> circleRepository,
            IRepository<TeacherSallary> teacherSalaryRepository,
            IRepository<ManagerSallary> managerSalaryRepository,
            IRepository<Subscribe> subscribeRepository,
            IRepository<SubscribeType> subscribeTypeRepository) : base(mapper)
        {
            _studentPaymentRepository = studentPaymentRepository;
            _UserRepository = UserRepository;
            _circleReportRepository = circleReportRepository;
            _circleRepository = circleRepository;
            _teacherSalaryRepository = teacherSalaryRepository;
            _managerSalaryRepository = managerSalaryRepository;
            _subscribeRepository = subscribeRepository;
            _subscribeTypeRepository = subscribeTypeRepository;
        }

        public async Task<IResponse<DashboardSummaryDto>> GetSummaryAsync()
        {
            Response<DashboardSummaryDto> output = new();
            try
            {
                DateTime utcNow = DateTime.UtcNow;
                DateTime currentMonthStart = new DateTime(utcNow.Year, utcNow.Month, 1);
                DateTime nextMonthStart = currentMonthStart.AddMonths(1);
                DateTime previousMonthStart = currentMonthStart.AddMonths(-1);

                var paymentsQuery = _studentPaymentRepository
                    .Where(payment => payment.PaymentDate.HasValue && payment.Amount.HasValue);

                decimal totalEarnings = await paymentsQuery
                    .SumAsync(payment => (decimal?)(payment.Amount ?? 0)) ?? 0m;

                decimal currentMonthEarnings = await paymentsQuery
                    .Where(payment => payment.PaymentDate >= currentMonthStart && payment.PaymentDate < nextMonthStart)
                    .SumAsync(payment => (decimal?)(payment.Amount ?? 0)) ?? 0m;

                decimal previousMonthEarnings = await paymentsQuery
                    .Where(payment => payment.PaymentDate >= previousMonthStart && payment.PaymentDate < currentMonthStart)
                    .SumAsync(payment => (decimal?)(payment.Amount ?? 0)) ?? 0m;

                double totalTeacherPayoutsDouble = await _teacherSalaryRepository
                    .Where(salary => salary.Sallary.HasValue)
                    .SumAsync(salary => salary.Sallary ?? 0d);

                double currentMonthTeacherPayoutsDouble = await _teacherSalaryRepository
                    .Where(salary => salary.Sallary.HasValue && salary.Month.HasValue &&
                                     salary.Month.Value >= currentMonthStart && salary.Month.Value < nextMonthStart)
                    .SumAsync(salary => salary.Sallary ?? 0d);

                double previousMonthTeacherPayoutsDouble = await _teacherSalaryRepository
                    .Where(salary => salary.Sallary.HasValue && salary.Month.HasValue &&
                                     salary.Month.Value >= previousMonthStart && salary.Month.Value < currentMonthStart)
                    .SumAsync(salary => salary.Sallary ?? 0d);

                double totalManagerPayoutsDouble = await _managerSalaryRepository
                    .Where(salary => salary.Sallary.HasValue)
                    .SumAsync(salary => salary.Sallary ?? 0d);

                double currentMonthManagerPayoutsDouble = await _managerSalaryRepository
                    .Where(salary => salary.Sallary.HasValue && salary.Month.HasValue &&
                                     salary.Month.Value >= currentMonthStart && salary.Month.Value < nextMonthStart)
                    .SumAsync(salary => salary.Sallary ?? 0d);

                double previousMonthManagerPayoutsDouble = await _managerSalaryRepository
                    .Where(salary => salary.Sallary.HasValue && salary.Month.HasValue &&
                                     salary.Month.Value >= previousMonthStart && salary.Month.Value < currentMonthStart)
                    .SumAsync(salary => salary.Sallary ?? 0d);

                decimal totalTeacherPayouts = Round(Convert.ToDecimal(totalTeacherPayoutsDouble));
                decimal currentMonthTeacherPayouts = Round(Convert.ToDecimal(currentMonthTeacherPayoutsDouble));
                decimal previousMonthTeacherPayouts = Round(Convert.ToDecimal(previousMonthTeacherPayoutsDouble));

                decimal totalManagerPayouts = Round(Convert.ToDecimal(totalManagerPayoutsDouble));
                decimal currentMonthManagerPayouts = Round(Convert.ToDecimal(currentMonthManagerPayoutsDouble));
                decimal previousMonthManagerPayouts = Round(Convert.ToDecimal(previousMonthManagerPayoutsDouble));

                decimal currentMonthNetIncome = Round(currentMonthEarnings - currentMonthTeacherPayouts - currentMonthManagerPayouts);
                decimal previousMonthNetIncome = Round(previousMonthEarnings - previousMonthTeacherPayouts - previousMonthManagerPayouts);
                decimal lifetimeNetIncome = Round(totalEarnings - totalTeacherPayouts - totalManagerPayouts);

                int totalStudents = await _UserRepository.Where(x=>x.UserTypeId == (int)UserTypesEnum.Student).CountAsync();
                int totalTeachers = await _UserRepository.Where(x => x.UserTypeId == (int)UserTypesEnum.Teacher).CountAsync();
                int totalCircleReports = await _circleReportRepository.GetAll().CountAsync();

                int currentMonthNewStudents = await _UserRepository
                    .Where(student => student.UserTypeId == (int)UserTypesEnum.Student && student.RegisterAt.HasValue &&
                                       student.RegisterAt.Value >= currentMonthStart && student.RegisterAt.Value < nextMonthStart)
                    .CountAsync();

                int previousMonthNewStudents = await _UserRepository
                    .Where(student => student.UserTypeId == (int)UserTypesEnum.Student &&  student.RegisterAt.HasValue &&
                                       student.RegisterAt.Value >= previousMonthStart && student.RegisterAt.Value < currentMonthStart)
                    .CountAsync();

                int currentMonthCircleReports = await _circleReportRepository
                    .Where(report => report.CreationTime >= currentMonthStart && report.CreationTime < nextMonthStart)
                    .CountAsync();

                int previousMonthCircleReports = await _circleReportRepository
                    .Where(report => report.CreationTime >= previousMonthStart && report.CreationTime < currentMonthStart)
                    .CountAsync();

                var summary = new DashboardSummaryDto
                {
                    PeriodStart = currentMonthStart,
                    PeriodEnd = nextMonthStart,
                    LifetimeEarnings = Round(totalEarnings),
                    LifetimeTeacherPayouts = totalTeacherPayouts,
                    LifetimeManagerPayouts = totalManagerPayouts,
                    LifetimeNetIncome = lifetimeNetIncome,
                    TotalStudents = totalStudents,
                    TotalTeachers = totalTeachers,
                    TotalCircleReports = totalCircleReports,
                    Metrics = new List<DashboardMetricDto>
                    {
                        new DashboardMetricDto
                        {
                            Key = "earnings",
                            Title = "Total Earnings",
                            ValueType = "currency",
                            Value = Round(currentMonthEarnings),
                            PreviousValue = Round(previousMonthEarnings),
                            ChangePercentage = CalculatePercentageChange(currentMonthEarnings, previousMonthEarnings),
                            TotalValue = Round(totalEarnings)
                        },
                        new DashboardMetricDto
                        {
                            Key = "newStudents",
                            Title = "New Students",
                            ValueType = "count",
                            Value = currentMonthNewStudents,
                            PreviousValue = previousMonthNewStudents,
                            ChangePercentage = CalculatePercentageChange(currentMonthNewStudents, previousMonthNewStudents),
                            TotalValue = totalStudents
                        },
                        new DashboardMetricDto
                        {
                            Key = "circleReports",
                            Title = "Circle Reports",
                            ValueType = "count",
                            Value = currentMonthCircleReports,
                            PreviousValue = previousMonthCircleReports,
                            ChangePercentage = CalculatePercentageChange(currentMonthCircleReports, previousMonthCircleReports),
                            TotalValue = totalCircleReports
                        },
                        new DashboardMetricDto
                        {
                            Key = "netIncome",
                            Title = "Net Income",
                            ValueType = "currency",
                            Value = currentMonthNetIncome,
                            PreviousValue = previousMonthNetIncome,
                            ChangePercentage = CalculatePercentageChange(currentMonthNetIncome, previousMonthNetIncome),
                            TotalValue = lifetimeNetIncome
                        }
                    }
                };

                return output.CreateResponse(summary);
            }
            catch (Exception ex)
            {
                return output.CreateResponse(ex);
            }
        }


        public async Task<IResponse<DashboardOverviewResponseDto>> GetRoleOverviewAsync(int userId, DashboardRangeInputDto? range = null, string? role = null)
        {
            Response<DashboardOverviewResponseDto> output = new();

            try
            {
                var userInfo = await _UserRepository.GetAll()
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.Id, u.UserTypeId, u.BranchId })
                    .FirstOrDefaultAsync();

                if (userInfo == null || !userInfo.UserTypeId.HasValue)
                {
                    return output.AppendError(MessageCodes.NotFound);
                }

                var userType = ResolveUserRole(role, (UserTypesEnum)userInfo.UserTypeId.Value);

                if (userType == UserTypesEnum.BranchLeader && !userInfo.BranchId.HasValue)
                {
                    return output.AppendError(MessageCodes.BusinessValidationError, "branchId", "Branch manager does not have an assigned branch.");
                }

                DashboardScope scope = CreateScope(userType, userInfo.BranchId, userInfo.Id);
                DashboardRange rangeInfo = ResolveRange(range);
                string rangeLabel = ResolveRangeLabel(rangeInfo, range?.Range);

                var paymentsBaseQuery = ApplyPaymentScope(_studentPaymentRepository.GetAll().AsNoTracking(), scope);
                var teacherSalaryBase = ApplyTeacherSalaryScope(_teacherSalaryRepository.GetAll().AsNoTracking(), scope);
                var managerSalaryBase = ApplyManagerSalaryScope(_managerSalaryRepository.GetAll().AsNoTracking(), scope);
                var circleReportsBase = ApplyCircleReportScope(_circleReportRepository.GetAll().AsNoTracking(), scope);
                var circleBase = ApplyCircleScope(_circleRepository.GetAll().AsNoTracking(), scope);

                var branchLeadersQuery = CreateScopedBranchLeadersQuery(scope);
                var managersQuery = CreateScopedManagersQuery(scope);
                var teachersQuery = CreateScopedTeachersQuery(scope);
                var studentsQuery = CreateScopedStudentsQuery(scope);

                DashboardOverviewMetricsDto metrics = new();

                metrics.BranchManagersCount = await branchLeadersQuery.CountAsync();
                metrics.SupervisorsCount = await managersQuery.CountAsync();
                metrics.TeachersCount = await teachersQuery.CountAsync();
                metrics.StudentsCount = await studentsQuery.CountAsync();

                metrics.CirclesCount = await circleBase.Select(c => c.Id).Distinct().CountAsync();
                metrics.ReportsCount = await circleReportsBase.CountAsync();

                TimeSpan rangeDuration = rangeInfo.EndExclusive - rangeInfo.Start;
                DateTime previousRangeStart = rangeInfo.Start - rangeDuration;
                DateTime previousRangeEndExclusive = rangeInfo.Start;

                var circleReportsRangeQuery = circleReportsBase
                    .Where(r => r.CreationTime >= rangeInfo.Start && r.CreationTime < rangeInfo.EndExclusive);

                metrics.CircleReports = await circleReportsRangeQuery.CountAsync();

                int previousCircleReports = await circleReportsBase
                    .Where(r => r.CreationTime >= previousRangeStart && r.CreationTime < previousRangeEndExclusive)
                    .CountAsync();

                metrics.CircleReportsPercentChange = CalculatePercentageChange(metrics.CircleReports, previousCircleReports);

                int newStudentsCount = await studentsQuery
                    .Where(u => u.RegisterAt.HasValue &&
                                u.RegisterAt.Value >= rangeInfo.Start &&
                                u.RegisterAt.Value < rangeInfo.EndExclusive)
                    .CountAsync();

                metrics.NewStudents = newStudentsCount;

                int previousNewStudentsCount = await studentsQuery
                    .Where(u => u.RegisterAt.HasValue &&
                                u.RegisterAt.Value >= previousRangeStart &&
                                u.RegisterAt.Value < previousRangeEndExclusive)
                    .CountAsync();

                metrics.NewStudentsPercentChange = CalculatePercentageChange(metrics.NewStudents, previousNewStudentsCount);

                var paymentsRangeQuery = paymentsBaseQuery
                    .Where(p =>
                        (p.PaymentDate ?? p.CreatedAt) >= rangeInfo.Start &&
                        (p.PaymentDate ?? p.CreatedAt) < rangeInfo.EndExclusive);

                var paymentsPreviousQuery = paymentsBaseQuery
                    .Where(p =>
                        (p.PaymentDate ?? p.CreatedAt) >= previousRangeStart &&
                        (p.PaymentDate ?? p.CreatedAt) < previousRangeEndExclusive);

                decimal earningsRaw = await paymentsRangeQuery
                    .SumAsync(p => (decimal?)(p.Amount ?? 0)) ?? 0m;

                decimal previousEarningsRaw = await paymentsPreviousQuery
                    .SumAsync(p => (decimal?)(p.Amount ?? 0)) ?? 0m;

                var teacherSalaryRange = teacherSalaryBase
                    .Where(s => s.Month.HasValue && s.Month.Value >= rangeInfo.Start && s.Month.Value < rangeInfo.EndExclusive);

                var teacherSalaryPrevious = teacherSalaryBase
                    .Where(s => s.Month.HasValue && s.Month.Value >= previousRangeStart && s.Month.Value < previousRangeEndExclusive);

                var managerSalaryRange = managerSalaryBase
                    .Where(s => s.Month.HasValue && s.Month.Value >= rangeInfo.Start && s.Month.Value < rangeInfo.EndExclusive);

                var managerSalaryPrevious = managerSalaryBase
                    .Where(s => s.Month.HasValue && s.Month.Value >= previousRangeStart && s.Month.Value < previousRangeEndExclusive);

                decimal teacherSalaryTotal = Round(await teacherSalaryRange.SumAsync(s => (decimal?)(s.Sallary ?? 0d)) ?? 0m);
                decimal teacherSalaryPreviousTotal = Round(await teacherSalaryPrevious.SumAsync(s => (decimal?)(s.Sallary ?? 0d)) ?? 0m);

                decimal managerSalaryTotal = Round(await managerSalaryRange.SumAsync(s => (decimal?)(s.Sallary ?? 0d)) ?? 0m);
                decimal managerSalaryPreviousTotal = Round(await managerSalaryPrevious.SumAsync(s => (decimal?)(s.Sallary ?? 0d)) ?? 0m);

                decimal outgoing = Round(teacherSalaryTotal + managerSalaryTotal);
                decimal previousOutgoing = Round(teacherSalaryPreviousTotal + managerSalaryPreviousTotal);

                decimal netIncomeRaw = Round(earningsRaw - outgoing);
                decimal previousNetIncomeRaw = Round(previousEarningsRaw - previousOutgoing);

                decimal incomingEgpRaw = await paymentsRangeQuery
                    .Where(p => p.CurrencyId == (int)CurrencyEnum.EGP || p.CurrencyId == null)
                    .SumAsync(p => (decimal?)(p.Amount ?? 0)) ?? 0m;
                decimal incomingSarRaw = await paymentsRangeQuery
                    .Where(p => p.CurrencyId == (int)CurrencyEnum.SAR)
                    .SumAsync(p => (decimal?)(p.Amount ?? 0)) ?? 0m;
                decimal incomingUsdRaw = await paymentsRangeQuery
                    .Where(p => p.CurrencyId == (int)CurrencyEnum.USD)
                    .SumAsync(p => (decimal?)(p.Amount ?? 0)) ?? 0m;

                decimal netProfitRaw = incomingSarRaw + incomingUsdRaw + incomingEgpRaw - outgoing;

                metrics.CurrencyCode = DefaultCurrencyCode;

                metrics.Earnings = Round(earningsRaw);
                metrics.EarningsCurrencyCode = ResolveCurrencyCode((int)CurrencyEnum.EGP);
                metrics.EarningsPercentChange = CalculatePercentageChange(metrics.Earnings, previousEarningsRaw);

                metrics.NetIncome = netIncomeRaw;
                metrics.NetIncomeCurrencyCode = DefaultCurrencyCode;
                metrics.NetIncomePercentChange = CalculatePercentageChange(metrics.NetIncome, previousNetIncomeRaw);

                metrics.Outgoing = outgoing;
                metrics.OutgoingCurrencyCode = DefaultCurrencyCode;

                metrics.IncomingEgp = Round(incomingEgpRaw);
                metrics.IncomingEgpCurrencyCode = ResolveCurrencyCode((int)CurrencyEnum.EGP);

                metrics.IncomingSar = Round(incomingSarRaw);
                metrics.IncomingSarCurrencyCode = ResolveCurrencyCode((int)CurrencyEnum.SAR);

                metrics.IncomingUsd = Round(incomingUsdRaw);
                metrics.IncomingUsdCurrencyCode = ResolveCurrencyCode((int)CurrencyEnum.USD);

                metrics.NetProfit = Round(netProfitRaw);
                metrics.NetProfitCurrencyCode = DefaultCurrencyCode;

                DashboardOverviewChartsDto charts = new()
                {
                    MonthlyRevenue = await BuildOverviewMonthlyRevenueAsync(rangeInfo, paymentsBaseQuery, teacherSalaryBase, managerSalaryBase, circleReportsBase),
                    Transactions = await LoadOverviewTransactionsAsync(paymentsBaseQuery)
                };

                var dto = new DashboardOverviewResponseDto
                {
                    Role = MapRoleName(userType),
                    RangeStart = FormatDate(rangeInfo.Start),
                    RangeEnd = FormatDate(rangeInfo.EndInclusive),
                    RangeLabel = rangeLabel,
                    Metrics = metrics,
                    Charts = charts
                };

                return output.CreateResponse(dto);
            }
            catch (Exception ex)
            {
                return output.CreateResponse(ex);
            }
        }

        public async Task<IResponse<IEnumerable<DashboardUpcomingCircleDto>>> GetUpcomingCirclesAsync(int userId, int? limit = null, string? role = null)
        {
            Response<IEnumerable<DashboardUpcomingCircleDto>> output = new();

            try
            {
                var userInfo = await _UserRepository.GetAll()
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.Id, u.UserTypeId, u.BranchId })
                    .FirstOrDefaultAsync();

                if (userInfo == null || !userInfo.UserTypeId.HasValue)
                {
                    return output.AppendError(MessageCodes.NotFound);
                }

                var userType = ResolveUserRole(role, (UserTypesEnum)userInfo.UserTypeId.Value);
                DashboardScope scope = CreateScope(userType, userInfo.BranchId, userInfo.Id);

                int effectiveLimit = limit.GetValueOrDefault(5);
                if (effectiveLimit <= 0)
                {
                    effectiveLimit = 5;
                }

                var circlesQuery = ApplyCircleScope(
                        _circleRepository.GetAll()
                            .AsNoTracking()
                            .Include(c => c.CircleDays)
                            .Include(c => c.ManagerCircles)
                                .ThenInclude(mc => mc.Manager)
                            .Include(c => c.Teacher),
                        scope)
                    .Where(c => c.CircleDays.Any());

                DateTime reference = DateTime.UtcNow;

                var circles = await circlesQuery
                    .ToListAsync();

                var mapped = circles
                    .Select(circle => BuildUpcomingDashboardCircle(circle, reference))
                    .Where(result => result.Dto.Day != null)
                    .OrderBy(result => result.NextOccurrence ?? DateTime.MaxValue)
                    .Take(effectiveLimit)
                    .Select(result => result.Dto)
                    .ToList();

                return output.CreateResponse(mapped);
            }
            catch (Exception ex)
            {
                return output.CreateResponse(ex);
            }
        }


        public async Task<IResponse<RepeatCustomerRateDto>> GetRepeatCustomerRateAsync(int months = 6)
        {
            Response<RepeatCustomerRateDto> output = new();
            if (months <= 0)
            {
                months = 6;
            }

            try
            {
                DateTime utcNow = DateTime.UtcNow;
                DateTime currentMonthStart = new DateTime(utcNow.Year, utcNow.Month, 1);
                DateTime startPeriod = currentMonthStart.AddMonths(1 - months);
                DateTime endPeriod = currentMonthStart.AddMonths(1);

                var allPayments = await _studentPaymentRepository
                    .Where(payment => payment.PaymentDate.HasValue && payment.StudentId.HasValue)
                    .Where(payment => payment.PaymentDate.Value < endPeriod)
                    .Select(payment => new
                    {
                        StudentId = payment.StudentId!.Value,
                        PaymentDate = payment.PaymentDate!.Value
                    })
                    .ToListAsync();

                var firstPaymentByStudent = allPayments
                    .GroupBy(payment => payment.StudentId)
                    .ToDictionary(group => group.Key, group => group.Min(entry => entry.PaymentDate));

                List<string> categories = new();
                List<decimal> rateSeries = new();

                for (int index = 0; index < months; index++)
                {
                    DateTime monthStart = new DateTime(startPeriod.Year, startPeriod.Month, 1).AddMonths(index);
                    DateTime nextMonthStart = monthStart.AddMonths(1);

                    categories.Add(monthStart.ToString("MMM yyyy", CultureInfo.InvariantCulture));

                    var monthPayments = allPayments
                        .Where(payment => payment.PaymentDate >= monthStart && payment.PaymentDate < nextMonthStart)
                        .ToList();

                    if (monthPayments.Count == 0)
                    {
                        rateSeries.Add(0m);
                        continue;
                    }

                    int totalCustomers = monthPayments
                        .Select(payment => payment.StudentId)
                        .Distinct()
                        .Count();

                    if (totalCustomers == 0)
                    {
                        rateSeries.Add(0m);
                        continue;
                    }

                    int returningCustomers = monthPayments
                        .Select(payment => payment.StudentId)
                        .Distinct()
                        .Count(studentId => firstPaymentByStudent.TryGetValue(studentId, out DateTime firstPaymentDate) && firstPaymentDate < monthStart);

                    decimal returningRate = totalCustomers == 0
                        ? 0m
                        : Math.Round((decimal)returningCustomers / totalCustomers * 100m, 2, MidpointRounding.AwayFromZero);

                    rateSeries.Add(returningRate);
                }

                decimal currentRate = rateSeries.LastOrDefault();
                decimal previousRate = rateSeries.Count > 1 ? rateSeries[^2] : 0m;

                RepeatCustomerRateDto dto = new()
                {
                    Chart = new ChartDto
                    {
                        Categories = categories,
                        Series = new List<ChartSeriesDto>
                        {
                            new ChartSeriesDto
                            {
                                Name = "Returning Customers %",
                                Data = rateSeries
                            }
                        }
                    },
                    CurrentRate = currentRate,
                    PreviousRate = previousRate,
                    RateChange = CalculatePercentageChange(currentRate, previousRate)
                };

                return output.CreateResponse(dto);
            }
            catch (Exception ex)
            {
                return output.CreateResponse(ex);
            }
        }

        public async Task<IResponse<MonthlyRevenueChartDto>> GetMonthlyRevenueAsync(int months = 6)
        {
            Response<MonthlyRevenueChartDto> output = new();
            if (months <= 0)
            {
                months = 6;
            }

            try
            {
                DateTime utcNow = DateTime.UtcNow;
                DateTime currentMonthStart = new DateTime(utcNow.Year, utcNow.Month, 1);
                DateTime startPeriod = currentMonthStart.AddMonths(1 - months);
                DateTime endPeriod = currentMonthStart.AddMonths(1);

                var revenueData = await ApplyPaymentScope(_studentPaymentRepository.GetAll().AsNoTracking(), new DashboardScope())
                    .Where(payment => payment.PaymentDate.HasValue && payment.Amount.HasValue &&
                                       payment.PaymentDate.Value >= startPeriod && payment.PaymentDate.Value < endPeriod)
                    .GroupBy(payment => new { payment.PaymentDate!.Value.Year, payment.PaymentDate!.Value.Month })
                    .Select(group => new
                    {
                        group.Key.Year,
                        group.Key.Month,
                        Total = group.Sum(payment => (decimal?)(payment.Amount ?? 0)) ?? 0m
                    })
                    .ToListAsync();

                var teacherData = await ApplyTeacherSalaryScope(_teacherSalaryRepository.GetAll().AsNoTracking(), new DashboardScope())
                    .Where(salary => salary.Month.HasValue && salary.Month.Value >= startPeriod && salary.Month.Value < endPeriod)
                    .GroupBy(salary => new { salary.Month!.Value.Year, salary.Month!.Value.Month })
                    .Select(group => new
                    {
                        group.Key.Year,
                        group.Key.Month,
                        Total = group.Sum(salary => salary.Sallary ?? 0d)
                    })
                    .ToListAsync();

                var managerData = await ApplyManagerSalaryScope(_managerSalaryRepository.GetAll().AsNoTracking(), new DashboardScope())
                    .Where(salary => salary.Month.HasValue && salary.Month.Value >= startPeriod && salary.Month.Value < endPeriod)
                    .GroupBy(salary => new { salary.Month!.Value.Year, salary.Month!.Value.Month })
                    .Select(group => new
                    {
                        group.Key.Year,
                        group.Key.Month,
                        Total = group.Sum(salary => salary.Sallary ?? 0d)
                    })
                    .ToListAsync();

                List<string> categories = new();
                List<decimal> revenueSeries = new();
                List<decimal> teacherSeries = new();
                List<decimal> managerSeries = new();
                List<decimal> netSeries = new();

                decimal totalRevenue = 0m;
                decimal totalTeacher = 0m;
                decimal totalManager = 0m;

                for (int index = 0; index < months; index++)
                {
                    DateTime monthStart = new DateTime(startPeriod.Year, startPeriod.Month, 1).AddMonths(index);
                    string monthLabel = monthStart.ToString("MMM yyyy", CultureInfo.InvariantCulture);
                    categories.Add(monthLabel);

                    decimal monthRevenue = revenueData
                        .Where(entry => entry.Year == monthStart.Year && entry.Month == monthStart.Month)
                        .Select(entry => entry.Total)
                        .FirstOrDefault();

                    decimal monthTeacher = teacherData
                        .Where(entry => entry.Year == monthStart.Year && entry.Month == monthStart.Month)
                        .Select(entry => Convert.ToDecimal(entry.Total))
                        .FirstOrDefault();

                    decimal monthManager = managerData
                        .Where(entry => entry.Year == monthStart.Year && entry.Month == monthStart.Month)
                        .Select(entry => Convert.ToDecimal(entry.Total))
                        .FirstOrDefault();

                    decimal monthNet = monthRevenue - monthTeacher - monthManager;

                    totalRevenue += monthRevenue;
                    totalTeacher += monthTeacher;
                    totalManager += monthManager;

                    revenueSeries.Add(Round(monthRevenue));
                    teacherSeries.Add(Round(monthTeacher));
                    managerSeries.Add(Round(monthManager));
                    netSeries.Add(Round(monthNet));
                }

                MonthlyRevenueChartDto dto = new()
                {
                    Chart = new ChartDto
                    {
                        Categories = categories,
                        Series = new List<ChartSeriesDto>
                        {
                            new ChartSeriesDto { Name = "Revenue", Data = revenueSeries },
                            new ChartSeriesDto { Name = "Teacher Salaries", Data = teacherSeries },
                            new ChartSeriesDto { Name = "Manager Salaries", Data = managerSeries },
                            new ChartSeriesDto { Name = "Net Income", Data = netSeries }
                        }
                    },
                    TotalRevenue = Round(totalRevenue),
                    TotalTeacherPayout = Round(totalTeacher),
                    TotalManagerPayout = Round(totalManager),
                    TotalNetIncome = Round(totalRevenue - totalTeacher - totalManager)
                };

                return output.CreateResponse(dto);
            }
            catch (Exception ex)
            {
                return output.CreateResponse(ex);
            }
        }

        public async Task<IResponse<SubscriberTypeAnalyticsDto>> GetSubscribersByTypeAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            Response<SubscriberTypeAnalyticsDto> output = new();

            try
            {
                var paymentsQuery = _studentPaymentRepository
                    .Where(payment => payment.StudentId.HasValue && payment.StudentSubscribeId.HasValue);

                if (startDate.HasValue)
                {
                    DateTime start = startDate.Value;
                    paymentsQuery = paymentsQuery
                        .Where(payment => payment.PaymentDate.HasValue && payment.PaymentDate.Value >= start);
                }

                if (endDate.HasValue)
                {
                    DateTime end = endDate.Value;
                    paymentsQuery = paymentsQuery
                        .Where(payment => payment.PaymentDate.HasValue && payment.PaymentDate.Value < end);
                }

                var subscriberTypeData = await (
                    from payment in paymentsQuery
                    join subscription in _subscribeRepository.GetAll()
                        on payment.StudentSubscribeId equals subscription.Id
                    join subscribeType in _subscribeTypeRepository.GetAll()
                        on subscription.SubscribeTypeId equals subscribeType.Id into typeGroup
                    from subscribeType in typeGroup.DefaultIfEmpty()
                    select new
                    {
                        StudentId = payment.StudentId!.Value,
                        TypeId = subscription.SubscribeTypeId,
                        SubscribeTypeName = subscribeType != null ? subscribeType.Name : null,
                        SubscriptionName = subscription.Name
                    }).ToListAsync();

                var normalizedData = subscriberTypeData
                    .Select(entry =>
                    {
                        string label = entry.SubscribeTypeName ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(label))
                        {
                            label = entry.SubscriptionName ?? string.Empty;
                        }

                        if (string.IsNullOrWhiteSpace(label))
                        {
                            label = "Uncategorized";
                        }

                        return new
                        {
                            entry.StudentId,
                            entry.TypeId,
                            TypeName = label
                        };
                    })
                    .ToList();

                var breakdown = normalizedData
                    .GroupBy(entry => new { entry.TypeId, entry.TypeName })
                    .Select(group => new SubscriberTypeBreakdownDto
                    {
                        SubscribeTypeId = group.Key.TypeId,
                        TypeName = group.Key.TypeName,
                        SubscriberCount = group.Select(entry => entry.StudentId).Distinct().Count()
                    })
                    .OrderByDescending(dto => dto.SubscriberCount)
                    .ToList();

                int totalSubscriptions = breakdown.Sum(entry => entry.SubscriberCount);
                int uniqueSubscribers = normalizedData.Select(entry => entry.StudentId).Distinct().Count();

                foreach (var item in breakdown)
                {
                    item.Percentage = totalSubscriptions == 0
                        ? 0m
                        : Math.Round((decimal)item.SubscriberCount / totalSubscriptions * 100m, 2, MidpointRounding.AwayFromZero);
                }

                ChartDto chart = new()
                {
                    Categories = breakdown.Select(entry => entry.TypeName).ToList(),
                    Series = new List<ChartSeriesDto>
                    {
                        new ChartSeriesDto
                        {
                            Name = "Subscribers",
                            Data = breakdown.Select(entry => (decimal)entry.SubscriberCount).ToList()
                        }
                    }
                };

                PieChartDto pie = new()
                {
                    TotalValue = totalSubscriptions,
                    Slices = breakdown.Select(entry => new PieChartSliceDto
                    {
                        Label = entry.TypeName,
                        Value = entry.SubscriberCount,
                        Percentage = entry.Percentage
                    }).ToList()
                };

                SubscriberTypeAnalyticsDto dto = new()
                {
                    SubscribersByType = chart,
                    Distribution = pie,
                    Breakdown = breakdown,
                    TotalSubscribers = totalSubscriptions,
                    UniqueSubscribers = uniqueSubscribers,
                    TotalSubscriptionTypes = breakdown.Count,
                    StartDate = startDate,
                    EndDate = endDate
                };

                return output.CreateResponse(dto);
            }
            catch (Exception ex)
            {
                return output.CreateResponse(ex);
            }
        }

        public async Task<IResponse<PieChartDto>> GetRevenueByCurrencyAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            Response<PieChartDto> output = new();
            try
            {
                var paymentsQuery = _studentPaymentRepository
                    .Where(payment => payment.Amount.HasValue && payment.PaymentDate.HasValue && payment.CurrencyId.HasValue);

                if (startDate.HasValue)
                {
                    paymentsQuery = paymentsQuery.Where(payment => payment.PaymentDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    paymentsQuery = paymentsQuery.Where(payment => payment.PaymentDate < endDate.Value);
                }

                var groupedPayments = await paymentsQuery
                    .GroupBy(payment => payment.CurrencyId!.Value)
                    .Select(group => new
                    {
                        CurrencyId = group.Key,
                        Total = group.Sum(payment => (decimal?)(payment.Amount ?? 0)) ?? 0m
                    })
                    .ToListAsync();

                decimal totalValue = groupedPayments.Sum(entry => entry.Total);

                List<PieChartSliceDto> slices = groupedPayments
                    .Select(entry =>
                    {
                        string label = CurrencyLabels.TryGetValue(entry.CurrencyId, out string? mappedLabel)
                            ? mappedLabel
                            : $"Currency {entry.CurrencyId}";

                        decimal percentage = totalValue == 0m
                            ? 0m
                            : Math.Round(entry.Total / totalValue * 100m, 2, MidpointRounding.AwayFromZero);

                        return new PieChartSliceDto
                        {
                            Label = label,
                            Value = Round(entry.Total),
                            Percentage = percentage
                        };
                    })
                    .ToList();

                PieChartDto dto = new()
                {
                    Slices = slices,
                    TotalValue = Round(totalValue)
                };

                return output.CreateResponse(dto);
            }
            catch (Exception ex)
            {
                return output.CreateResponse(ex);
            }
        }

        private async Task<List<DashboardMonthlyRevenuePointDto>> BuildMonthlyRevenueAsync(
            DateTime referenceEnd,
            IQueryable<StudentPayment> paymentsBaseQuery,
            IQueryable<TeacherSallary> teacherSalaryBaseQuery,
            IQueryable<ManagerSallary> managerSalaryBaseQuery,
            int months = 6)
        {
            months = Math.Max(1, months);

            List<DashboardMonthlyRevenuePointDto> results = new();

            DateTime anchorMonthStart = new DateTime(referenceEnd.Year, referenceEnd.Month, 1);

            for (int index = months - 1; index >= 0; index--)
            {
                DateTime monthStart = anchorMonthStart.AddMonths(-index);
                DateTime monthEndExclusive = monthStart.AddMonths(1);

                decimal earningsRaw = await paymentsBaseQuery
                    .Where(p =>
                        (p.PaymentDate ?? p.CreatedAt) >= monthStart &&
                        (p.PaymentDate ?? p.CreatedAt) < monthEndExclusive)
                    .SumAsync(p => (decimal?)(p.Amount ?? 0)) ?? 0m;

                double teacherRawDouble = await teacherSalaryBaseQuery
                    .Where(s => s.Month.HasValue && s.Month.Value >= monthStart && s.Month.Value < monthEndExclusive)
                    .SumAsync(s => (double?)(s.Sallary ?? 0d)) ?? 0d;

                double managerRawDouble = await managerSalaryBaseQuery
                    .Where(s => s.Month.HasValue && s.Month.Value >= monthStart && s.Month.Value < monthEndExclusive)
                    .SumAsync(s => (double?)(s.Sallary ?? 0d)) ?? 0d;

                decimal teacherRaw = Convert.ToDecimal(teacherRawDouble);
                decimal managerRaw = Convert.ToDecimal(managerRawDouble);
                decimal netRaw = earningsRaw - teacherRaw;

                results.Add(new DashboardMonthlyRevenuePointDto
                {
                    Month = monthStart.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                    Earnings = Round(earningsRaw),
                    TeacherPayout = Round(teacherRaw),
                    ManagerPayout = Round(managerRaw),
                    NetIncome = Round(netRaw)
                });
            }

            return results;
        }

        private async Task<List<DashboardTransactionDto>> LoadRecentTransactionsAsync(IQueryable<StudentPayment> paymentsBaseQuery, int take = 10)
        {
            var recentPayments = await paymentsBaseQuery
                .Where(p => p.PaymentDate.HasValue || p.CreatedAt.HasValue)
                .OrderByDescending(p => p.PaymentDate ?? p.CreatedAt)
                .Take(take)
                .Select(p => new
                {
                    p.Id,
                    p.Amount,
                    p.CurrencyId,
                    p.PaymentDate,
                    p.CreatedAt,
                    p.PayStatue,
                    p.IsCancelled,
                    StudentName = p.Student != null ? p.Student.FullName : null,
                    StudentEmail = p.Student != null ? p.Student.Email : null
                })
                .ToListAsync();

            return recentPayments
                .Select(entry => new DashboardTransactionDto
                {
                    Id = entry.Id,
                    Amount = Round(Convert.ToDecimal(entry.Amount ?? 0)),
                    Currency = entry.CurrencyId.HasValue && CurrencyLabels.TryGetValue(entry.CurrencyId.Value, out var label)
                        ? label
                        : "N/A",
                    Date = entry.PaymentDate ?? entry.CreatedAt,
                    Status = entry.IsCancelled == true
                        ? "cancelled"
                        : entry.PayStatue == true
                            ? "paid"
                            : entry.PayStatue == false
                                ? "failed"
                                : "pending",
                    Student = !string.IsNullOrWhiteSpace(entry.StudentName)
                        ? entry.StudentName
                        : (!string.IsNullOrWhiteSpace(entry.StudentEmail) ? entry.StudentEmail : $"Student #{entry.Id}")
                })
                .ToList();
        }

        private static DashboardScope CreateScope(UserTypesEnum role, int? branchId, int userId)
        {
            return role switch
            {
                UserTypesEnum.BranchLeader => new DashboardScope { BranchId = branchId },
                UserTypesEnum.Manager => new DashboardScope { ManagerId = userId, BranchId = branchId },
                UserTypesEnum.Teacher => new DashboardScope { TeacherId = userId, BranchId = branchId },
                _ => new DashboardScope { BranchId = branchId }
            };
        }

        private static string MapRoleName(UserTypesEnum role)
        {
            return role switch
            {
                UserTypesEnum.Admin => "Admin",
                UserTypesEnum.BranchLeader => "BranchManager",
                UserTypesEnum.Manager => "Supervisor",
                UserTypesEnum.Teacher => "Teacher",
                _ => role.ToString()
            };
        }

        private static string ResolveCurrencyCode(int currencyId)
        {
            return CurrencyLabels.TryGetValue(currencyId, out var label) ? label : DefaultCurrencyCode;
        }

        private static DashboardRange ResolveRange(DashboardRangeInputDto? range)
        {
            DateTime utcNow = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(range?.Range))
            {
                var requested = range.Range!.Trim();
                if (string.Equals(requested, "monthly", StringComparison.OrdinalIgnoreCase))
                {
                    DateTime start = new DateTime(utcNow.Year, utcNow.Month, 1);
                    return new DashboardRange(start, start.AddMonths(1), start.AddMonths(1));
                }

                if (string.Equals(requested, "last30d", StringComparison.OrdinalIgnoreCase))
                {
                    DateTime end = utcNow.Date.AddDays(1);
                    return new DashboardRange(end.AddDays(-30), end, end);
                }

                var rangeParts = requested.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (rangeParts.Length == 2 && DateTime.TryParse(rangeParts[0], out var parsedStart) && DateTime.TryParse(rangeParts[1], out var parsedEnd))
                {
                    parsedStart = parsedStart.ToUniversalTime();
                    parsedEnd = parsedEnd.ToUniversalTime();
                    if (parsedStart > parsedEnd)
                    {
                        (parsedStart, parsedEnd) = (parsedEnd, parsedStart);
                    }

                    return new DashboardRange(parsedStart.Date, parsedEnd.Date.AddDays(1), parsedEnd);
                }
            }

            DateTime referenceEnd = (range?.EndDate?.ToUniversalTime() ?? utcNow);
            DateTime startCandidate = (range?.StartDate?.ToUniversalTime() ?? referenceEnd.AddDays(-29));

            if (startCandidate > referenceEnd)
            {
                (startCandidate, referenceEnd) = (referenceEnd, startCandidate);
            }

            DateTime rangeStart = startCandidate.Date;
            DateTime rangeEndExclusive = referenceEnd.Date.AddDays(1);

            return new DashboardRange(rangeStart, rangeEndExclusive, referenceEnd);
        }

        private IQueryable<User> CreateScopedBranchLeadersQuery(DashboardScope scope)
        {
            var query = _UserRepository.GetAll()
                .AsNoTracking()
                .Where(u => u.UserTypeId == (int)UserTypesEnum.BranchLeader && !u.IsDeleted && !u.Inactive);

            if (scope.BranchId.HasValue)
            {
                query = query.Where(u => u.BranchId == scope.BranchId.Value);
            }

            return query;
        }

        private IQueryable<User> CreateScopedManagersQuery(DashboardScope scope)
        {
            var query = _UserRepository.GetAll()
                .AsNoTracking()
                .Where(u => u.UserTypeId == (int)UserTypesEnum.Manager && !u.IsDeleted && !u.Inactive);

            if (scope.BranchId.HasValue)
            {
                query = query.Where(u => u.BranchId == scope.BranchId.Value);
            }

            if (scope.ManagerId.HasValue)
            {
                query = query.Where(u => u.Id == scope.ManagerId.Value);
            }

            return query;
        }

        private IQueryable<User> CreateScopedTeachersQuery(DashboardScope scope)
        {
            var query = _UserRepository.GetAll()
                .AsNoTracking()
                .Where(u => u.UserTypeId == (int)UserTypesEnum.Teacher && !u.IsDeleted && !u.Inactive);

            if (scope.BranchId.HasValue)
            {
                query = query.Where(u => u.BranchId == scope.BranchId.Value);
            }

            if (scope.ManagerId.HasValue)
            {
                query = query.Where(u => u.ManagerId == scope.ManagerId.Value);
            }

            if (scope.TeacherId.HasValue)
            {
                query = query.Where(u => u.Id == scope.TeacherId.Value);
            }

            return query;
        }

        private IQueryable<User> CreateScopedStudentsQuery(DashboardScope scope)
        {
            var query = _UserRepository.GetAll()
                .AsNoTracking()
                .Where(u => u.UserTypeId == (int)UserTypesEnum.Student && !u.IsDeleted && !u.Inactive);

            if (scope.BranchId.HasValue)
            {
                query = query.Where(u => u.BranchId == scope.BranchId.Value);
            }

            if (scope.ManagerId.HasValue)
            {
                query = query.Where(u => u.ManagerId == scope.ManagerId.Value);
            }

            if (scope.TeacherId.HasValue)
            {
                query = query.Where(u => u.TeacherId == scope.TeacherId.Value);
            }

            return query;
        }

        private static IQueryable<StudentPayment> ApplyPaymentScope(IQueryable<StudentPayment> query, DashboardScope scope)
        {
            query = query
                .Where(p => p.Amount.HasValue)
                .Where(p => p.PayStatue == true)
                .Where(p => !(p.IsCancelled ?? false))
                .Where(p => p.Student != null && !p.Student.IsDeleted && !p.Student.Inactive);

            if (scope.BranchId.HasValue)
            {
                int branchId = scope.BranchId.Value;
                query = query.Where(p => p.Student!.BranchId == branchId);
            }

            if (scope.ManagerId.HasValue)
            {
                int managerId = scope.ManagerId.Value;
                query = query.Where(p => p.Student!.ManagerId == managerId);
            }

            if (scope.TeacherId.HasValue)
            {
                int teacherId = scope.TeacherId.Value;
                query = query.Where(p => p.Student!.TeacherId == teacherId);
            }

            return query;
        }

        private static IQueryable<TeacherSallary> ApplyTeacherSalaryScope(IQueryable<TeacherSallary> query, DashboardScope scope)
        {
            query = query.Where(s => s.Sallary.HasValue)
                         .Where(s => s.IsPayed == true)
                         .Where(s => s.IsDeleted == null || !s.IsDeleted.Value);

            if (scope.BranchId.HasValue)
            {
                int branchId = scope.BranchId.Value;
                query = query.Where(s => s.Teacher != null && s.Teacher.BranchId == branchId);
            }

            if (scope.ManagerId.HasValue)
            {
                int managerId = scope.ManagerId.Value;
                query = query.Where(s => s.Teacher != null && s.Teacher.ManagerId == managerId);
            }

            if (scope.TeacherId.HasValue)
            {
                int teacherId = scope.TeacherId.Value;
                query = query.Where(s => s.TeacherId == teacherId);
            }

            return query;
        }

        private static IQueryable<ManagerSallary> ApplyManagerSalaryScope(IQueryable<ManagerSallary> query, DashboardScope scope)
        {
            query = query.Where(s => s.Sallary.HasValue)
                         .Where(s => s.IsPayed == true);

            if (scope.BranchId.HasValue)
            {
                int branchId = scope.BranchId.Value;
                query = query.Where(s => s.Manager != null && s.Manager.BranchId == branchId);
            }

            if (scope.ManagerId.HasValue)
            {
                int managerId = scope.ManagerId.Value;
                query = query.Where(s => s.ManagerId == managerId);
            }

            return query;
        }

        private static IQueryable<CircleReport> ApplyCircleReportScope(IQueryable<CircleReport> query, DashboardScope scope)
        {
            query = query.Where(r => !r.IsDeleted && !r.IsPermanentlyDeleted);

            if (scope.BranchId.HasValue)
            {
                int branchId = scope.BranchId.Value;
                query = query.Where(r =>
                    r.Circle != null && r.Circle.BranchId.HasValue && r.Circle.BranchId == branchId);
            }

            if (scope.ManagerId.HasValue)
            {
                int managerId = scope.ManagerId.Value;
                query = query.Where(r =>
                    (r.Student != null && r.Student.ManagerId == managerId) ||
                    (r.Teacher != null && r.Teacher.ManagerId == managerId) ||
                    (r.Circle != null && r.Circle.ManagerCircles.Any(mc => mc.ManagerId == managerId)));
            }

            if (scope.TeacherId.HasValue)
            {
                int teacherId = scope.TeacherId.Value;
                query = query.Where(r => r.TeacherId == teacherId);
            }

            return query;
        }

        private static IQueryable<Circle> ApplyCircleScope(IQueryable<Circle> query, DashboardScope scope)
        {
            query = query.Where(c => c.IsDeleted == null || !c.IsDeleted.Value);

            if (scope.BranchId.HasValue)
            {
                int branchId = scope.BranchId.Value;
                query = query.Where(c => c.BranchId.HasValue && c.BranchId == branchId);
            }

            if (scope.ManagerId.HasValue)
            {
                int managerId = scope.ManagerId.Value;
                query = query.Where(c => c.ManagerCircles.Any(mc => mc.ManagerId == managerId));
            }

            if (scope.TeacherId.HasValue)
            {
                int teacherId = scope.TeacherId.Value;
                query = query.Where(c => c.TeacherId == teacherId);
            }

            return query;
        }

        private sealed class DashboardScope
        {
            public int? BranchId { get; init; }
            public int? ManagerId { get; init; }
            public int? TeacherId { get; init; }
        }

        private sealed class DashboardRange
        {
            public DashboardRange(DateTime start, DateTime endExclusive, DateTime referenceEnd)
            {
                Start = start;
                EndExclusive = endExclusive;
                ReferenceEnd = referenceEnd;
            }

            public DateTime Start { get; }
            public DateTime EndExclusive { get; }
            public DateTime ReferenceEnd { get; }
            public DateTime EndInclusive => EndExclusive.AddTicks(-1);
        }

        private static UserTypesEnum ResolveUserRole(string? requestedRole, UserTypesEnum actualRole)
        {
            if (!string.IsNullOrWhiteSpace(requestedRole) && Enum.TryParse<UserTypesEnum>(requestedRole, true, out var parsed))
            {
                return parsed;
            }

            return actualRole;
        }

        private static string ResolveRangeLabel(DashboardRange rangeInfo, string? requestedRange)
        {
            if (!string.IsNullOrWhiteSpace(requestedRange))
            {
                return requestedRange!;
            }

            int days = (int)(rangeInfo.EndExclusive - rangeInfo.Start).TotalDays;
            return days switch
            {
                <= 1 => "today",
                <= 7 => "last7d",
                <= 30 => "last30d",
                _ => "custom"
            };
        }

        private static string FormatDate(DateTime date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        private static string? FormatTime(TimeSpan? time) => time?.ToString("HH\\:mm", CultureInfo.InvariantCulture);

        private async Task<List<DashboardOverviewMonthlyRevenueDto>> BuildOverviewMonthlyRevenueAsync(
            DashboardRange range,
            IQueryable<StudentPayment> paymentsQuery,
            IQueryable<TeacherSallary> teacherSalaryQuery,
            IQueryable<ManagerSallary> managerSalaryQuery,
            IQueryable<CircleReport> circleReportQuery)
        {
            List<DashboardOverviewMonthlyRevenueDto> result = new();

            DateTime monthCursor = new DateTime(range.Start.Year, range.Start.Month, 1);
            DateTime endCursor = new DateTime(range.EndExclusive.Year, range.EndExclusive.Month, 1);

            while (monthCursor <= endCursor)
            {
                DateTime monthStart = monthCursor;
                DateTime nextMonth = monthStart.AddMonths(1);

                decimal earnings = await paymentsQuery
                    .Where(p => (p.PaymentDate ?? p.CreatedAt) >= monthStart && (p.PaymentDate ?? p.CreatedAt) < nextMonth)
                    .SumAsync(p => (decimal?)(p.Amount ?? 0m)) ?? 0m;

                decimal teacherPayout = await teacherSalaryQuery
                    .Where(s => s.Month.HasValue && s.Month.Value >= monthStart && s.Month.Value < nextMonth)
                    .SumAsync(s => (decimal?)(s.Sallary ?? 0d)) ?? 0m;

                decimal managerPayout = await managerSalaryQuery
                    .Where(s => s.Month.HasValue && s.Month.Value >= monthStart && s.Month.Value < nextMonth)
                    .SumAsync(s => (decimal?)(s.Sallary ?? 0d)) ?? 0m;

                int reports = await circleReportQuery
                    .Where(r => r.CreationTime >= monthStart && r.CreationTime < nextMonth)
                    .CountAsync();

                decimal netIncome = Round(earnings - teacherPayout - managerPayout);

                if (earnings != 0 || netIncome != 0 || reports != 0)
                {
                    result.Add(new DashboardOverviewMonthlyRevenueDto
                    {
                        Month = monthStart.ToString("MMM", CultureInfo.InvariantCulture),
                        Earnings = Round(earnings),
                        NetIncome = netIncome,
                        Reports = reports
                    });
                }

                monthCursor = nextMonth;
            }

            return result;
        }

        private async Task<List<DashboardOverviewTransactionDto>> LoadOverviewTransactionsAsync(IQueryable<StudentPayment> paymentsBaseQuery)
        {
            return await paymentsBaseQuery
                .OrderByDescending(p => p.PaymentDate ?? p.CreatedAt)
                .Take(10)
                .Select(entry => new DashboardOverviewTransactionDto
                {
                    Id = entry.Id,
                    Amount = Round(entry.Amount ?? 0m),
                    Currency = ResolveCurrencyCode(entry.CurrencyId ?? (int)CurrencyEnum.EGP),
                    Date = FormatDate((entry.PaymentDate ?? entry.CreatedAt) ?? DateTime.UtcNow),
                    Status = entry.IsCancelled == true
                        ? "failed"
                        : entry.PayStatue == true
                            ? "completed"
                            : entry.PayStatue == false
                                ? "failed"
                                : "pending",
                    Student = !string.IsNullOrWhiteSpace(entry.Student!.FullName)
                        ? entry.Student!.FullName
                        : (!string.IsNullOrWhiteSpace(entry.Student!.Email) ? entry.Student!.Email : $"Student #{entry.Student.Id}")
                })
                .ToListAsync();
        }

        private (DashboardUpcomingCircleDto Dto, DateTime? NextOccurrence) BuildUpcomingDashboardCircle(Circle circle, DateTime reference)
        {
            DateTime? next = null;
            string? day = null;
            string? startTime = null;

            foreach (var schedule in circle.CircleDays)
            {
                var nextCandidate = CalculateNextOccurrenceForDay(reference, schedule.DayId, schedule.Time);
                if (!nextCandidate.HasValue)
                {
                    continue;
                }

                if (!next.HasValue || nextCandidate < next)
                {
                    next = nextCandidate;
                    day = ResolveDayName(schedule.DayId);
                    startTime = FormatTime(schedule.Time);
                }
            }

            DashboardUpcomingCircleDto dto = new()
            {
                Id = circle.Id,
                Name = circle.Name ?? string.Empty,
                Day = day,
                StartTime = startTime,
                EndTime = null,
                Teacher = circle.Teacher?.FullName,
                Managers = circle.ManagerCircles?
                    .Where(mc => mc.Manager != null && mc.Manager.IsDeleted != true)
                    .Select(mc => mc.Manager!.FullName ?? string.Empty)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .ToList() ?? new List<string>()
            };

            return (dto, next);
        }

        private static DateTime? CalculateNextOccurrenceForDay(DateTime reference, int? dayId, TimeSpan? time)
        {
            if (!dayId.HasValue || !time.HasValue)
            {
                return null;
            }

            var desiredDay = (DayOfWeek)(dayId.Value % 7);
            int offset = ((int)desiredDay - (int)reference.DayOfWeek + 7) % 7;
            if (offset == 0 && reference.TimeOfDay > time.Value)
            {
                offset = 7;
            }

            return reference.Date.AddDays(offset).Add(time.Value);
        }

        private static string? ResolveDayName(int? dayId)
        {
            return dayId.HasValue ? ((DayOfWeek)(dayId.Value % 7)).ToString().ToLowerInvariant() : null;
        }

        private static decimal CalculatePercentageChange(decimal current, decimal previous)
        {
            if (previous == 0m)
            {
                return current == 0m ? 0m : 100m;
            }

            decimal difference = current - previous;
            return Math.Round(difference / Math.Abs(previous) * 100m, 2, MidpointRounding.AwayFromZero);
        }

        private static decimal Round(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }
    }
}
