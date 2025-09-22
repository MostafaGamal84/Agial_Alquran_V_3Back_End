using FluentValidation;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.DTO.CircleDto;
using System;
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

        RuleFor(l => l.DayId)
            .NotNull().WithMessage(CircleValidationResponseConstants.DayRequired)
            .Must(day => day.HasValue && Enum.IsDefined(typeof(DaysEnum), day.Value))
            .WithMessage(CircleValidationResponseConstants.DayRequired);

        RuleFor(l => l.StartTime)
            .NotNull().WithMessage(CircleValidationResponseConstants.StartTimeRequired)
            .Must(time => !time.HasValue || (time.Value >= TimeSpan.Zero && time.Value < TimeSpan.FromDays(1)))
            .WithMessage(CircleValidationResponseConstants.StartTimeInvalid);

    }

}
