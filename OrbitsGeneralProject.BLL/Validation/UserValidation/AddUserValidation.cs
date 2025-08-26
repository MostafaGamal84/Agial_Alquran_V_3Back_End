using FluentValidation;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.DTO.UserDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.BLL.Validation.UserValidation
{
    public class AddUserValidation :AbstractValidator<CreateUserDto>
    {
        public AddUserValidation() 
        {
            RuleFor(l => l.UserTypeId).NotNull().WithMessage(UserValidationReponseConstants.UserTypeRequired).NotEmpty().WithMessage(UserValidationReponseConstants.UserTypeRequired);
            RuleFor(l => l.Email)
            .Must((u, e) =>
            {
                if (e.Contains(" "))
                    return false;
                return true;
            }).WithMessage(UserValidationReponseConstants.WhiteSpace);
            RuleFor(l => l.Email).NotNull().WithMessage(UserValidationReponseConstants.EmailNotNullOrEmpty).NotEmpty().WithMessage(UserValidationReponseConstants.EmailNotNullOrEmpty).MinimumLength(6).WithMessage(UserValidationReponseConstants.EmailNotNullOrEmpty)
                .EmailAddress(FluentValidation.Validators.EmailValidationMode.Net4xRegex).WithMessage(UserValidationReponseConstants.ValidEmail);
            RuleFor(l => l.Mobile).NotNull().WithMessage(UserValidationReponseConstants.PhoneNumberLength).NotEmpty().WithMessage(UserValidationReponseConstants.PhoneNumberLength).Length(10).WithMessage(UserValidationReponseConstants.PhoneNumberLength)
                .Matches(new Regex(@"^([0-9]{10})")).WithMessage(UserValidationReponseConstants.ValidPhoneNumber);
        }

    }
}
