using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Orbits.GeneralProject.BLL.BaseReponse;
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
            { 1, "EGP" },
            { 2, "SAR" },
            { 3, "USD" }
        };

        private readonly IRepository<StudentPayment> _studentPaymentRepository;
        private readonly IRepository<Student> _studentRepository;
        private readonly IRepository<Teacher> _teacherRepository;
        private readonly IRepository<CircleReport> _circleReportRepository;
        private readonly IRepository<TeacherSallary> _teacherSalaryRepository;
        private readonly IRepository<ManagerSallary> _managerSalaryRepository;
        private readonly IRepository<Subscribe> _subscribeRepository;
        private readonly IRepository<SubscribeType> _subscribeTypeRepository;

        public DashboardBLL(
            IMapper mapper,
            IRepository<StudentPayment> studentPaymentRepository,
            IRepository<Student> studentRepository,
            IRepository<Teacher> teacherRepository,
            IRepository<CircleReport> circleReportRepository,
            IRepository<TeacherSallary> teacherSalaryRepository,
            IRepository<ManagerSallary> managerSalaryRepository,
            IRepository<Subscribe> subscribeRepository,
            IRepository<SubscribeType> subscribeTypeRepository) : base(mapper)
        {
            _studentPaymentRepository = studentPaymentRepository;
            _studentRepository = studentRepository;
            _teacherRepository = teacherRepository;
            _circleReportRepository = circleReportRepository;
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

                int totalStudents = await _studentRepository.GetAll().CountAsync();
                int totalTeachers = await _teacherRepository.GetAll().CountAsync();
                int totalCircleReports = await _circleReportRepository.GetAll().CountAsync();

                int currentMonthNewStudents = await _studentRepository
                    .Where(student => student.CreatedAt.HasValue &&
                                       student.CreatedAt.Value >= currentMonthStart && student.CreatedAt.Value < nextMonthStart)
                    .CountAsync();

                int previousMonthNewStudents = await _studentRepository
                    .Where(student => student.CreatedAt.HasValue &&
                                       student.CreatedAt.Value >= previousMonthStart && student.CreatedAt.Value < currentMonthStart)
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

                var revenueData = await _studentPaymentRepository
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

                var teacherData = await _teacherSalaryRepository
                    .Where(salary => salary.Month.HasValue && salary.Month.Value >= startPeriod && salary.Month.Value < endPeriod)
                    .GroupBy(salary => new { salary.Month!.Value.Year, salary.Month!.Value.Month })
                    .Select(group => new
                    {
                        group.Key.Year,
                        group.Key.Month,
                        Total = group.Sum(salary => salary.Sallary ?? 0d)
                    })
                    .ToListAsync();

                var managerData = await _managerSalaryRepository
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
