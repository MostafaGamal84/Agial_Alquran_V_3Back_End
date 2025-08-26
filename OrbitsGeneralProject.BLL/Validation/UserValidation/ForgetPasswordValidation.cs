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
//    public class ForgetPasswordValidation : AbstractValidator<ForgetPasswordDto>
//    {
//        public ForgetPasswordValidation() 
//        {
//            RuleFor(x=>x.Email)
//                .NotEmpty().WithMessage(UserValidationReponseConstants.EmailNotNullOrEmpty)
//                .NotNull().WithMessage(UserValidationReponseConstants.EmailNotNullOrEmpty)
//                .EmailAddress().WithMessage(UserValidationReponseConstants.ValidEmail);
//        }
//    }
//}
