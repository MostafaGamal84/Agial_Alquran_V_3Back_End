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
    public class VerifyLoginCodeValidation : AbstractValidator<VerifyLoginCodeDto>
    {
        public VerifyLoginCodeValidation() 
        {
            RuleFor(x => x.Email).NotEmpty().NotNull().WithMessage(LoginValidationReponseConstants.EmailNotNullOrEmpty);
            RuleFor(x => x.Code).NotEmpty().NotNull().WithMessage(LoginValidationReponseConstants.CodeNullOrEmpty)
                .Length(4).WithMessage(LoginValidationReponseConstants.CodeLength);
        }
    }
}
