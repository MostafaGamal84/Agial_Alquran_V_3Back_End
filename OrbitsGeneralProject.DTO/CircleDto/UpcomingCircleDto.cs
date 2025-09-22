using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.DTO.CircleDto
{
    public class UpcomingCircleDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? DayId { get; set; }
        public string? DayName { get; set; }
        public DateTime? NextOccurrenceDate { get; set; }
        public int? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public ICollection<ManagerCirclesDto>? Managers { get; set; }
    }
}
