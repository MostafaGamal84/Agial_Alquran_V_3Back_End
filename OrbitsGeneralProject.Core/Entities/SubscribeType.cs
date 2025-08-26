using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class SubscribeType:EntityBase
    {
        public int Id { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public string? Name { get; set; }
        public decimal? ForignPricePerHour { get; set; }
        public decimal? ArabPricePerHour { get; set; }
    }
}
