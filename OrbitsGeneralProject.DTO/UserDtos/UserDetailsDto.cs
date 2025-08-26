using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.UserDtos
{
    public class UserDetailsDto
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public List<int> CenterIds { get; set; } = new List<int>();
        public List<int> DepartmentIds { get; set; } = new List<int>();
        public string? NationalId { get; set; }
        public string? JobId { get; set; }
        public string? JobName { get; set; }
        public bool? Inactive { get; set; }
        public bool IsDeleted { get; set; }
        public int? UserTypeId { get; set; }
        public string? UserType { get; set; }
        public bool CanUpdate { get; set; } = false;
    }
}
