using System;
using System.Text.Json.Serialization;

namespace Orbits.GeneralProject.DTO.CircleDto
{
    public class UpdateCircleDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? TeacherId { get; set; }

        [JsonPropertyName("day")]
        public int? DayId { get; set; }

        [JsonPropertyName("time")]
        public TimeSpan? StartTime { get; set; }

        public List<int>? Managers { get; set; }

        public List<int>? StudentsIds { get; set; } = new List<int>();
    }
}