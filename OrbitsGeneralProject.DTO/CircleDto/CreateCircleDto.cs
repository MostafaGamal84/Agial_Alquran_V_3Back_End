using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Orbits.GeneralProject.DTO.CircleDto
{
    public class CreateCircleDto
    {
        public string? Name { get; set; }

        public int? TeacherId { get; set; }

        [JsonPropertyName("days")]
        public List<CircleDayRequestDto>? Days { get; set; } = new List<CircleDayRequestDto>();

        public List<int>? Managers { get; set; } = new List<int>();

        public List<int>? StudentsIds { get; set; } = new List<int>();
    }
}
