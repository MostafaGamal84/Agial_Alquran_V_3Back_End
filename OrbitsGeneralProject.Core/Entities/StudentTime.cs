using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class StudentTime:EntityBase
    {
        public int Id { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public int? StudentId { get; set; }
        public int? TimeId { get; set; }
        public bool? IsDeleted { get; set; }

        public virtual User? Student { get; set; }
        public virtual Time? Time { get; set; }
    }
}
