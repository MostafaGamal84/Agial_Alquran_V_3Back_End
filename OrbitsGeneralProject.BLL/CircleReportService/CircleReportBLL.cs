using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.Helpers;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.BLL.Validation.CircleReportValidation;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO.CircleReportDtos;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.Repositroy.Base;
using System;
using System.Linq.Expressions;
using Orbits.GeneralProject.Repositroy.Enums;

namespace Orbits.GeneralProject.BLL.CircleReportService
{
    public class CircleReportBLL : BaseBLL, ICircleReportBLL
    {
        private readonly IRepository<CircleReport> _circleReportRepository;
        private readonly IRepository<StudentSubscribe> _studentSubscribeRecordRepository;
        private readonly IRepository<SubscribeType> _subscribeTypeRepository;
        private readonly IRepository<Nationality> _nationalityRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<ManagerTeacher> _managerTeacherRepository;
        private readonly IRepository<ManagerStudent> _managerStudentRepository;
        private readonly IRepository<TeacherSallary> _teacherSallaryRepository;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public CircleReportBLL(IMapper mapper, IRepository<CircleReport> circleReportRepository,
             IUnitOfWork unitOfWork,
             IRepository<User> userRepository, IRepository<StudentSubscribe> studentSubscribeRecordRepository, IRepository<SubscribeType> subscribeTypeRepository, IRepository<Nationality> nationalityRepository, IRepository<ManagerTeacher> managerTeacherRepository, IRepository<ManagerStudent> managerStudentRepository, IRepository<TeacherSallary> teacherSallaryRepository) : base(mapper)
        {
            _circleReportRepository = circleReportRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userRepository = userRepository;
            _studentSubscribeRecordRepository = studentSubscribeRecordRepository;
            _subscribeTypeRepository = subscribeTypeRepository;
            _nationalityRepository = nationalityRepository;
            _managerTeacherRepository = managerTeacherRepository;
            _managerStudentRepository = managerStudentRepository;
            _teacherSallaryRepository = teacherSallaryRepository;
        }



        public IResponse<PagedResultDto<CircleReportReDto>> GetDeletedPagedList(FilteredResultRequestDto pagedDto)
        {
            var output = new Response<PagedResultDto<CircleReportReDto>>();
            string? sw = pagedDto.SearchTerm?.Trim().ToLower();

            var deletedReportsQuery = _circleReportRepository
                .DisableFilter(nameof(DynamicFilters.IsDeleted))
                .Where(r =>
                    string.IsNullOrEmpty(sw)
                    || (r.Student != null && r.Student.FullName != null && r.Student.FullName.ToLower().Contains(sw))
                    || (r.Teacher != null && r.Teacher.FullName != null && r.Teacher.FullName.ToLower().Contains(sw))
                    || (r.Circle != null && r.Circle.Name != null && r.Circle.Name.ToLower().Contains(sw))
                    || (r.Other != null && r.Other.ToLower().Contains(sw)));

            var totalCount = deletedReportsQuery.Count();
            var entities = deletedReportsQuery
                .Include(r => r.Teacher)
                .Include(r => r.Student)
                .Include(r => r.Circle)
                .OrderByDescending(r => r.CreatedAt ?? r.CreationTime)
                .Skip(pagedDto.SkipCount)
                .Take(pagedDto.MaxResultCount)
                .ToList();

            var result = new PagedResultDto<CircleReportReDto>
            {
                Items = _mapper.Map<List<CircleReportReDto>>(entities),
                TotalCount = totalCount
            };

            return output.CreateResponse(result);
        }

