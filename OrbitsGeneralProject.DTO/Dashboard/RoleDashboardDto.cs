using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.DTO.Dashboard
{
    public class DashboardRangeInputDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Range { get; set; }
    }

    public class RoleDashboardOverviewDto
    {
        public string Role { get; set; } = string.Empty;
        public DateTime RangeStart { get; set; }
        public DateTime RangeEnd { get; set; }
        public string? RangeLabel { get; set; }
        public DashboardRoleMetricsDto Metrics { get; set; } = new();
        public DashboardRoleChartsDto Charts { get; set; } = new();
    }

    public class DashboardRoleMetricsDto
    {
        public decimal? Earnings { get; set; }
        public int? NewStudents { get; set; }
        public int? CircleReports { get; set; }
        public decimal? NetIncome { get; set; }
        public int? BranchManagersCount { get; set; }
        public int? SupervisorsCount { get; set; }
        public int? TeachersCount { get; set; }
        public int? StudentsCount { get; set; }
        public int? CirclesCount { get; set; }
        public int? ReportsCount { get; set; }
    }

    public class DashboardRoleChartsDto
    {
        public List<DashboardMonthlyRevenuePointDto> MonthlyRevenue { get; set; } = new();
        public DashboardProjectOverviewDto? ProjectOverview { get; set; }
        public List<DashboardTransactionDto> Transactions { get; set; } = new();
    }

    public class DashboardMonthlyRevenuePointDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal Earnings { get; set; }
        public decimal TeacherPayout { get; set; }
        public decimal ManagerPayout { get; set; }
        public decimal NetIncome { get; set; }
    }

    public class DashboardProjectOverviewDto
    {
        public int TotalCircles { get; set; }
        public int ActiveCircles { get; set; }
        public int Teachers { get; set; }
        public int Students { get; set; }
        public int Reports { get; set; }
    }

    public class DashboardTransactionDto
    {
        public int Id { get; set; }
        public string? Student { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
