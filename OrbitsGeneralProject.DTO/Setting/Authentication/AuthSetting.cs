using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.Setting.Authentication
{
    public class AuthSetting
    {
        public string Key { get; set; }
        public string Issuer { get; set; }
        public string ExpiryInDays { get; set; }
    }
}
