//using FluentValidation;
//using Orbits.GeneralProject.BLL.Constants;
//using Orbits.GeneralProject.DTO.UserDtos;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;

//namespace Orbits.GeneralProject.BLL.Validation.User
//{
//    public class UpdateProfileValidation : AbstractValidator<UpdateAccountDto>
//    {
//        public UpdateProfileValidation()
//        {
//            RuleFor(l => l.Mobile).NotNull().NotEmpty()
//                .Length(10).WithMessage(UserValidationReponseConstants.PhoneNumberLength)
//                .Matches(new Regex(@"^([0-9]{10})")).WithMessage(UserValidationReponseConstants.ValidPhoneNumber);
//            RuleFor(l => l.Username).NotNull().NotEmpty().WithMessage(UserValidationReponseConstants.UserNameNotNullOrEmpty)
//                .MaximumLength(250).WithMessage(UserValidationReponseConstants.UserNameMaxLength)
//                .Matches(new Regex(@"^(?!.*\d_)(?!.*_\d)[a-zA-Z0-9ء-ي ]+$"));
//            RuleFor(x => x.Password)
//                .MinimumLength(8).WithMessage(UserValidationReponseConstants.PasswordLength).When(x => x.Password is not null)
//                .Matches("^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{12,}$/").WithMessage(LoginValidationReponseConstants.PasswordMustBeComplex);
//        }
//    }
//}
