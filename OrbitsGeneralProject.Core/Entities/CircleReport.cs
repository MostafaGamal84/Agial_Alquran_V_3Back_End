using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class CircleReport:EntityBase
    {
        public CircleReport()
        {
            StudentSubscribes = new HashSet<StudentSubscribe>();
            TeacherReportRecords = new HashSet<TeacherReportRecord>();
        }

        public int Id { get; set; }
        public double? Minutes { get; set; }
        public int? NewId { get; set; }
        public string? NewFrom { get; set; }
        public string? NewTo { get; set; }
        public string? NewRate { get; set; }
        public string? RecentPast { get; set; }
        public string? RecentPastRate { get; set; }
        public string? DistantPast { get; set; }
        public string? DistantPastRate { get; set; }
        public string? FarthestPast { get; set; }
        public string? FarthestPastRate { get; set; }
        public string? TheWordsQuranStranger { get; set; }
        public string? Intonation { get; set; }
        public string? Other { get; set; }
        public DateTime CreationTime { get; set; }
        public int? CircleId { get; set; }
        public int? StudentId { get; set; }
        public int? TeacherId { get; set; }
        public int? AttendStatueId { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsPermanentlyDeleted { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }

        public virtual Circle? Circle { get; set; }
        public virtual User? Student { get; set; }
        public virtual User? Teacher { get; set; }
        public virtual ICollection<StudentSubscribe> StudentSubscribes { get; set; }
        public virtual ICollection<TeacherReportRecord> TeacherReportRecords { get; set; }
    }
}
