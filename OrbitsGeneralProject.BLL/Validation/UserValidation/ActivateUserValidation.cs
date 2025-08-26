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
//    public class ActivateUserValidation : AbstractValidator<ActivateUserDto>
//    {
//        public ActivateUserValidation() 
//        {
//            RuleFor(x=>x.Email).NotEmpty().WithMessage(UserValidationReponseConstants.EmailNotNullOrEmpty)
//                .NotNull().WithMessage(UserValidationReponseConstants.EmailNotNullOrEmpty);
//            RuleFor(x => x.Password).NotEmpty().WithMessage(UserValidationReponseConstants.PasswordNotEmptyOrNull)
//                .NotNull().WithMessage(UserValidationReponseConstants.PasswordNotEmptyOrNull);
//        }
//    }
//}
