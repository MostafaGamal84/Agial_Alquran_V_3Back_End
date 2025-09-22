using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class CircleTime:EntityBase
    {
        public int Id { get; set; }
        public int? CircleId { get; set; }
        public TimeSpan? Time { get; set; }
        public int? DayId { get; set; }

        public virtual Circle? Circle { get; set; }
        public virtual Day? Day { get; set; }
    }
}
