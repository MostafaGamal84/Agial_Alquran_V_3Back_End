using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.LoginDtos
{
    public class LoginResultDto
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public string Username { get; set; }
        public int? Role { get; set; }
        public int? BranchId { get; set; }
        public int? UserId { get; set; }
        public string? FullName { get; set; }
        //public int? UserTypeId { get; set; }

    }
}
