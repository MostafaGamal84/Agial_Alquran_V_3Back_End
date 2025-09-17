using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class TeacherReportRecord:EntityBase
    {
        public int Id { get; set; }
        public int? TeacherId { get; set; }
        public int? CircleReportId { get; set; }
        public int? CircleSallary { get; set; }
        public int? Minutes { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public bool? IsDeleted { get; set; }

        public virtual CircleReport? CircleReport { get; set; }
        public virtual User? Teacher { get; set; }
    }
}
