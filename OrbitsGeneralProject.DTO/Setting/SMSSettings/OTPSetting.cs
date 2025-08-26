using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.Setting.SMSSettings
{
    public class OTPSetting
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string GetTokenURL { get; set; }
        public string SendOTPURL { get; set; }
        public string AppName { get; set; }
        public string GrantType { get; set; }
        public bool IsAmanaProviderCanSend { get; set; }
        public bool CanSendToFront { get; set; }
    }
}
