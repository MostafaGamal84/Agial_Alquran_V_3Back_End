using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.LoginDtos
{
    public class VerifyLoginCodeDto
    {
        public string Email { get; set; }
        public string Code { get; set; }
        public bool PasswordIsCorrect { get; set; }=false;
    }
}