        public IResponse<PagedResultDto<CircleReportReDto>> GetPagedList(
            FilteredResultRequestDto pagedDto,
            int userId,
            int? circleId,
            int? studentId,
            int? nationalityId)
        {
            var output = new Response<PagedResultDto<CircleReportReDto>>();

            var me = _userRepository
                .Where(u => u.Id == userId)
                .Select(u => new { u.Id, u.UserTypeId, u.BranchId })
                .FirstOrDefault();

            if (me == null) return output.AppendError(MessageCodes.NotFound);

            var userType = (UserTypesEnum)(me.UserTypeId ?? 0);
            string? sw = pagedDto.SearchTerm?.Trim().ToLower();

            bool isAdmin = userType == UserTypesEnum.Admin;
            bool isBranchLeader = userType == UserTypesEnum.BranchLeader;
            bool isManager = userType == UserTypesEnum.Manager;
            bool isTeacher = userType == UserTypesEnum.Teacher;
            bool applyBranchRestriction = me.BranchId.HasValue;
            var residentGroup = ResidentGroupFilterHelper.Parse(pagedDto?.ResidentGroup);
            var residentIdsFilter = ResidentGroupFilterHelper.ResolveResidentIds(_nationalityRepository.GetAll(), residentGroup);
            bool applyResidentFilter = residentIdsFilter != null;
            var managerStudentsQuery = _managerStudentRepository.GetAll();
            var fromDate = pagedDto.FromDate?.Date;
            var toDateExclusive = pagedDto.ToDate?.Date.AddDays(1);

            Expression<Func<CircleReport, bool>> predicate = r =>
                !r.IsDeleted &&
                (
                    isAdmin
                    || (isBranchLeader &&
                        (r.Student != null && r.Student.BranchId.HasValue && r.Student.BranchId == me.BranchId))
                    || (isManager &&
                        (r.Student != null && managerStudentsQuery.Any(ms => ms.ManagerId == me.Id && ms.StudentId == r.StudentId)))
                    || (isTeacher && r.TeacherId == me.Id)
                )
                && (!applyBranchRestriction || (r.Circle != null && r.Circle.BranchId.HasValue && r.Circle.BranchId == me.BranchId))
                && (!circleId.HasValue || r.CircleId == circleId.Value)
                && (!studentId.HasValue || r.StudentId == studentId.Value)
                && (!nationalityId.HasValue || (r.Student != null && r.Student.NationalityId == nationalityId.Value))
                && (!applyResidentFilter || (r.Student != null && r.Student.ResidentId.HasValue && residentIdsFilter!.Contains(r.Student.ResidentId.Value)))
                && (!fromDate.HasValue || r.CreationTime >= fromDate.Value)
                && (!toDateExclusive.HasValue || r.CreationTime < toDateExclusive.Value)
                && (
                    string.IsNullOrEmpty(sw)
                    || (r.Student != null && r.Student.FullName != null && r.Student.FullName.ToLower().Contains(sw))
                    || (r.Teacher != null && r.Teacher.FullName != null && r.Teacher.FullName.ToLower().Contains(sw))
                    || (r.Circle != null && r.Circle.Name != null && r.Circle.Name.ToLower().Contains(sw))
                    || (r.Other != null && r.Other.ToLower().Contains(sw))
                );

            var list = GetPagedList<CircleReportReDto, CircleReport, DateTime>(
                pagedDto,
                repository: _circleReportRepository,
                r => r.CreatedAt ?? r.CreationTime,
                searchExpression: predicate,
                sortDirection: "DESC",
                disableFilter: true,
                excluededColumns: null,
                includeProperties: new Expression<Func<CircleReport, object>>[] { r => r.Student, r => r.Teacher, r => r.Circle }
            );

            return output.CreateResponse(list);
        }

        public IResponse<PagedResultDto<CircleReportReDto>> GetMobilePagedList(
            FilteredResultRequestDto pagedDto,
            int userId,
            int? circleId,
            int? studentId,
            int? nationalityId,
            int? teacherId)
        {
            var output = new Response<PagedResultDto<CircleReportReDto>>();
            var query = BuildMobileScopedReportsQuery(pagedDto, userId, circleId, studentId, nationalityId, teacherId);

            var totalCount = query.Count();
            var items = query
                .OrderByDescending(r => r.CreatedAt ?? r.CreationTime)
                .Skip(pagedDto.SkipCount)
                .Take(pagedDto.MaxResultCount)
                .Select(r => new CircleReportReDto
                {
                    Id = r.Id,
                    Minutes = r.Minutes,
                    NewId = r.NewId,
                    NewFrom = r.NewFrom,
                    NewTo = r.NewTo,
                    RecentPast = r.RecentPast,
                    DistantPast = r.DistantPast,
                    FarthestPast = r.FarthestPast,
                    GeneralRate = r.GeneralRate,
                    TheWordsQuranStranger = r.TheWordsQuranStranger,
                    Intonation = r.Intonation,
                    Other = r.Other,
                    CreationTime = r.CreationTime,
                    CircleId = r.CircleId,
                    StudentId = r.StudentId,
                    TeacherId = r.TeacherId,
                    CircleName = r.Circle != null ? r.Circle.Name : null,
                    StudentName = r.Student != null ? r.Student.FullName : null,
                    TeacherName = r.Teacher != null ? r.Teacher.FullName : null,
                    AttendStatueId = r.AttendStatueId,
                    IsVisual = r.IsVisual,
                    NextCircleOrder = r.NextCircleOrder
                })
                .ToList();

            return output.CreateResponse(new PagedResultDto<CircleReportReDto>(totalCount, items));
        }

