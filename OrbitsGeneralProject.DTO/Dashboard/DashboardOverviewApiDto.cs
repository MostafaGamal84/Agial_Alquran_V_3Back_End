using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.DTO.Dashboard
{
    public class DashboardOverviewResponseDto
    {
        public string Role { get; set; } = string.Empty;
        public string RangeLabel { get; set; } = string.Empty;
        public string RangeStart { get; set; } = string.Empty;
        public string RangeEnd { get; set; } = string.Empty;
        public DashboardOverviewMetricsDto Metrics { get; set; } = new();
        public DashboardOverviewChartsDto Charts { get; set; } = new();
    }

    public class DashboardOverviewMetricsDto
    {
        public string CurrencyCode { get; set; } = string.Empty;

        public decimal Earnings { get; set; }
        public string EarningsCurrencyCode { get; set; } = string.Empty;
        public decimal EarningsPercentChange { get; set; }

        public int NewStudents { get; set; }
        public decimal NewStudentsPercentChange { get; set; }

        public int CircleReports { get; set; }
        public decimal CircleReportsPercentChange { get; set; }

        public decimal NetIncome { get; set; }
        public string NetIncomeCurrencyCode { get; set; } = string.Empty;
        public decimal NetIncomePercentChange { get; set; }

        public int BranchManagersCount { get; set; }
        public int SupervisorsCount { get; set; }
        public int TeachersCount { get; set; }
        public int StudentsCount { get; set; }
        public int CirclesCount { get; set; }
        public int ReportsCount { get; set; }

        public decimal Outgoing { get; set; }
        public string OutgoingCurrencyCode { get; set; } = string.Empty;
        public decimal IncomingEgp { get; set; }
        public string IncomingEgpCurrencyCode { get; set; } = string.Empty;
        public decimal IncomingSar { get; set; }
        public string IncomingSarCurrencyCode { get; set; } = string.Empty;
        public decimal IncomingUsd { get; set; }
        public string IncomingUsdCurrencyCode { get; set; } = string.Empty;
        public decimal NetProfit { get; set; }
        public string NetProfitCurrencyCode { get; set; } = string.Empty;
    }

    public class DashboardOverviewChartsDto
    {
        public List<DashboardOverviewMonthlyRevenueDto> MonthlyRevenue { get; set; } = new();
        public List<DashboardOverviewTransactionDto> Transactions { get; set; } = new();
    }

    public class DashboardOverviewMonthlyRevenueDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal Earnings { get; set; }
        public decimal NetIncome { get; set; }
        public int Reports { get; set; }
    }

    public class DashboardOverviewTransactionDto
    {
        public int Id { get; set; }
        public string Student { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class DashboardUpcomingCircleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Day { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public List<string> Managers { get; set; } = new();
        public string? Teacher { get; set; }
    }
}
