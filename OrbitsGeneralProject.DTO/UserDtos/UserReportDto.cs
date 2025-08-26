using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.UserDtos
{
    public class UserReportDto
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? NationalId { get; set; }
        public string? UserType { get; set; }
        public bool? Inactive { get; set; }
        public string? Centers { get; set; }
        public string? Departments { get; set; }
    }
}
