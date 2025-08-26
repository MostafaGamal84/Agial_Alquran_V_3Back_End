using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO.HelperDtos.SMSDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Twilio.Rest.Api.V2010.Account;

namespace Orbits.GeneralProject.BLL.Helpers.SMSHelper
{
    public interface ISMSServiceBLL
    {
        //Task<IResponse<bool>> SendListSMS(List<SMSMessageDto> dtos);
        //Task<IResponse<bool>> SendSMS(SMSMessageDto dto);
        Task<IResponse<MessageResource>> SendSMSTwillio(string mobileNumber, string body);
    }
}
