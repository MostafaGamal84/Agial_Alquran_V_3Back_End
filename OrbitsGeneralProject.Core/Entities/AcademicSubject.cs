using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class AcademicSubject : EntityBase
    {
        public AcademicSubject()
        {
            AcademicReports = new HashSet<AcademicReport>();
        }

        public int Id { get; set; }
        public string? Name { get; set; }
        public int? DisplayOrder { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public int? ModefiedBy { get; set; }

        public virtual ICollection<AcademicReport> AcademicReports { get; set; }
    }
}
