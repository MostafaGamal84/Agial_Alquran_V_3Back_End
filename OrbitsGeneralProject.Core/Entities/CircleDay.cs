using System;
using Orbits.GeneralProject.Core.Infrastructure;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class CircleDay : EntityBase
    {
        public int Id { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public int? DayId { get; set; }
        public int? CircleId { get; set; }

        public virtual Circle? Circle { get; set; }
    }
}
