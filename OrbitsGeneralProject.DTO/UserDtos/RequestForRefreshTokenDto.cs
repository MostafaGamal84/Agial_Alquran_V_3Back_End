using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.UserDtos
{
    public class RequestForRefreshTokenDto
    {
        public string OldToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