        public async Task<IResponse<CircleReportStatsDto>> GetMobileStatsAsync(int userId, int? teacherId, int? studentId, DateTime? month)
        {
            var output = new Response<CircleReportStatsDto>();

            var filter = new FilteredResultRequestDto();
            if (month.HasValue)
            {
                var monthStart = new DateTime(month.Value.Year, month.Value.Month, 1);
                filter.FromDate = monthStart;
                filter.ToDate = monthStart.AddMonths(1).AddDays(-1);
            }

            var counts = await BuildMobileScopedReportsQuery(filter, userId, null, studentId, null, teacherId)
                .GroupBy(r => r.AttendStatueId)
                .Select(g => new
                {
                    StatusId = g.Key ?? 0,
                    Count = g.Count()
                })
                .ToListAsync();

            var normalizedMonth = month.HasValue ? new DateTime(month.Value.Year, month.Value.Month, 1) : (DateTime?)null;

            return output.CreateResponse(new CircleReportStatsDto
            {
                TotalReports = counts.Sum(x => x.Count),
                AttendedCount = counts.Where(x => x.StatusId == (int)AttendStatus.Present).Sum(x => x.Count),
                ExcusedAbsenceCount = counts.Where(x => x.StatusId == (int)AttendStatus.AbsentWithExcuse).Sum(x => x.Count),
                UnexcusedAbsenceCount = counts.Where(x => x.StatusId == (int)AttendStatus.AbsentWithoutExcuse).Sum(x => x.Count),
                TeacherId = teacherId,
                StudentId = studentId,
                Month = normalizedMonth
            });
        }

        private IQueryable<CircleReport> BuildMobileScopedReportsQuery(
            FilteredResultRequestDto? pagedDto,
            int userId,
            int? circleId,
            int? studentId,
            int? nationalityId,
            int? teacherId)
        {
            var me = _userRepository
                .Where(u => u.Id == userId)
                .Select(u => new { u.Id, u.UserTypeId, u.BranchId })
                .FirstOrDefault();

            if (me == null)
            {
                return _circleReportRepository.Where(r => false);
            }

            var userType = (UserTypesEnum)(me.UserTypeId ?? 0);
            string? sw = pagedDto?.SearchTerm?.Trim().ToLower();

            bool isAdmin = userType == UserTypesEnum.Admin;
            bool isBranchLeader = userType == UserTypesEnum.BranchLeader;
            bool isManager = userType == UserTypesEnum.Manager;
            bool isTeacher = userType == UserTypesEnum.Teacher;
            bool isStudent = userType == UserTypesEnum.Student;

            var residentGroup = ResidentGroupFilterHelper.Parse(pagedDto?.ResidentGroup);
            var residentIdsFilter = ResidentGroupFilterHelper.ResolveResidentIds(_nationalityRepository.GetAll(), residentGroup);
            bool applyResidentFilter = residentIdsFilter != null;
            var managerTeachersQuery = _managerTeacherRepository.GetAll();
            var fromDate = pagedDto?.FromDate?.Date;
            var toDateExclusive = pagedDto?.ToDate?.Date.AddDays(1);

            return _circleReportRepository.Where(r =>
                !r.IsDeleted
                && (
                    isAdmin
                    || (isBranchLeader && me.BranchId.HasValue && (
                        (r.Circle != null && r.Circle.BranchId.HasValue && r.Circle.BranchId == me.BranchId)
                        || (r.Student != null && r.Student.BranchId.HasValue && r.Student.BranchId == me.BranchId)
                        || (r.Teacher != null && r.Teacher.BranchId.HasValue && r.Teacher.BranchId == me.BranchId)
                    ))
                    || (isManager && managerTeachersQuery.Any(mt => mt.ManagerId == me.Id && mt.TeacherId == r.TeacherId))
                    || (isTeacher && r.TeacherId == me.Id)
                    || (isStudent && r.StudentId == me.Id)
                )
                && (!circleId.HasValue || r.CircleId == circleId.Value)
                && (!studentId.HasValue || r.StudentId == studentId.Value)
                && (!teacherId.HasValue || r.TeacherId == teacherId.Value)
                && (!nationalityId.HasValue || (r.Student != null && r.Student.NationalityId == nationalityId.Value))
                && (!applyResidentFilter || (r.Student != null && r.Student.ResidentId.HasValue && residentIdsFilter!.Contains(r.Student.ResidentId.Value)))
                && (!fromDate.HasValue || r.CreationTime >= fromDate.Value)
                && (!toDateExclusive.HasValue || r.CreationTime < toDateExclusive.Value)
                && (
                    string.IsNullOrEmpty(sw)
                    || (r.Student != null && r.Student.FullName != null && r.Student.FullName.ToLower().Contains(sw))
                    || (r.Teacher != null && r.Teacher.FullName != null && r.Teacher.FullName.ToLower().Contains(sw))
                    || (r.Circle != null && r.Circle.Name != null && r.Circle.Name.ToLower().Contains(sw))
                    || (r.Other != null && r.Other.ToLower().Contains(sw))
                ));
        }


