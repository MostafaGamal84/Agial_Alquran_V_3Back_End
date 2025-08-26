using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.Setting.MailSetting
{
    public class MailSetting
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Password { get; set; }
        public string UserName { get; set; }
    }
}
