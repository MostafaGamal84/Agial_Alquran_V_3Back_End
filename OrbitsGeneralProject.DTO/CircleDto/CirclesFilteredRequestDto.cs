using Orbits.GeneralProject.DTO.Paging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.CircleDto
{
    public class CirclesFilteredRequestDto : FilteredResultRequestDto
    {
        public int ManagerId { get; set; }
        public List<int> SubCategoryIds { get; set; } = new List<int>();
    }
}