        public async Task<IResponse<bool>> AddAsync(CircleReportAddDto model, int userId)
        {
            var output = new Response<bool>();
            model.CreationTime = BusinessDateTime.NormalizeClientDateTimeToCairoStorage(model.CreationTime);

            // 1) Validate DTO
            var validator = new CircleReportValidation();
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                return output.AppendErrors(validationResult.Errors);

            // 2) Name unique?
            if (await _circleReportRepository.AnyAsync(x => x.StudentId == model.StudentId && x.CreationTime.Date == model.CreationTime.Date))
                return output.CreateResponse(MessageCodes.ReportAlreadyExists);

            var student = _userRepository.GetById((int)model.StudentId!);
            if (student == null) return output.CreateResponse(MessageCodes.StudentNotFound);
            var teacher = _userRepository.GetById((int)model.TeacherId!);
            if (teacher == null) return output.CreateResponse(MessageCodes.TeacherNotFound);
            if (await IsTeacherMonthLockedAsync(model.TeacherId, model.CreationTime))
                return CreateLockedMonthResponse(output, nameof(model.CreationTime));
            var studentSubscribe = student.StudentSubscribes.LastOrDefault();
            if (studentSubscribe == null) return output.CreateResponse(MessageCodes.StudentSubscribeNotFound);
            //if (studentSubscribe.RemainingMinutes < model.Minutes) return output.CreateResponse(MessageCodes.StudentMinutesNotFound);

            var subscribeType = studentSubscribe.StudentSubscribeType;
            var teacherSalaryMinutes = ResolveTeacherSalaryMinutes(model.AttendStatueId, model.Minutes);
            var teacherSalaryAmount = ResolveTeacherSalaryAmount(subscribeType, model.AttendStatueId, model.Minutes);
            var consumedMinutes = ResolveStudentConsumedMinutes(model.AttendStatueId, model.Minutes);

            // 4) Map & create the circle
            var entity = _mapper.Map<CircleReportAddDto, CircleReport>(model);
            entity.CreatedBy = userId;
            entity.StudentId = student.Id;
            entity.Id = 0;
            entity.CreatedAt = BusinessDateTime.CairoNow;
            entity.IsDeleted = false;
            entity.TeacherSalaryMinutes = teacherSalaryMinutes;
            entity.TeacherSalaryAmount = teacherSalaryAmount;

            await _circleReportRepository.AddAsync(entity);

            if (consumedMinutes > 0)
            {
                studentSubscribe.ModefiedAt = BusinessDateTime.CairoNow;
                studentSubscribe.ModefiedBy = userId;
                studentSubscribe.RemainingMinutes -= consumedMinutes;
            }
           

            await _unitOfWork.CommitAsync();

            return output.CreateResponse(data: true);
        }

        // ???? ??? ????? ?????? ???? AttendStatue ????
        public enum AttendStatus
        {
            Present = 1,            // ?? ??????
            AbsentWithExcuse = 2,   // ???? ????
            AbsentWithoutExcuse = 3 // ???? ???? ???
        }

