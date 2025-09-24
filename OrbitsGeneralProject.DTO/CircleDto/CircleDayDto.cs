using System;
using System.Text.Json.Serialization;

namespace Orbits.GeneralProject.DTO.CircleDto
{
    public class CircleDayRequestDto
    {
        [JsonPropertyName("dayId")]
        public int DayId { get; set; }

        [JsonPropertyName("time")]
        public TimeSpan? Time { get; set; }
    }

    public class CircleDayDto : CircleDayRequestDto
    {
        [JsonPropertyName("dayName")]
        public string? DayName { get; set; }
    }
}
