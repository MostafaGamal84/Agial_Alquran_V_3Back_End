using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Orbits.GeneralProject.DTO.CircleDto
{
    public class CreateCircleDto
    {


        public string? Name { get; set; }

        public int? TeacherId { get; set; }

        [JsonPropertyName("day")]
        public List<int>? DayIds { get; set; } = new List<int>();

        [JsonPropertyName("time")]
        public TimeSpan? StartTime { get; set; }

        public List<int>? Managers { get; set; } = new List<int>();

        public List<int>? StudentsIds { get; set; } = new List<int>();


    }
}