        private static bool CountsForBilling(int? attendStatusId)
        {
            return attendStatusId == (int)AttendStatus.Present
                || attendStatusId == (int)AttendStatus.AbsentWithoutExcuse;
        }

        private static DateTime GetMonthStart(DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, 1);
        }

        private async Task<bool> IsTeacherMonthLockedAsync(int? teacherId, DateTime creationTime)
        {
            if (!teacherId.HasValue)
            {
                return false;
            }

            var monthStart = GetMonthStart(creationTime);
            return await _teacherSallaryRepository.AnyAsync(x =>
                x.IsDeleted != true
                && x.TeacherId == teacherId.Value
                && x.Month.HasValue
                && x.Month.Value.Year == monthStart.Year
                && x.Month.Value.Month == monthStart.Month
                && x.IsPayed == true);
        }

        private IResponse<bool> CreateLockedMonthResponse(Response<bool> output, string fieldName)
        {
            return output.AppendError(
                MessageCodes.BusinessValidationError,
                fieldName,
                "لا يمكن إضافة أو تعديل أو حذف تقرير في شهر تم تسليم راتبه.");
        }

        public async Task<IResponse<bool>> Update(CircleReportAddDto model, int userId)
        {
            var output = new Response<bool>();
            model.CreationTime = BusinessDateTime.NormalizeClientDateTimeToCairoStorage(model.CreationTime);

            // 1) Validate DTO
            var validator = new CircleReportValidation();
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                return output.AppendErrors(validationResult.Errors);

            // 2) Load existing report (tracked)
            var report = await _circleReportRepository.GetByIdAsync(model.Id!.Value);
            if (report == null) return output.AppendError(MessageCodes.NotFound);
            if (await IsTeacherMonthLockedAsync(report.TeacherId, report.CreationTime))
                return CreateLockedMonthResponse(output, nameof(model.CreationTime));

            // 3) Load student & teacher
            var student = _userRepository.GetById((int)model.StudentId!);
            if (student == null) return output.CreateResponse(MessageCodes.StudentNotFound);

            var teacher = _userRepository.GetById((int)model.TeacherId!);
            if (teacher == null) return output.CreateResponse(MessageCodes.TeacherNotFound);
            if (await IsTeacherMonthLockedAsync(model.TeacherId, model.CreationTime))
                return CreateLockedMonthResponse(output, nameof(model.CreationTime));

            // 4) Ensure student has a subscription
            var studentSubscribe = student.StudentSubscribes.LastOrDefault();
            if (studentSubscribe == null) return output.CreateResponse(MessageCodes.StudentSubscribeNotFound);

            // --- ??? ?? ??? ???? ??????? ---
            int? prevStatusId = report.AttendStatueId;
            int? newStatusId = model.AttendStatueId ?? report.AttendStatueId;
            int prevChargedMinutes = ResolveStudentConsumedMinutes(prevStatusId, report.Minutes);
            int newChargedMinutes = ResolveStudentConsumedMinutes(newStatusId, model.Minutes);
            decimal newTeacherSalaryMinutes = ResolveTeacherSalaryMinutes(newStatusId, model.Minutes);

            // 5) ????? ??????? ??? ??????? ?? ?????? ?????? ?????? ??? ?? ??? ??????
            int deltaRemaining = prevChargedMinutes - newChargedMinutes;
          
            studentSubscribe.RemainingMinutes += deltaRemaining;
            studentSubscribe.ModefiedAt = BusinessDateTime.CairoNow;
            studentSubscribe.ModefiedBy = userId;

            // 6) ????? ??????? ???? (in-place ???? ????? ???? ????)
            _mapper.Map(model, report); // ????? ??????? ???????? ???
            report.ModifiedBy = userId;
            report.ModifiedAt = BusinessDateTime.CairoNow;
            report.StudentId = student.Id; // ????? ?????
            report.IsDeleted = false;
            report.AttendStatueId = newStatusId;
            report.TeacherSalaryMinutes = newTeacherSalaryMinutes;

            var subscribeType = studentSubscribe.StudentSubscribeType;
            var pricePerUnit = ResolveHourlyRate(subscribeType);
            report.TeacherSalaryAmount = newTeacherSalaryMinutes > 0m ? CalculateTeacherSalary(pricePerUnit, newTeacherSalaryMinutes) : 0m;

            // 8) ??? ?? ??? ?????? ?????
            await _unitOfWork.CommitAsync();

            return output.CreateResponse(true);
        }

