using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.SubscribeDtos
{
    public class CreateSubscribeDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal? Leprice { get; set; }
        public decimal? Sarprice { get; set; }
        public decimal? Usdprice { get; set; }
        public int? Minutes { get; set; }
        public int? SubscribeTypeId { get; set; }
        public int? SubscribeFor { get; set; }

    }
}
