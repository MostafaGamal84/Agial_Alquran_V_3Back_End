//using FluentValidation;
//using Orbits.GeneralProject.BLL.Constants;
//using Orbits.GeneralProject.DTO.UserDtos;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Orbits.GeneralProject.BLL.Validation.UserValidation
//{
//    public class ChangePasswordValidation : AbstractValidator<ChangePasswordDto>
//    {
//        public ChangePasswordValidation() 
//        {
//            RuleFor(x => x.NewPassword)
//                .NotEmpty().WithMessage(UserValidationReponseConstants.PasswordNotEmptyOrNull)
//                .NotNull().WithMessage(UserValidationReponseConstants.PasswordNotEmptyOrNull)
//                .Matches("^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{12,}$/").WithMessage(LoginValidationReponseConstants.PasswordMustBeComplex);
//        }
//    }
//}
