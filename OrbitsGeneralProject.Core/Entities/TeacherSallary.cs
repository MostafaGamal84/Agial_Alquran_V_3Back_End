using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class TeacherSallary:EntityBase
    {
        public int Id { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public int? TeacherId { get; set; }
        public DateTime? Month { get; set; }
        public double? Sallary { get; set; }
        public bool? IsPayed { get; set; }
        public DateTime? PayedAt { get; set; }
        public string? ReceiptPath { get; set; }
        public bool? IsDeleted { get; set; }

        public virtual User? Teacher { get; set; }
    }
}
