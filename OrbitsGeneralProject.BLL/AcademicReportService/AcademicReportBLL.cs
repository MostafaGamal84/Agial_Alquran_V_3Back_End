using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.Helpers;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.BLL.Validation.AcademicReportValidation;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO.AcademicReportDtos;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.Repositroy.Base;

namespace Orbits.GeneralProject.BLL.AcademicReportService
{
    public class AcademicReportBLL : BaseBLL, IAcademicReportBLL
    {
        private readonly IRepository<AcademicReport> _academicReportRepository;
        private readonly IRepository<AcademicCircle> _academicCircleRepository;
        private readonly IRepository<AcademicCircleStudent> _academicCircleStudentRepository;
        private readonly IRepository<AcademicManagerCircle> _academicManagerCircleRepository;
        private readonly IRepository<AcademicSubject> _academicSubjectRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AcademicReportBLL(
            IMapper mapper,
            IRepository<AcademicReport> academicReportRepository,
            IRepository<AcademicCircle> academicCircleRepository,
            IRepository<AcademicCircleStudent> academicCircleStudentRepository,
            IRepository<AcademicManagerCircle> academicManagerCircleRepository,
            IRepository<AcademicSubject> academicSubjectRepository,
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork) : base(mapper)
        {
            _academicReportRepository = academicReportRepository;
            _academicCircleRepository = academicCircleRepository;
            _academicCircleStudentRepository = academicCircleStudentRepository;
            _academicManagerCircleRepository = academicManagerCircleRepository;
            _academicSubjectRepository = academicSubjectRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IResponse<AcademicReportReDto>> GetByIdAsync(int id, int userId)
        {
            var output = new Response<AcademicReportReDto>();
            var report = await BuildScopedReportsQuery(userId)
                .Include(x => x.AcademicCircle)
                .Include(x => x.Student)
                .Include(x => x.Teacher)
                .Include(x => x.Subject)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (report == null)
            {
                return output.AppendError(MessageCodes.NotFound);
            }

            return output.CreateResponse(MapReport(report));
        }

        public IResponse<PagedResultDto<AcademicReportReDto>> GetPagedList(
            FilteredResultRequestDto pagedDto,
            int? circleId,
            int? studentId,
            int? teacherId,
            int? subjectId,
            int userId)
        {
            var output = new Response<PagedResultDto<AcademicReportReDto>>();
            var searchTerm = pagedDto.SearchTerm?.Trim().ToLower();

            var query = BuildScopedReportsQuery(userId)
                .Include(x => x.AcademicCircle)
                .Include(x => x.Student)
                .Include(x => x.Teacher)
                .Include(x => x.Subject)
                .AsQueryable();

            if (circleId.HasValue && circleId.Value > 0)
            {
                query = query.Where(x => x.AcademicCircleId == circleId.Value);
            }

            if (studentId.HasValue && studentId.Value > 0)
            {
                query = query.Where(x => x.StudentId == studentId.Value);
            }

            if (teacherId.HasValue && teacherId.Value > 0)
            {
                query = query.Where(x => x.TeacherId == teacherId.Value);
            }

            if (subjectId.HasValue && subjectId.Value > 0)
            {
                query = query.Where(x => x.SubjectId == subjectId.Value);
            }

            if (pagedDto.FromDate.HasValue)
            {
                var fromDate = BusinessDateTime.GetCairoDayRangeUtc(pagedDto.FromDate.Value).StartUtc;
                query = query.Where(x => x.ReportDate >= fromDate);
            }

            if (pagedDto.ToDate.HasValue)
            {
                var toDate = BusinessDateTime.GetCairoDayRangeUtc(pagedDto.ToDate.Value).EndUtc;
                query = query.Where(x => x.ReportDate < toDate);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(x =>
                    (x.Student != null && x.Student.FullName != null && x.Student.FullName.ToLower().Contains(searchTerm))
                    || (x.Teacher != null && x.Teacher.FullName != null && x.Teacher.FullName.ToLower().Contains(searchTerm))
                    || (x.Subject != null && x.Subject.Name != null && x.Subject.Name.ToLower().Contains(searchTerm))
                    || (x.AcademicCircle != null && x.AcademicCircle.Name != null && x.AcademicCircle.Name.ToLower().Contains(searchTerm))
                    || (x.LessonTitle != null && x.LessonTitle.ToLower().Contains(searchTerm))
                    || (x.NextHomework != null && x.NextHomework.ToLower().Contains(searchTerm))
                    || (x.TeacherNotes != null && x.TeacherNotes.ToLower().Contains(searchTerm)));
            }

            var totalCount = query.Count();
            var reports = query
                .OrderByDescending(x => x.ReportDate)
                .Skip(pagedDto.SkipCount)
                .Take(pagedDto.MaxResultCount)
                .ToList();

            var result = new PagedResultDto<AcademicReportReDto>(totalCount, reports.Select(MapReport).ToList());
            return output.CreateResponse(result);
        }

        public async Task<IResponse<bool>> AddAsync(AcademicReportAddDto model, int userId)
        {
            var output = new Response<bool>();
            model.ReportDate = BusinessDateTime.NormalizeClientDateTimeToUtc(model.ReportDate);

            var validationResult = new AcademicReportValidation().Validate(model);
            if (!validationResult.IsValid)
            {
                return output.AppendErrors(validationResult.Errors);
            }

            var validationMessage = await ValidateReportModelAsync(model, userId);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                return output.AppendError(MessageCodes.BusinessValidationError, validationMessage);
            }

            var dayRange = BusinessDateTime.GetCairoDayRangeUtc(BusinessDateTime.ToCairo(model.ReportDate));
            var duplicateExists = await _academicReportRepository.AnyAsync(x =>
                x.StudentId == model.StudentId
                && x.SubjectId == model.SubjectId
                && x.AcademicCircleId == model.AcademicCircleId
                && x.ReportDate >= dayRange.StartUtc
                && x.ReportDate < dayRange.EndUtc);

            if (duplicateExists)
            {
                return output.AppendError(MessageCodes.BusinessValidationError, "تم تسجيل تقرير لهذه المادة لنفس الطالب في نفس اليوم بالفعل.");
            }

            var entity = new AcademicReport
            {
                AcademicCircleId = model.AcademicCircleId,
                StudentId = model.StudentId,
                TeacherId = model.TeacherId,
                SubjectId = model.SubjectId,
                ReportDate = model.ReportDate,
                StageId = model.StageId,
                LessonTitle = model.LessonTitle?.Trim(),
                StudentPerformanceId = model.StudentPerformanceId,
                PreviousHomeworkStatusId = model.PreviousHomeworkStatusId,
                HomeworkScore = model.HomeworkScore,
                NextHomework = model.NextHomework?.Trim(),
                TeacherNotes = model.TeacherNotes?.Trim(),
                SessionDurationMinutes = model.SessionDurationMinutes,
                CreatedAt = BusinessDateTime.UtcNow,
                CreatedBy = userId,
                IsDeleted = false
            };

            await _academicReportRepository.AddAsync(entity);
            await _unitOfWork.CommitAsync();

            return output.CreateResponse(true);
        }

