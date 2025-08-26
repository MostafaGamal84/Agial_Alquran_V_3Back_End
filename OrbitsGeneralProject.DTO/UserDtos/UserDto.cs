using Orbits.GeneralProject.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.UserDtos
{
    public class UserDto
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? NationalId { get; set; }
        public string? UserType { get; set; }
        public bool? Inactive { get; set; }
        public bool IsDeleted { get; set; }
        public int? UserTypeId { get; set; }
        public List<LookupDto> Centers { get; set; } = new List<LookupDto>();
        public List<LookupDto> Departments { get; set; } = new List<LookupDto>();
    }
}
