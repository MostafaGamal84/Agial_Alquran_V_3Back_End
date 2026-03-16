using System;

namespace Orbits.GeneralProject.DTO.CircleReportDtos
{
    public class CircleReportStatsDto
    {
        public int TotalReports { get; set; }
        public int AttendedCount { get; set; }
        public int ExcusedAbsenceCount { get; set; }
        public int UnexcusedAbsenceCount { get; set; }
        public int? TeacherId { get; set; }
        public int? StudentId { get; set; }
        public DateTime? Month { get; set; }
    }
}
