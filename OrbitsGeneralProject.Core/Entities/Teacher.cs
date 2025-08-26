using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class Teacher:EntityBase
    {
        public int Id { get; set; }
        public string? CircleId { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public int? CreatedBy { get; set; }

        public virtual User IdNavigation { get; set; } = null!;
    }
}
