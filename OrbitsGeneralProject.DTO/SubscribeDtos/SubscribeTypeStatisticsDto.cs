using System.Collections.Generic;

namespace Orbits.GeneralProject.DTO.SubscribeDtos
{
    public class SubscribeTypeStatisticsDto
    {
        public SubscribeTypeDistributionDto Distribution { get; set; } = new();

        public List<SubscribeTypeBreakdownItemDto> Breakdown { get; set; } = new();

        public int TotalSubscribers { get; set; }

        public int UniqueSubscribers { get; set; }

        public int TotalSubscriptionTypes { get; set; }
    }

    public class SubscribeTypeDistributionDto
    {
        public int TotalValue { get; set; }

        public List<SubscribeTypeDistributionSliceDto> Slices { get; set; } = new();
    }

    public class SubscribeTypeDistributionSliceDto
    {
        public string Label { get; set; } = string.Empty;

        public int Value { get; set; }

        public decimal Percentage { get; set; }
    }

    public class SubscribeTypeBreakdownItemDto
    {
        public int SubscribeTypeId { get; set; }

        public string TypeName { get; set; } = string.Empty;

        public int SubscriberCount { get; set; }

        public decimal Percentage { get; set; }
    }
}