        public async Task<IResponse<bool>> DeleteAsync(int id, int userId)
        {
            var output = new Response<bool>();

            var report = await _circleReportRepository.GetByIdAsync(id);
            if (report == null || report.IsDeleted)
            {
                return output.AppendError(MessageCodes.NotFound);
            }

            if (await IsTeacherMonthLockedAsync(report.TeacherId, report.CreationTime))
            {
                return CreateLockedMonthResponse(output, nameof(id));
            }

            var student = _userRepository.GetById(report.StudentId ?? 0);
            if (student == null)
            {
                return output.CreateResponse(MessageCodes.StudentNotFound);
            }

            var studentSubscribe = student.StudentSubscribes.LastOrDefault();
            if (studentSubscribe == null)
            {
                return output.CreateResponse(MessageCodes.StudentSubscribeNotFound);
            }

            var chargedMinutes = ResolveStudentConsumedMinutes(report.AttendStatueId, report.Minutes);
            if (chargedMinutes > 0)
            {
                studentSubscribe.RemainingMinutes += chargedMinutes;
                studentSubscribe.ModefiedAt = BusinessDateTime.CairoNow;
                studentSubscribe.ModefiedBy = userId;
            }

            report.IsDeleted = true;
            report.ModifiedBy = userId;
            report.ModifiedAt = BusinessDateTime.CairoNow;

            await _unitOfWork.CommitAsync();

            return output.CreateResponse(true);
        }

        public async Task<IResponse<bool>> RestoreAsync(int id, int userId)
        {
            var output = new Response<bool>();

            var report = await _circleReportRepository.DisableFilter(nameof(DynamicFilters.IsDeleted))
                .FirstOrDefaultAsync(x => x.Id == id);
            if (report == null)
                return output.AppendError(MessageCodes.NotFound, nameof(id), "Entity not found");

            if (!report.IsDeleted)
                return output.CreateResponse(true);

            if (await IsTeacherMonthLockedAsync(report.TeacherId, report.CreationTime))
                return CreateLockedMonthResponse(output, nameof(id));

            report.IsDeleted = false;
            report.ModifiedBy = userId;
            report.ModifiedAt = BusinessDateTime.CairoNow;

            await _unitOfWork.CommitAsync();
            return output.CreateResponse(true);
        }

        private decimal CalculateTeacherSalary(decimal hourlyRate, double? minutes)
        {
            if (minutes == null)
            {
                return 0m;
            }

            decimal minutesValue = Convert.ToDecimal(minutes.Value);
            var total = (hourlyRate / 60) * minutesValue;
            return Math.Round(total, 2, MidpointRounding.AwayFromZero);
        }

        private decimal CalculateTeacherSalary(decimal hourlyRate, decimal minutes)
        {
            var total = (hourlyRate / 60 )  * minutes;
            return Math.Round(total, 2, MidpointRounding.AwayFromZero);
        }

        private decimal ResolveHourlyRate(SubscribeType? subscribeType)
        {
            if (subscribeType == null)
            {
                return 0m;
            }

            return subscribeType.HourPrice ?? 0m;
        }

        private static int ResolveStudentConsumedMinutes(int? attendStatusId, double? minutes)
        {
            if (!CountsForBilling(attendStatusId) || !minutes.HasValue)
            {
                return 0;
            }

            return Convert.ToInt32(minutes.Value);
        }

        private static decimal ResolveTeacherSalaryMinutes(int? attendStatusId, double? minutes)
        {
            if (!CountsForBilling(attendStatusId) || !minutes.HasValue)
            {
                return 0m;
            }

            return Math.Round(Convert.ToDecimal(minutes.Value), 2, MidpointRounding.AwayFromZero);
        }

        private decimal ResolveTeacherSalaryAmount(SubscribeType? subscribeType, int? attendStatusId, double? minutes)
        {
            var teacherSalaryMinutes = ResolveTeacherSalaryMinutes(attendStatusId, minutes);
            if (teacherSalaryMinutes <= 0m)
            {
                return 0m;
            }

            return CalculateTeacherSalary(ResolveHourlyRate(subscribeType), teacherSalaryMinutes);
        }

    }
}
