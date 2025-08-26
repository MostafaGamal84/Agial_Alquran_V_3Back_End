using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class Subscribe:EntityBase
    {
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
    }
}
