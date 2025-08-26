using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.UserDtos
{
    public class CreateUserDto
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? NationalId { get; set; }
        public DateTime? BirthDate { get; set; } 
        public int? UserTypeId { get; set; }
        public List<int>? CenterIds { get; set; }
        public List<int>? DepartmentIds { get; set; }
        public string? JobId { get; set; }
        public string? JobName { get; set; }
    }
}
