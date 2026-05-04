using System;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class AcademicReport : EntityBase
    {
        public int Id { get; set; }
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
        public bool IsDeleted { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public int? ModefiedBy { get; set; }

        public virtual AcademicCircle? AcademicCircle { get; set; }
        public virtual User? Student { get; set; }
        public virtual User? Teacher { get; set; }
        public virtual AcademicSubject? Subject { get; set; }
    }
}
