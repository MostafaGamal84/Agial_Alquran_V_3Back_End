using FluentValidation;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.DTO.UserDtos;

namespace Orbits.GeneralProject.BLL.Validation.UserValidation
{
    public class ChangePasswordValidation : AbstractValidator<ChangePasswordDto>
    {
        public ChangePasswordValidation()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage(UserValidationReponseConstants.PasswordNotEmptyOrNull)
                .NotNull().WithMessage(UserValidationReponseConstants.PasswordNotEmptyOrNull);

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage(UserValidationReponseConstants.PasswordNotEmptyOrNull)
                .NotNull().WithMessage(UserValidationReponseConstants.PasswordNotEmptyOrNull)
                .Matches("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^A-Za-z0-9]).{8,}$")
                .WithMessage(LoginValidationReponseConstants.PasswordMustBeComplex);

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage(UserValidationReponseConstants.PasswordNotEmptyOrNull)
                .NotNull().WithMessage(UserValidationReponseConstants.PasswordNotEmptyOrNull)
                .Equal(x => x.NewPassword).WithMessage(LoginValidationReponseConstants.ConfirmPasswordMustMatch);
        }
    }
}
