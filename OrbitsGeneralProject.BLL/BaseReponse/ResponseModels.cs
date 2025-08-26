namespace Orbits.GeneralProject.BLL.BaseReponse
{
    public class TErrorField
    {
        public string FieldName { get; set; } = string.Empty;
        public string Code { get; set; }
        public string Message { get; set; }
        public string FieldLang { get; set; }// = nameof(JsonLanguageModel.Default);
    }

    public class CodeDescription
    {
        public string Code { get; set; }
        public List<ErrorField> Fields { get; set; }
    }

    public class ErrorField
    {
        public string PropertyName { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class StatusDescription
    {
        public string Code { get; set; }
        public string Description { get; set; }
    }

    public enum Severity
    {
        Error = 0,
        Warning = 1,
        Info = 2
    }
}