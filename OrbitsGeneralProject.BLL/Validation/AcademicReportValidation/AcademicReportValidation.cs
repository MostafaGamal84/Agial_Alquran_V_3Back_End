using FluentValidation;
using Orbits.GeneralProject.DTO.AcademicReportDtos;

namespace Orbits.GeneralProject.BLL.Validation.AcademicReportValidation
{
    public class AcademicReportValidation : AbstractValidator<AcademicReportAddDto>
    {
        public AcademicReportValidation()
        {
            RuleFor(x => x.AcademicCircleId)
                .NotNull().WithMessage("اختيار الحلقة مطلوب.")
                .GreaterThan(0).WithMessage("اختيار الحلقة مطلوب.");

            RuleFor(x => x.StudentId)
                .NotNull().WithMessage("اختيار الطالب مطلوب.")
                .GreaterThan(0).WithMessage("اختيار الطالب مطلوب.");

            RuleFor(x => x.TeacherId)
                .NotNull().WithMessage("اختيار المعلم مطلوب.")
                .GreaterThan(0).WithMessage("اختيار المعلم مطلوب.");

            RuleFor(x => x.SubjectId)
                .NotNull().WithMessage("اختيار المادة مطلوب.")
                .GreaterThan(0).WithMessage("اختيار المادة مطلوب.");

            RuleFor(x => x.StageId)
                .InclusiveBetween(1, 3)
                .WithMessage("المرحلة الدراسية غير صحيحة.");

            RuleFor(x => x.LessonTitle)
                .NotEmpty().WithMessage("درس اليوم مطلوب.")
                .MaximumLength(500).WithMessage("درس اليوم يجب ألا يزيد عن 500 حرف.");

            RuleFor(x => x.StudentPerformanceId)
                .InclusiveBetween(1, 4)
                .WithMessage("تقييم أداء الطالب غير صحيح.");

            RuleFor(x => x.PreviousHomeworkStatusId)
                .InclusiveBetween(1, 3)
                .WithMessage("تقييم واجب الحصة الماضية غير صحيح.");

            RuleFor(x => x.HomeworkScore)
                .InclusiveBetween(1, 10)
                .WithMessage("درجة الواجب يجب أن تكون بين 1 و 10.");

            RuleFor(x => x.SessionDurationMinutes)
                .NotNull().WithMessage("مدة الحصة مطلوبة.")
                .InclusiveBetween(1, 300).WithMessage("مدة الحصة يجب أن تكون بين دقيقة و 300 دقيقة.");

            RuleFor(x => x.NextHomework)
                .MaximumLength(1000).WithMessage("واجب الحصة القادمة يجب ألا يزيد عن 1000 حرف.");

            RuleFor(x => x.TeacherNotes)
                .MaximumLength(2000).WithMessage("ملاحظات المعلم يجب ألا تزيد عن 2000 حرف.");
        }
    }
}
