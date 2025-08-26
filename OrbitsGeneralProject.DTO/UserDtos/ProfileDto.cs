using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.UserDtos
{
    public class ProfileDto
    {
        public string? UserName { get; set; }
        public string? UserType { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? JobName { get; set; }
        public string? NationalId { get; set; }
        public List<string>? Departments { get; set; }
        public List<string>? Centers { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? PhotoPath { get; set; }
    }
}
