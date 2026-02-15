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
        private readonly IRepository<TeacherReportRecord> _teacherReportRecordRepository;
        private readonly IRepository<StudentSubscribe> _studentSubscribeRecordRepository;
        private readonly IRepository<SubscribeType> _subscribeTypeRepository;
        private readonly IRepository<Nationality> _nationalityRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<ManagerStudent> _managerStudentRepository;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public CircleReportBLL(IMapper mapper, IRepository<CircleReport> circleReportRepository,
             IUnitOfWork unitOfWork,
             IRepository<User> userRepository, IRepository<TeacherReportRecord> teacherReportRecordRepository, IRepository<StudentSubscribe> studentSubscribeRecordRepository, IRepository<SubscribeType> subscribeTypeRepository, IRepository<Nationality> nationalityRepository, IRepository<ManagerStudent> managerStudentRepository) : base(mapper)
        {
            _circleReportRepository = circleReportRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userRepository = userRepository;
            _teacherReportRecordRepository = teacherReportRecordRepository;
            _studentSubscribeRecordRepository = studentSubscribeRecordRepository;
            _subscribeTypeRepository = subscribeTypeRepository;
            _nationalityRepository = nationalityRepository;
            _managerStudentRepository = managerStudentRepository;
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
                    || (r.Other != null && r.Other.ToLower().Contains(sw)))
                .AsNoTracking();

            var totalCount = deletedReportsQuery.Count();
            var entities = deletedReportsQuery
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
    int? circleId,         // ???? ??????? ?????????
    int? studentId,
    int? nationalityId)        // ???? ??????? ???????
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

            // ?????? ??????: ??????? + ????? ????? + ???
            Expression<Func<CircleReport, bool>> predicate = r =>
                // -------- ??????? ----------
                !r.IsDeleted &&
                (
                    // Admin: ?? ????????
                    isAdmin

                    // Branch Leader: ?????? ????? ?? ?????? ???? ??? ????????
                    || (isBranchLeader &&
                        (r.Student != null && r.Student.BranchId.HasValue && r.Student.BranchId == me.BranchId)
                       )

                    // Manager: ?????? ????? ?????? ??? ???????
                    || (isManager &&
                        (r.Student != null && managerStudentsQuery.Any(ms => ms.ManagerId == me.Id && ms.StudentId == r.StudentId))
                       )

                    // Teacher: ?????? ????? ?? ????? ??????? ?????? ??????
                    || (isTeacher &&
                        (
                            
                             (r.Student != null && r.Student.TeacherId == me.Id)
                        )
                       )
                )

                // -------- ????? ??????? ----------
                && (!applyBranchRestriction || (r.Circle != null && r.Circle.BranchId.HasValue && r.Circle.BranchId == me.BranchId))
                && (!circleId.HasValue || r.CircleId == circleId.Value)
                && (!studentId.HasValue || r.StudentId == studentId.Value)
                && (!nationalityId.HasValue || (r.Student != null && r.Student.NationalityId == nationalityId.Value))
                && (!applyResidentFilter || (r.Student != null && r.Student.ResidentId.HasValue && residentIdsFilter!.Contains(r.Student.ResidentId.Value)))

                // -------- ????? ??????? ----------
                && (
                    string.IsNullOrEmpty(sw)
                    || (r.Student != null && r.Student.FullName != null && r.Student.FullName.ToLower().Contains(sw))
                    || (r.Teacher != null && r.Teacher.FullName != null && r.Teacher.FullName.ToLower().Contains(sw))
                    || (r.Circle != null && r.Circle.Name != null && r.Circle.Name.ToLower().Contains(sw))
                    || (r.Other != null && r.Other.ToLower().Contains(sw))
                );

            // ??????? ?????? ?????
            var list = GetPagedList<CircleReportReDto, CircleReport, DateTime>(
                pagedDto,
                repository: _circleReportRepository,
                r => r.CreatedAt ?? r.CreationTime,
                searchExpression: predicate,
                sortDirection: "DESC",
                disableFilter: true,
                excluededColumns: null
            );

            return output.CreateResponse(list);
        }


        public async Task<IResponse<bool>> AddAsync(CircleReportAddDto model, int userId)
        {
            var output = new Response<bool>();

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
            var studentSubscribe = student.StudentSubscribes.LastOrDefault();
            if (studentSubscribe == null) return output.CreateResponse(MessageCodes.StudentSubscribeNotFound);
            if (studentSubscribe.RemainingMinutes < model.Minutes) return output.CreateResponse(MessageCodes.StudentMinutesNotFound);

            // 4) Map & create the circle
            var entity = _mapper.Map<CircleReportAddDto, CircleReport>(model);
            entity.CreatedBy = userId;
            entity.StudentId = student.Id;
            entity.Id = 0;
            entity.CreatedAt = DateTime.UtcNow;
            entity.IsDeleted = false;

            // 4a) Save circle to get the generated Id
            var created = await _circleReportRepository.AddAsync(entity);



            await _unitOfWork.CommitAsync(); // after this, created.Id is available

            var subscribeType = studentSubscribe.StudentSubscribeType;
            
            if (model.Minutes > 0 || model.Minutes is not null)
            {
                var hourlyRate = ResolveHourlyRate(subscribeType);

                var teacherReportRecord = new TeacherReportRecord
                {
                    CircleReportId = created.Id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                    IsDeleted = false,
                    Minutes = (int)created.Minutes!.Value,
                    TeacherId = created.TeacherId,
                    CircleSallary = CalculateTeacherSalary(hourlyRate , created.Minutes)
                };
                _teacherReportRecordRepository.Add(teacherReportRecord);
                studentSubscribe.ModefiedAt = DateTime.UtcNow;
                studentSubscribe.ModefiedBy = userId;
                studentSubscribe.RemainingMinutes = studentSubscribe.RemainingMinutes - (int)model.Minutes;
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

        public async Task<IResponse<bool>> Update(CircleReportAddDto model, int userId)
        {
            var output = new Response<bool>();

            // 1) Validate DTO
            var validator = new CircleReportValidation();
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                return output.AppendErrors(validationResult.Errors);

            // 2) Load existing report (tracked)
            var report = await _circleReportRepository.GetByIdAsync(model.Id!.Value);
            if (report == null) return output.AppendError(MessageCodes.NotFound);

            // 3) Load student & teacher
            var student = _userRepository.GetById((int)model.StudentId!);
            if (student == null) return output.CreateResponse(MessageCodes.StudentNotFound);

            var teacher = _userRepository.GetById((int)model.TeacherId!);
            if (teacher == null) return output.CreateResponse(MessageCodes.TeacherNotFound);

            // 4) Ensure student has a subscription
            var studentSubscribe = student.StudentSubscribes.LastOrDefault();
            if (studentSubscribe == null) return output.CreateResponse(MessageCodes.StudentSubscribeNotFound);

            // --- ??? ?? ??? ???? ??????? ---
            int prevMinutes = (int)(report.Minutes ?? 0);
            int newMinutes = (int)(model.Minutes ?? 0);

            int? prevStatusId = report.AttendStatueId;
            int? newStatusId = model.AttendStatueId ?? report.AttendStatueId;

            bool prevCounts = CountsForBilling(prevStatusId);
            bool newCounts = CountsForBilling(newStatusId);

            // 5) ????? ???? ????? ?????? ??? ?????? ??????
            // Four cases:
            // A) prev: count & new: count   -> ???/????? ??? ??????? ???
            // B) prev: count & new: noCount -> ????? ?? ????? ??????? ???????
            // C) prev: noCount & new: count -> ??? ?? ????? ??????? ???????
            // D) prev: noCount & new: noCount -> ?? ?????
            int deltaRemaining = 0;
            if (prevCounts && newCounts)
            {
                // remaining -= (new - prev)  ==> delta = -(new - prev)
                deltaRemaining = -(newMinutes - prevMinutes);
            }
            else if (prevCounts && !newCounts)
            {
                // ???? ???? ??????? ???? ???? ????? ??????
                deltaRemaining = +prevMinutes;
            }
            else if (!prevCounts && newCounts)
            {
                // ???? ???? ??????? ???????
                deltaRemaining = -newMinutes;
            }
            // else: 0

            studentSubscribe.RemainingMinutes += deltaRemaining;
            studentSubscribe.ModefiedAt = DateTime.UtcNow;
            studentSubscribe.ModefiedBy = userId;

            // 6) ????? ??????? ???? (in-place ???? ????? ???? ????)
            _mapper.Map(model, report); // ????? ??????? ???????? ???
            report.ModifiedBy = userId;
            report.ModifiedAt = DateTime.UtcNow;
            report.StudentId = student.Id; // ????? ?????
            report.IsDeleted = false;

            // 7) ???? ?????? ????? ??? ?????? ??????? ??? (????/???? ???? ???)
            var teacherReport = _teacherReportRecordRepository
                .Where(x => x.CircleReportId == model.Id).FirstOrDefault();

            if (teacherReport != null)
            {
                // ??? ???????/?????? ??? ??? ???????? ???????
                var subscribeType = studentSubscribe.StudentSubscribeType;
                var pricePerUnit = ResolveHourlyRate(subscribeType);

                if (newCounts)
                {
                    teacherReport.Minutes = newMinutes;
                    teacherReport.CircleSallary = CalculateTeacherSalary(pricePerUnit, newMinutes);
                }
                else
                {
                    // ????? ?????? ??? ?????? ????
                    teacherReport.Minutes = 0;
                    teacherReport.CircleSallary = 0;
                }

                teacherReport.ModefiedAt = DateTime.UtcNow;
                teacherReport.ModefiedBy = userId;
            }

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

            if (CountsForBilling(report.AttendStatueId) && report.Minutes.HasValue)
            {
                studentSubscribe.RemainingMinutes += (int)report.Minutes.Value;
                studentSubscribe.ModefiedAt = DateTime.UtcNow;
                studentSubscribe.ModefiedBy = userId;
            }

            report.IsDeleted = true;
            report.ModifiedBy = userId;
            report.ModifiedAt = DateTime.UtcNow;

            var teacherReport = _teacherReportRecordRepository
                .Where(x => x.CircleReportId == id && x.IsDeleted != true)
                .FirstOrDefault();

            if (teacherReport != null)
            {
                teacherReport.IsDeleted = true;
                teacherReport.ModefiedAt = DateTime.UtcNow;
                teacherReport.ModefiedBy = userId;
            }

            await _unitOfWork.CommitAsync();

            return output.CreateResponse(true);
        }

        private int CalculateTeacherSalary(decimal hourlyRate, double? minutes)
        {
            if (minutes == null)
            {
                return 0;
            }

            decimal minutesValue = Convert.ToDecimal(minutes.Value);
            var total = (hourlyRate / 60) * minutesValue;
            return (int)Math.Round(total, MidpointRounding.AwayFromZero);
        }

        private int CalculateTeacherSalary(decimal hourlyRate, int minutes)
        {
            var total = (hourlyRate / 60 )  * minutes;
            return (int)Math.Round(total, MidpointRounding.AwayFromZero);
        }

        private decimal ResolveHourlyRate(SubscribeType? subscribeType)
        {
            if (subscribeType == null)
            {
                return 0m;
            }

            return subscribeType.HourPrice ?? 0m;
        }

    }
}