        public async Task<IResponse<bool>> UpdateAsync(AcademicReportAddDto model, int userId)
        {
            var output = new Response<bool>();
            model.ReportDate = BusinessDateTime.NormalizeClientDateTimeToUtc(model.ReportDate);

            var validationResult = new AcademicReportValidation().Validate(model);
            if (!validationResult.IsValid)
            {
                return output.AppendErrors(validationResult.Errors);
            }

            if (!model.Id.HasValue || model.Id.Value <= 0)
            {
                return output.AppendError(MessageCodes.BusinessValidationError, "معرف التقرير غير صحيح.");
            }

            var report = await BuildScopedReportsQuery(userId).FirstOrDefaultAsync(x => x.Id == model.Id.Value);
            if (report == null)
            {
                return output.AppendError(MessageCodes.NotFound);
            }

            var validationMessage = await ValidateReportModelAsync(model, userId);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                return output.AppendError(MessageCodes.BusinessValidationError, validationMessage);
            }

            var dayRange = BusinessDateTime.GetCairoDayRangeUtc(BusinessDateTime.ToCairo(model.ReportDate));
            var duplicateExists = await _academicReportRepository.AnyAsync(x =>
                x.Id != report.Id
                && x.StudentId == model.StudentId
                && x.SubjectId == model.SubjectId
                && x.AcademicCircleId == model.AcademicCircleId
                && x.ReportDate >= dayRange.StartUtc
                && x.ReportDate < dayRange.EndUtc);

