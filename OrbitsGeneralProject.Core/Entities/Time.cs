using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class Time:EntityBase
    {
        public Time()
        {
            StudentTimes = new HashSet<StudentTime>();
        }

        public int Id { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public string? NameOfDay { get; set; }
        public bool? IsDeleted { get; set; }

        public virtual ICollection<StudentTime> StudentTimes { get; set; }
    }
}
