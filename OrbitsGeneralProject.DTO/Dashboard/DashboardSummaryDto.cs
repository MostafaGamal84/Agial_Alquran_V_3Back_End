using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.DTO.Dashboard
{
    public class DashboardSummaryDto
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public List<DashboardMetricDto> Metrics { get; set; } = new();
        public decimal LifetimeEarnings { get; set; }
        public decimal LifetimeTeacherPayouts { get; set; }
        public decimal LifetimeManagerPayouts { get; set; }
        public decimal LifetimeNetIncome { get; set; }
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalCircleReports { get; set; }
    }

    public class DashboardMetricDto
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ValueType { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal PreviousValue { get; set; }
        public decimal ChangePercentage { get; set; }
        public decimal? TotalValue { get; set; }
    }
}