            if (duplicateExists)
            {
                return output.AppendError(MessageCodes.BusinessValidationError, "تم تسجيل تقرير لهذه المادة لنفس الطالب في نفس اليوم بالفعل.");
            }

            report.AcademicCircleId = model.AcademicCircleId;
            report.StudentId = model.StudentId;
            report.TeacherId = model.TeacherId;
            report.SubjectId = model.SubjectId;
            report.ReportDate = model.ReportDate;
            report.StageId = model.StageId;
            report.LessonTitle = model.LessonTitle?.Trim();
            report.StudentPerformanceId = model.StudentPerformanceId;
            report.PreviousHomeworkStatusId = model.PreviousHomeworkStatusId;
            report.HomeworkScore = model.HomeworkScore;
            report.NextHomework = model.NextHomework?.Trim();
            report.TeacherNotes = model.TeacherNotes?.Trim();
            report.SessionDurationMinutes = model.SessionDurationMinutes;
            report.ModefiedAt = BusinessDateTime.UtcNow;
            report.ModefiedBy = userId;

            await _unitOfWork.CommitAsync();

            return output.CreateResponse(true);
        }

        public async Task<IResponse<bool>> DeleteAsync(int id, int userId)
        {
            var output = new Response<bool>();
            var report = await BuildScopedReportsQuery(userId).FirstOrDefaultAsync(x => x.Id == id);
            if (report == null)
            {
                return output.AppendError(MessageCodes.NotFound);
            }

            var me = _userRepository.GetById(userId);
            var userType = (UserTypesEnum)(me?.UserTypeId ?? 0);
            if (userType == UserTypesEnum.Student)
            {
                return output.AppendError(MessageCodes.BusinessValidationError, "غير مسموح لك بحذف تقارير المواد.");
            }

            report.IsDeleted = true;
            report.ModefiedAt = BusinessDateTime.UtcNow;
            report.ModefiedBy = userId;

            await _unitOfWork.CommitAsync();
            return output.CreateResponse(true);
        }

        private IQueryable<AcademicReport> BuildScopedReportsQuery(int userId)
        {
            var me = _userRepository
                .Where(x => x.Id == userId)
                .Select(x => new { x.Id, x.UserTypeId, x.BranchId, x.EducationSystemTypeId })
                .FirstOrDefault();

            if (me == null || !EducationSystemTypeHelper.CanAccessAcademicModule(me.UserTypeId, me.EducationSystemTypeId))
            {
                return _academicReportRepository.Where(x => false);
            }

            var userType = (UserTypesEnum)(me.UserTypeId ?? 0);
            var managerCircleQuery = _academicManagerCircleRepository.GetAll();

            return _academicReportRepository.Where(x =>
                userType == UserTypesEnum.Admin
                || (userType == UserTypesEnum.BranchLeader && x.AcademicCircle != null && x.AcademicCircle.BranchId == me.BranchId)
                || (userType == UserTypesEnum.Manager && managerCircleQuery.Any(mc => mc.ManagerId == me.Id && mc.AcademicCircleId == x.AcademicCircleId))
                || (userType == UserTypesEnum.Teacher && x.TeacherId == me.Id)
                || (userType == UserTypesEnum.Student && x.StudentId == me.Id));
        }

