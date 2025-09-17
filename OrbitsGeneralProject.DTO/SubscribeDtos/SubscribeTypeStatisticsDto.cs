using System.Collections.Generic;

namespace Orbits.GeneralProject.DTO.SubscribeDtos
{
    public class SubscribeTypeStatisticsDto
    {
        public List<string> Labels { get; set; } = new();

        public List<int> Series { get; set; } = new();

        public List<SubscribeTypeStatisticItemDto> Items { get; set; } = new();

        public List<SubscribeTypeLegendDto> Legends { get; set; } = new();


        public int TotalSubscriptions { get; set; }

        public int UniqueSubscribers { get; set; }
    }

    public class SubscribeTypeStatisticItemDto
    {
        public int? SubscribeTypeId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int SubscriptionCount { get; set; }

        public int UniqueStudentCount { get; set; }

        public decimal Percentage { get; set; }
    }

    public class SubscribeTypeLegendDto
    {
        public string Name { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }

}
