using FluentValidation;
using Orbits.GeneralProject.DTO.AcademicCircleDtos;

namespace Orbits.GeneralProject.BLL.Validation.AcademicCircleValidation
{
    public class AcademicCircleValidation : AbstractValidator<CreateAcademicCircleDto>
    {
        public AcademicCircleValidation()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("اسم حلقة المواد مطلوب.")
                .MaximumLength(200).WithMessage("اسم حلقة المواد يجب ألا يزيد عن 200 حرف.");

            RuleFor(x => x.TeacherId)
                .NotNull().WithMessage("اختيار المعلم مطلوب.")
                .GreaterThan(0).WithMessage("اختيار المعلم مطلوب.");

            RuleFor(x => x.ManagerIds)
                .Must(x => x != null && x.Any())
                .WithMessage("يجب اختيار مشرف واحد على الأقل.");

            RuleFor(x => x.StudentIds)
                .Must(x => x != null && x.Any())
                .WithMessage("يجب اختيار طالب واحد على الأقل.");
        }
    }
}
