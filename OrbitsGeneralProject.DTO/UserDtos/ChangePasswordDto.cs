using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.UserDtos
{
    public class ChangePasswordDto
    {
        public string Email { get; set;}
        public string NewPassword { get; set; }
    }
}
