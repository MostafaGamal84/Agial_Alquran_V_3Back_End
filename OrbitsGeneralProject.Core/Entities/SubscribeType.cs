using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class SubscribeType:EntityBase
    {
        public SubscribeType()
        {
            StudentSubscribes = new HashSet<StudentSubscribe>();
            Subscribes = new HashSet<Subscribe>();
        }

        public int Id { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public string? Name { get; set; }
        public decimal? ForignPricePerHour { get; set; }
        public decimal? ArabPricePerHour { get; set; }
        public decimal? EgyptPricePerHour { get; set; }
        public bool? IsDeleted { get; set; }
        public int? Type { get; set; }

        public virtual ICollection<StudentSubscribe> StudentSubscribes { get; set; }
        public virtual ICollection<Subscribe> Subscribes { get; set; }
    }
}
