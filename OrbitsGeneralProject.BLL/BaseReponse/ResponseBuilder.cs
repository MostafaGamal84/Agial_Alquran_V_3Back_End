using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.Core.Extensions;
using EntityFramework.Exceptions.Common;
using Orbits.GeneralProject.Repositroy.Enums;

using FluentValidation.Results;

using System;
using System.Collections.Generic;
using Orbits.GeneralProject.Repositroy.Extentions;

namespace Orbits.GeneralProject.BLL.BaseReponse
{
    public class ResponseBuilder<T>
    {
        private bool isSuccess;
        private T data;
        private List<TErrorField> errors;
        private Exception exception;
        private IResponse<T> response;
        internal ResponseBuilder(IResponse<T> response)
        {
            //this.data = Activator.CreateInstance<T>();//TODO:recheck this
            this.response = response;
            this.errors = new List<TErrorField>();
            this.exception = null;
        }

        private void InitializeErrorsIfNot()
        {
            if (this.errors == null)
                this.errors = new List<TErrorField>();
        }
        internal ResponseBuilder<T> AppendError(TErrorField error)
        {
            return AppendErrors(new List<TErrorField> { error });
        }

        internal ResponseBuilder<T> AppendError(MessageCodes code, string message)
        {
            return AppendErrors(new List<TErrorField>
            {
                new TErrorField
                    {
                        FieldName = "",
                        Code = code.StringValue(),
                        Message = !string.IsNullOrEmpty(message)
                                ? string.Format(code.GetDescription(),message):code.GetDescription()
                        //Message = !string.IsNullOrWhiteSpace(message)
                        //        ? _messageLocalizer [code.StringValue(),  _labelLocalizer[message] != null && !string.IsNullOrWhiteSpace( _labelLocalizer[message].Value)? _labelLocalizer[message]: message]
                        //        : _messageLocalizer [code.StringValue()]

                    }
               }
            );


        }
        internal ResponseBuilder<T> AppendError(MessageCodes code, string fieldName, string message)
        {
            return AppendErrors(new List<TErrorField> { new TErrorField { FieldName = fieldName, Code = code.StringValue() ,
                  Message =message
            } });
        }
        internal ResponseBuilder<T> AppendError(ValidationFailure error)
        {
            return AppendErrors(new List<ValidationFailure> { error });
        }
        internal ResponseBuilder<T> AppendErrors(List<TErrorField> errors)
        {
            InitializeErrorsIfNot();
            this.errors.AddRange(errors);
            return this;
        }
        internal ResponseBuilder<T> AppendErrors(List<ValidationFailure> errors)
        {
            InitializeErrorsIfNot();
            foreach (var item in errors)
            {
                this.errors.Add(new TErrorField

                {
                    FieldName = item.PropertyName,
                    Code = item.ErrorCode,
                    Message = item.ErrorMessage,
                    FieldLang = item.AttemptedValue?.ToString()
                });

            }
            return this;
        }
        internal ResponseBuilder<T> WithError(TErrorField error)
        {
            return WithErrors(new List<TErrorField> { error });
        }
        internal ResponseBuilder<T> WithError(ValidationFailure error)
        {
            return WithErrors(new List<ValidationFailure> { error });
        }
        internal ResponseBuilder<T> WithErrors(List<TErrorField> errors)
        {
            InitializeErrorsIfNot();
            this.errors.AddRange(errors);
            return this;
        }
        internal ResponseBuilder<T> WithErrors(List<ValidationFailure> errors)
        {
            InitializeErrorsIfNot();
            foreach (var item in errors)
            {
                item.PropertyName = item.PropertyName == "File.File" ? "File" : item.PropertyName;
                this.errors.Add(new TErrorField
                {
                    FieldName = item.PropertyName,
                    //FieldName = !string.IsNullOrWhiteSpace(localizedFieldName)
                    //          ? $"[" + localizedFieldName + "]"
                    //          : $"[" + item.PropertyName + "]",
                    Code = item.ErrorCode,
                    Message = string.Format(item.ErrorMessage, $"[" + item.PropertyName + "]"),
                    //for (Default,Ar) in Required Fields with  jsonmodel values
                    FieldLang = item.ErrorCode == MessageCodes.Required.StringValue() ? item.AttemptedValue?.ToString() : null
                });
                ;
                ;

            }
            return this;
        }
        internal ResponseBuilder<T> WithData(T data)
        {
            this.data = data;
            return this;
        }
        internal ResponseBuilder<T> WithException( Exception exception )
        {
            this.exception = exception;

            if (exception is ReferenceConstraintException)
            {
                this.AppendError(new TErrorField()
                { Message = exception.InnerException.Message.ToString().FormatSqlErrorMessage(SqlExceptionEnum.ReferenceNotFound) });
            }
            else
            {
                this.errors.Add(new TErrorField { Message = "exMessage:" + exception.Message + "ex.InnerException:" + exception.InnerException });

            }
            return this;
        }

        internal bool IsSuccess { get => ((errors == null || errors.Count == 0) && exception == null) ? true : false; }
        internal IResponse<T> Build()
        {
            isSuccess = ((errors == null || errors.Count == 0) && exception == null) ? true : false;
            response.IsSuccess = isSuccess;
            response.Errors = errors;
            response.Data = data;
            return response;

        }

    }

}

