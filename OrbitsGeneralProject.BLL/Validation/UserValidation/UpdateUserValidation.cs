using FluentValidation;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.DTO.UserDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.BLL.Validation.UserValidation
{
    public class UpdateUserValidation : AbstractValidator<UpdateUserDto>
    {
        public UpdateUserValidation()
        {
            RuleFor(l => l.FullName).NotNull().NotEmpty().WithMessage(UserValidationReponseConstants.UserNameNotNullOrEmpty);
            RuleFor(l => l.FullName).MaximumLength(250).WithMessage(UserValidationReponseConstants.UserNameMaxLength);
            RuleFor(l => l.Mobile)
     .NotNull().WithMessage(UserValidationReponseConstants.PhoneNumberLength)
     .NotEmpty().WithMessage(UserValidationReponseConstants.PhoneNumberLength)
     .Matches(new Regex(@"^\+?[0-9]{10,15}$"))
         .WithMessage(UserValidationReponseConstants.ValidPhoneNumber);
        }
    }
}
