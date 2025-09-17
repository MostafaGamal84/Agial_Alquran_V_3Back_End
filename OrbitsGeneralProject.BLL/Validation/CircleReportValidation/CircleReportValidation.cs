using FluentValidation;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.DTO.CircleDto;
using Orbits.GeneralProject.DTO.CircleReportDtos;
using System.Text.RegularExpressions;

namespace Orbits.GeneralProject.BLL.Validation.CircleReportValidation;
public class CircleReportValidation : AbstractValidator<CircleReportAddDto>
{
    public CircleReportValidation()
    {
        When(m => m.AttendStatueId == 1 , () => {
            RuleFor(l => l.NewId).NotNull().WithMessage(CircleReportValidationResponseConstants.Surah_Must_Not_Null)
             .NotEmpty().WithMessage(CircleReportValidationResponseConstants.Surah_Must_Not_Null);
        });

        When(m => m.AttendStatueId == 1 || m.AttendStatueId == 3, () => {
            RuleFor(l => l.Minutes).NotNull().WithMessage(CircleReportValidationResponseConstants.Surah_Must_Not_Null)
             .NotEmpty().WithMessage(CircleReportValidationResponseConstants.Minutes_Must_Not_Null);
        });
        
        RuleFor(l => l.TeacherId).NotNull().WithMessage(CircleReportValidationResponseConstants.Teacher_Must_Not_Null)
           .NotEmpty().WithMessage(CircleReportValidationResponseConstants.Teacher_Must_Not_Null);
        RuleFor(l => l.StudentId).NotNull().WithMessage(CircleReportValidationResponseConstants.Student_Must_Not_Null)
           .NotEmpty().WithMessage(CircleReportValidationResponseConstants.Student_Must_Not_Null);
        RuleFor(l => l.CircleId).NotNull().WithMessage(CircleReportValidationResponseConstants.Circle_Must_Not_Null)
           .NotEmpty().WithMessage(CircleReportValidationResponseConstants.Circle_Must_Not_Null);


    }
   
}
