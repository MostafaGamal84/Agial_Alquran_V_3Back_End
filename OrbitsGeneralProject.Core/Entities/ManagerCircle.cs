using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class ManagerCircle:EntityBase
    {
        public int Id { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public int? ManagerId { get; set; }
        public int? CircleId { get; set; }

        public virtual Circle? Circle { get; set; }
        public virtual User? Manager { get; set; }
    }
}
