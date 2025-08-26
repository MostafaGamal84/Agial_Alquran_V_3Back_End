using FluentValidation;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.DTO.LoginDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.BLL.Validation.LoginValidation
{
    public class LoginValidation : AbstractValidator<LoginDto>
    {
        public LoginValidation() 
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage(LoginValidationReponseConstants.EmailNotNullOrEmpty).NotNull().WithMessage(LoginValidationReponseConstants.EmailNotNullOrEmpty);
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(LoginValidationReponseConstants.PasswordNotNullOrEmpty)
                .NotNull().WithMessage(LoginValidationReponseConstants.PasswordNotNullOrEmpty);
        }
    }
}
