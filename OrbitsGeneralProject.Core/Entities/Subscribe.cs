using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class Subscribe:EntityBase
    {
        public Subscribe()
        {
            StudentPayments = new HashSet<StudentPayment>();
            StudentSubscribes = new HashSet<StudentSubscribe>();
        }

        public int Id { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public string? Name { get; set; }
        public decimal? Leprice { get; set; }
        public decimal? Sarprice { get; set; }
        public decimal? Usdprice { get; set; }
        public int? Minutes { get; set; }
        public int? SubscribeTypeId { get; set; }
        public bool? IsDeleted { get; set; }

        public virtual SubscribeType? SubscribeType { get; set; }
        public virtual ICollection<StudentPayment> StudentPayments { get; set; }
        public virtual ICollection<StudentSubscribe> StudentSubscribes { get; set; }
    }
}
