using Orbits.GeneralProject.BLL.Constants;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;

namespace Orbits.GeneralProject.BLL.BaseReponse
{
    public class AuthorizationResponse
    {
        public bool IsSuccess { get; set; }

        public List<TErrorField> Errors { get; set; }
        public bool Data { get; set; }

    }

    public class Response<T> : IResponse<T>
    {
        public bool IsSuccess
        {
            get
            {
                return responseBuilder.IsSuccess;
            }
            set { }

        }

        public List<TErrorField> Errors { get; set; }
        public T Data { get; set; }

        private ResponseBuilder<T> responseBuilder;
        //private ICustomStringLocalizer<ErrorMessage> _messageLocalizer;
        //private ICustomStringLocalizer<Label> _labelLocalizer;


        public Response()
        {

            responseBuilder = new ResponseBuilder<T>(this);


            //_messageLocalizer = LocalizerProvider<ErrorMessage>.GetLocalizer();// new CustomStringLocalizer();// (ICustomStringLocalizer) // lServiceProviderFactory.ServiceProvider.GetService(typeof(ICustomStringLocalizer));
            //_labelLocalizer =  LocalizerProvider<Label>.GetLocalizer();
            //responseBuilder = new ResponseBuilder<T>(this, _messageLocalizer, _labelLocalizer);

        }

        public IResponse<T> CreateResponse()
        {
            return responseBuilder.CreateResponse();
        }
        public IResponse<T> CreateResponse(T data)
        {
            return responseBuilder.WithData(data).Build();

        }
        public IResponse<T> CreateResponse(List<ValidationFailure> inputValidations = null)
        {
            return responseBuilder.WithErrors(inputValidations).Build();
        }

        //for one business error
        public IResponse<T> CreateResponse(MessageCodes messageCode, string message = "")
        {
            return responseBuilder.AppendError(messageCode, message).Build();
        }
        public IResponse<T> CreateResponse(Exception ex)
        {
            return responseBuilder.WithException(ex).Build();
        }
        public IResponse<T> AppendError(TErrorField error)
        {
            return responseBuilder.AppendError(error).CreateResponse();

        }

        public IResponse<T> AppendError(MessageCodes code, string? message = null)
        {
            return responseBuilder.AppendError(code, message).CreateResponse();
        }
        public IResponse<T> AppendError(MessageCodes code, string fieldName, string message)
        {
            return responseBuilder.AppendError(code, fieldName, message).CreateResponse();

        }
        public IResponse<T> AppendError(ValidationFailure error)
        {
            return responseBuilder.AppendError(error).CreateResponse();
        }
        public IResponse<T> AppendErrors(List<TErrorField> errors)
        {
            return responseBuilder.AppendErrors(errors).CreateResponse();

        }
        public IResponse<T> AppendErrors(List<ValidationFailure> errors)
        {
            return responseBuilder.AppendErrors(errors).CreateResponse();

        }
        //public ITResponse<T> CreateResponse<IInputDto, IValidator>( IInputDto dto, IValidator validator ) where IValidator : DtoValidationAbstractBase<IInputDto> where IInputDto : BaseDto

        //{
        //    var validationResult = validator.Validate(dto);
        //    if (!validationResult.IsValid)
        //        return responseBuilder.WithErrors(validationResult.Errors).Build();
        //    else
        //        return responseBuilder.Build();
        //}

    }

    #region old

    //private IStringLocalizer<ILocalize> _localizedService;

    //public string GetLocalizedString( string input )
    //{
    //    Lang = string.IsNullOrWhiteSpace(Lang) ? "en" : Lang;
    //    CultureInfo.CurrentCulture = new CultureInfo(Lang); //can be not set for default culture
    //                                                        //var myText = _localizedService[input];
    //    //var myText = _localizer.GetLocalizedString(input);
    //    var myText = _localizer[input];

    //    return myText;
    //}
    //internal class Response<T> : IResponse<T>
    //{
    //    public List<CodeDescription> Errors { get; set; }
    //    public StatusDescription State { get; set; }
    //    public T Data { get; set; }

    //}
    //(IStringLocalizer<ILocalize>)serviceProvider.GetService(typeof(IStringLocalizer<ILocalize>));
    //     InitializeLocalizer();
    //public void InitializeLocalizer( ILocalizeService localizedService )
    //{
    //    var options = Options.Create(new LocalizationOptions { ResourcesPath = "Resources"});  // you should not need any params here if using a StringLocalizer<T>
    //    var factory = new ResourceManagerStringLocalizerFactory(options, NullLoggerFactory.Instance);            

    //}
    #endregion
    public static class ServiceProviderFactory
    {
        public static IServiceProvider ServiceProvider { get; set; }
        public static IServiceCollection ServiceCollection { get; set; }


    }
}
