using Orbits.GeneralProject.Core.Entities;
using System;
using System.Text.Json.Serialization;

namespace Orbits.GeneralProject.DTO.CircleDto
{
    public class CreateCircleDto
    {
       

        public string? Name { get; set; }
        public int? Time { get; set; }

        public int? TeacherId { get; set; }

        public int? DayId { get; set; }

        public TimeSpan? StartTime { get; set; }

        public List<int>? Managers { get; set; } = new List<int>();

        public List<int>? StudentsIds { get; set; } = new List<int>();


    }
}