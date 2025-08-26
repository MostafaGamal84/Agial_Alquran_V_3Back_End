using Orbits.GeneralProject.DTO.Paging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.UserDtos
{
    public class UserFilterPagedDto : FilteredResultRequestDto
    {
        public List<int>? UserTypeIds { get; set; } = new List<int>();
        public List<int>? CenterIds { get; set; } = new List<int>();
        public bool? Inactive { get; set; }
    }
}
