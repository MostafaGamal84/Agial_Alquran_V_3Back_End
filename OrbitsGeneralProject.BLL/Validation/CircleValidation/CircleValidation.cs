using FluentValidation;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.DTO.CircleDto;
using System.Text.RegularExpressions;

namespace Orbits.GeneralProject.BLL.Validation.CircleValidation;
public class CircleValidation : AbstractValidator<CreateCircleDto>
{
    public CircleValidation()
    {
        RuleFor(l => l.Name).NotNull().WithMessage(CircleValidationResponseConstants.NAME_Must_Not_Null)
            .NotEmpty().WithMessage(CircleValidationResponseConstants.NAME_Must_Not_Null);
        RuleFor(l => l.Name).MaximumLength(250).WithMessage(CircleValidationResponseConstants.Name_Max_Length);
        RuleFor(l => l.Name).Matches(new Regex(@"^(?!.*\d_)(?!.*_\d)[a-zA-Z0-9ء-ي ]+$"))
    .WithMessage(CircleValidationResponseConstants.ValidName);

    }
   
}
