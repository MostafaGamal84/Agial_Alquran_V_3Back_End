using System.Collections.Generic;

namespace Orbits.GeneralProject.DTO.UserDtos
{
    public class BulkDisableUsersResultDto
    {
        public int RequestedCount { get; set; }
        public int DisabledCount { get; set; }
        public List<int> DisabledUserIds { get; set; } = new();
        public List<int> SkippedUserIds { get; set; } = new();
    }
}
