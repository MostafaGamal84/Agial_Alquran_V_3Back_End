using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO.HelperDtos.MailDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.BLL.Helpers.MailHelper
{
    public interface IMailServiceBLL
    {
        Task<IResponse<MailMessage>> SendEmail(EmailMessage msg);
        Task<IResponse<bool>> SendEmails(List<EmailMessage> mails);
    }
}
