using FluentValidation;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.DTO.UserDtos;

namespace Orbits.GeneralProject.BLL.Validation.UserValidation
{
    public class ResetPasswordValidation : AbstractValidator<ResetPasswordDto>
    {
        public ResetPasswordValidation()
        {
            RuleFor(x => x.Email)
               .NotEmpty().WithMessage(UserValidationReponseConstants.EmailNotNullOrEmpty)
               .NotNull().WithMessage(UserValidationReponseConstants.EmailNotNullOrEmpty)
               .EmailAddress().WithMessage(UserValidationReponseConstants.ValidEmail);

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage(UserValidationReponseConstants.PasswordNotEmptyOrNull)
                .NotNull().WithMessage(UserValidationReponseConstants.PasswordNotEmptyOrNull);

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage(UserValidationReponseConstants.CodeNotEmptyOrNull)
                .NotNull().WithMessage(UserValidationReponseConstants.CodeNotEmptyOrNull)
                .Length(4).WithMessage(UserValidationReponseConstants.CodeLength);
        }
    }
}
