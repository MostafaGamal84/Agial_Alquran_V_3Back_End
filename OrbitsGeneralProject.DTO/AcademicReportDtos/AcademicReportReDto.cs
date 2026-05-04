using System;

namespace Orbits.GeneralProject.DTO.AcademicReportDtos
{
    public class AcademicReportReDto
    {
        public int Id { get; set; }
        public int? AcademicCircleId { get; set; }
        public string? AcademicCircleName { get; set; }
        public int? StudentId { get; set; }
        public string? StudentName { get; set; }
        public int? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public int? SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public DateTime ReportDate { get; set; }
        public int? StageId { get; set; }
        public string? LessonTitle { get; set; }
        public int? StudentPerformanceId { get; set; }
        public int? PreviousHomeworkStatusId { get; set; }
        public int? HomeworkScore { get; set; }
        public string? NextHomework { get; set; }
        public string? TeacherNotes { get; set; }
        public int? SessionDurationMinutes { get; set; }
    }
}
