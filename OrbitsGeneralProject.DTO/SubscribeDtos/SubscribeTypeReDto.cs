using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.SubscribeDtos
{
    public class SubscribeTypeReDto
    {

        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal? ForignPricePerHour { get; set; }
        public decimal? ArabPricePerHour { get; set; }
        public decimal? EgyptPricePerHour { get; set; }
        public SubscribeTypeCategory? Type { get; set; }

    }
}
