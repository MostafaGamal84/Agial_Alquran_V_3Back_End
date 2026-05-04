using System;

namespace Orbits.GeneralProject.DTO.AcademicReportDtos
{
    public class AcademicReportAddDto
    {
        public int? Id { get; set; }
        public int? AcademicCircleId { get; set; }
        public int? StudentId { get; set; }
        public int? TeacherId { get; set; }
        public int? SubjectId { get; set; }
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
