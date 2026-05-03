using System;

namespace Orbits.GeneralProject.DTO.TeacherSallaryDtos
{
    /// <summary>
    /// Represents a detailed teacher report record used in salary calculation for a specific month.
    /// </summary>
    public class TeacherMonthlyReportRecordDto
    {
        public int Id { get; set; }
        public int TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public int? CircleReportId { get; set; }
        public int? CircleId { get; set; }
        public int? StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? SectionName { get; set; }
        public double Minutes { get; set; }
        public double Salary { get; set; }
        public int? AttendStatusId { get; set; }
        public DateTime? RecordCreatedAt { get; set; }
        public DateTime? CircleReportCreatedAt { get; set; }
    }
}
