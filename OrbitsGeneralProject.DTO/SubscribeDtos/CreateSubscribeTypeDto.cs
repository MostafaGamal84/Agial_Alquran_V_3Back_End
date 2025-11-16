using Orbits.GeneralProject.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.SubscribeDtos
{
    public class CreateSubscribeTypeDto
    {

        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal? HourPrice { get; set; }
        public SubscribeTypeCategory? Group { get; set; }
    }
}
