using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.HelperDtos.MailDtos
{
    public class EmailMessage
    {
        public string Body { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
    }
}
