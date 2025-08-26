using Microsoft.Extensions.Options;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO.HelperDtos.MailDtos;
using Orbits.GeneralProject.DTO.Setting.MailSetting;
using System.Net;
using System.Net.Mail;

namespace Orbits.GeneralProject.BLL.Helpers.MailHelper
{
    public class MailServiceBLL : IMailServiceBLL
    {
        private readonly MailSetting _mailSetting;

        public MailServiceBLL(IOptions<MailSetting> mailSetting)
        {
            _mailSetting = mailSetting.Value;
        }

        public async Task<IResponse<MailMessage>> SendEmail(EmailMessage msg)
        {
            var output = new Response<MailMessage>();
            try
            {
                using (SmtpClient smtp = new SmtpClient())
                {
                    smtp.EnableSsl = true;
                    smtp.Host = _mailSetting.Host;
                    smtp.UseDefaultCredentials = false;
                    NetworkCredential NetworkCred = new NetworkCredential(_mailSetting.UserName, _mailSetting.Password);
                    smtp.Credentials = NetworkCred;
                    smtp.Port = _mailSetting.Port;
                    using (System.Net.Mail.MailMessage mm = new MailMessage(_mailSetting.UserName, msg.To))
                    {
                        mm.Subject = msg.Subject;
                        mm.Body = msg.Body;
                        mm.IsBodyHtml = true;
                        await smtp.SendMailAsync(mm);
                        return output.CreateResponse(mm);
                    }
                }
            }
            catch (System.Exception ex)
            {
                return output.CreateResponse(ex);
            }
        }
        public async Task<IResponse<bool>> SendEmails(List<EmailMessage> mails)
        {
            var output = new Response<bool>();
            try
            {
                using (SmtpClient smtp = new SmtpClient())
                {
                    smtp.EnableSsl = true;
                    smtp.Host = _mailSetting.Host;
                    smtp.UseDefaultCredentials = false;
                    smtp.Port = _mailSetting.Port;
                    NetworkCredential NetworkCred = new NetworkCredential(_mailSetting.UserName, _mailSetting.Password);
                    smtp.Credentials = NetworkCred;
                    foreach (var email in mails)
                    {
                        using (System.Net.Mail.MailMessage mm = new MailMessage(_mailSetting.UserName, email.To))
                        {
                            mm.Subject = email.Subject;
                            mm.Body = email.Body;
                            mm.IsBodyHtml = true;
                            await smtp.SendMailAsync(mm);
                        }
                    }
                }
                return output.CreateResponse(true);
            }
            catch (System.Exception ex)
            {
                return output.CreateResponse(ex);
            }
        }
    }
}
