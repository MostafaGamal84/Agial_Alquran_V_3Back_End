using System.Collections.Generic;

namespace Orbits.GeneralProject.DTO.UserDtos
{
    public class BulkDisableUsersDto
    {
        public List<int> UserIds { get; set; } = new();
    }
}
