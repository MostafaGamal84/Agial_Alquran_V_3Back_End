using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class AcademicCircle : EntityBase
    {
        public AcademicCircle()
        {
            AcademicCircleStudents = new HashSet<AcademicCircleStudent>();
            AcademicManagerCircles = new HashSet<AcademicManagerCircle>();
            AcademicReports = new HashSet<AcademicReport>();
        }

        public int Id { get; set; }
        public string? Name { get; set; }
        public int? TeacherId { get; set; }
        public int? BranchId { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public int? ModefiedBy { get; set; }

        public virtual User? Teacher { get; set; }
        public virtual ICollection<AcademicCircleStudent> AcademicCircleStudents { get; set; }
        public virtual ICollection<AcademicManagerCircle> AcademicManagerCircles { get; set; }
        public virtual ICollection<AcademicReport> AcademicReports { get; set; }
    }
}
