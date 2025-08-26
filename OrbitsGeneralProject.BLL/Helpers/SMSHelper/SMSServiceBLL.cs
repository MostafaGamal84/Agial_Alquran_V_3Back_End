using Microsoft.Extensions.Options;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO.HelperDtos.SMSDtos;
using Orbits.GeneralProject.DTO.Setting.SMSSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Microsoft.AspNetCore.Server.IIS;
using System.Net.Http;

namespace Orbits.GeneralProject.BLL.Helpers.SMSHelper
{
    public class SMSServiceBLL : ISMSServiceBLL
    {
        private readonly TwilioSetting _twilio;
        private readonly OTPSetting _OTPSetting;

        public SMSServiceBLL(IOptions<TwilioSetting> twilio, IOptions<OTPSetting> OTPSetting)
        {
            _twilio = twilio.Value;
            _OTPSetting = OTPSetting.Value;
        }
        public async Task<IResponse<MessageResource>> SendSMSTwillio(string mobileNumber, string body)
        {
            var output = new Response<MessageResource>();
            try
            {
                TwilioClient.Init(_twilio.AccountSID, _twilio.AuthToken);
                var result = await MessageResource.CreateAsync(
                        body: body,
                        from: new Twilio.Types.PhoneNumber(_twilio.TwilioPhoneNumber),
                        to: mobileNumber
                    );

                return output.CreateResponse(result);
            }
            catch (Exception ex)
            {
                return output.CreateResponse(ex);
            }
        }
        //    public async Task<IResponse<bool>> SendListSMS(List<SMSMessageDto> dtos)
        //    {
        //        var output = new Response<bool>();
        //        try
        //        {
        //            foreach(SMSMessageDto dto in dtos)
        //            {
        //                var httpclient = new HttpClient();
        //                var getTokenContent = new FormUrlEncodedContent(new[]
        //                {
        //                    new KeyValuePair<string, string>("grant_type", _OTPSetting.GrantType)
        //                });
        //                var authenticationString = $"{_OTPSetting.UserName}:{_OTPSetting.Password}";
        //                var base64String = Convert.ToBase64String(
        //                   System.Text.Encoding.ASCII.GetBytes(authenticationString));
        //                httpclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64String);
        //                var requestToGetToken = await httpclient.PostAsync(_OTPSetting.GetTokenURL, getTokenContent)
        //                    .ConfigureAwait(false);
        //                var bindTokenToDto = await requestToGetToken.Content.ReadAsAsync<ReadOTPHTTPClientResult>();
        //                httpclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", $"{bindTokenToDto.access_token}");
        //                var contentOfSecondRequestToSendOTP = new FormUrlEncodedContent(
        //                new[]
        //                        {
        //                    new KeyValuePair<string, string>("mobileno", dto.PhoneNumber),
        //                    new KeyValuePair<string, string>("msg", dto.Body),
        //                    new KeyValuePair<string, string>("appname", _amanaOTP.AppName),
        //                        });
        //                var requestToSendOTP = await httpclient.PostAsync(_amanaOTP.SendOTPURL, contentOfSecondRequestToSendOTP)
        //                    .ConfigureAwait(false);
        //            }

        //            return output.CreateResponse(true);
        //        }
        //        catch (Exception ex)
        //        {
        //            return output.CreateResponse(ex);
        //        }
        //    }
        //    public async Task<IResponse<bool>> SendSMS(SMSMessageDto dto)
        //    {
        //        var output = new Response<bool>();
        //        try
        //        {
        //            var httpclient = new HttpClient();
        //            var getTokenContent = new FormUrlEncodedContent(new[]
        //            {
        //                    new KeyValuePair<string, string>("grant_type", _amanaOTP.GrantType)
        //                });
        //            var authenticationString = $"{_amanaOTP.UserName}:{_amanaOTP.Password}";
        //            var base64String = Convert.ToBase64String(
        //               System.Text.Encoding.ASCII.GetBytes(authenticationString));
        //            httpclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64String);
        //            var requestToGetToken = await httpclient.PostAsync(_amanaOTP.GetTokenURL, getTokenContent)
        //                .ConfigureAwait(false);
        //            var bindTokenToDto = await requestToGetToken.Content.ReadAsAsync<ReadOTPHTTPClientResult>();
        //            httpclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", $"{bindTokenToDto.access_token}");
        //            var contentOfSecondRequestToSendOTP = new FormUrlEncodedContent(
        //            new[]
        //                    {
        //                    new KeyValuePair<string, string>("mobileno", dto.PhoneNumber),
        //                    new KeyValuePair<string, string>("msg", dto.Body),
        //                    new KeyValuePair<string, string>("appname", _amanaOTP.AppName),
        //                    });
        //            var requestToSendOTP = await httpclient.PostAsync(_amanaOTP.SendOTPURL, contentOfSecondRequestToSendOTP)
        //                .ConfigureAwait(false);
        //            return output.CreateResponse(true);
        //        }
        //        catch (Exception ex)
        //        {
        //            return output.CreateResponse(ex);
        //        }
        //    }
    }
}
