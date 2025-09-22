using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Orbits.GeneralProject.DTO.CircleDto
{
    public class UpcomingCircleDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? NextDayId { get; set; }
        public string? NextDayName { get; set; }

        [JsonPropertyName("day")]
        public ICollection<int>? DayIds { get; set; }

        [JsonPropertyName("dayName")]
        public ICollection<string>? DayNames { get; set; }
        public DateTime? NextOccurrenceDate { get; set; }
        [JsonPropertyName("time")]
        public TimeSpan? StartTime { get; set; }
        public int? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public ICollection<ManagerCirclesDto>? Managers { get; set; }
    }
}
