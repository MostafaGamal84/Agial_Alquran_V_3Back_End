using FluentValidation;
using Orbits.GeneralProject.DTO.AcademicSubjectDtos;

namespace Orbits.GeneralProject.BLL.Validation.AcademicSubjectValidation
{
    public class AcademicSubjectValidation : AbstractValidator<CreateAcademicSubjectDto>
    {
        public AcademicSubjectValidation()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("اسم المادة مطلوب.")
                .MaximumLength(200).WithMessage("اسم المادة يجب ألا يزيد عن 200 حرف.");

            RuleFor(x => x.DisplayOrder)
                .GreaterThan(0).When(x => x.DisplayOrder.HasValue)
                .WithMessage("ترتيب العرض يجب أن يكون أكبر من صفر.");
        }
    }
}
