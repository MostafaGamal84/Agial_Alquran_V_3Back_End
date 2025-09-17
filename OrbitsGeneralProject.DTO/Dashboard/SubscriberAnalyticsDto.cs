using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.DTO.Dashboard
{
    public class SubscriberTypeAnalyticsDto
    {
        public ChartDto SubscribersByType { get; set; } = new();

        public PieChartDto Distribution { get; set; } = new();

        public List<SubscriberTypeBreakdownDto> Breakdown { get; set; } = new();

        public int TotalSubscribers { get; set; }

        public int UniqueSubscribers { get; set; }

        public int TotalSubscriptionTypes { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }

    public class SubscriberTypeBreakdownDto
    {
        public int? SubscribeTypeId { get; set; }

        public string TypeName { get; set; } = string.Empty;

        public int SubscriberCount { get; set; }

        public decimal Percentage { get; set; }
    }
}