        private AcademicReportReDto MapReport(AcademicReport report)
        {
            return new AcademicReportReDto
            {
                Id = report.Id,
                AcademicCircleId = report.AcademicCircleId,
                AcademicCircleName = report.AcademicCircle?.Name,
                StudentId = report.StudentId,
                StudentName = report.Student?.FullName,
                TeacherId = report.TeacherId,
                TeacherName = report.Teacher?.FullName,
                SubjectId = report.SubjectId,
                SubjectName = report.Subject?.Name,
                ReportDate = report.ReportDate,
                StageId = report.StageId,
                LessonTitle = report.LessonTitle,
                StudentPerformanceId = report.StudentPerformanceId,
                PreviousHomeworkStatusId = report.PreviousHomeworkStatusId,
                HomeworkScore = report.HomeworkScore,
                NextHomework = report.NextHomework,
                TeacherNotes = report.TeacherNotes,
                SessionDurationMinutes = report.SessionDurationMinutes
            };
        }

        private async Task<string?> ValidateReportModelAsync(AcademicReportAddDto model, int userId)
        {
            var me = _userRepository.GetById(userId);
            if (me == null)
            {
                return "المستخدم الحالي غير موجود.";
            }

            if (!EducationSystemTypeHelper.CanAccessAcademicModule(me.UserTypeId, me.EducationSystemTypeId))
            {
                return "المستخدم غير مفعل لنظام المواد الدراسية.";
            }

            var userType = (UserTypesEnum)(me.UserTypeId ?? 0);
            if (userType == UserTypesEnum.Student)
            {
                return "غير مسموح لك بإضافة أو تعديل تقارير المواد.";
            }

            var circle = _academicCircleRepository.GetById(model.AcademicCircleId!.Value);
            if (circle == null || circle.IsDeleted)
            {
                return "الحلقة المختارة غير موجودة.";
            }

            var subject = _academicSubjectRepository.GetById(model.SubjectId!.Value);
            if (subject == null || subject.IsDeleted)
            {
                return "المادة المختارة غير موجودة.";
            }

            var student = _userRepository.GetById(model.StudentId!.Value);
            if (student == null
                || student.UserTypeId != (int)UserTypesEnum.Student
                || !EducationSystemTypeHelper.SupportsAcademic(student.EducationSystemTypeId))
            {
                return "الطالب المختار غير صحيح.";
            }

            var teacher = _userRepository.GetById(model.TeacherId!.Value);
            if (teacher == null
                || teacher.UserTypeId != (int)UserTypesEnum.Teacher
                || !EducationSystemTypeHelper.SupportsAcademic(teacher.EducationSystemTypeId))
            {
                return "المعلم المختار غير صحيح.";
            }

            if (circle.TeacherId != teacher.Id)
            {
                return "المعلم المختار غير مسند لهذه الحلقة.";
            }

            var studentLinkedToCircle = _academicCircleStudentRepository.Any(x => x.AcademicCircleId == circle.Id && x.StudentId == student.Id);
            if (!studentLinkedToCircle)
            {
                return "الطالب المختار غير مسند لهذه الحلقة.";
            }

            if (userType == UserTypesEnum.BranchLeader && circle.BranchId != me.BranchId)
            {
                return "لا يمكنك التعامل مع حلقة خارج فرعك.";
            }

            if (userType == UserTypesEnum.Manager && !_academicManagerCircleRepository.Any(x => x.ManagerId == me.Id && x.AcademicCircleId == circle.Id))
            {
                return "لا يمكنك التعامل مع هذه الحلقة.";
            }

            if (userType == UserTypesEnum.Teacher && (teacher.Id != me.Id || circle.TeacherId != me.Id))
            {
                return "لا يمكنك إضافة أو تعديل تقرير لمعلم آخر.";
            }

            return null;
        }
    }
}
