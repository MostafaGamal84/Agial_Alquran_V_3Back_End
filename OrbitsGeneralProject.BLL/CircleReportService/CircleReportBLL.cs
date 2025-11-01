using AutoMapper;
using Microsoft.Extensions.Hosting;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.BLL.Validation.CircleReportValidation;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Enums;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO.CircleReportDtos;
using Orbits.GeneralProject.DTO.ManagerDto;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.Repositroy.Base;
using System;
using System.Linq.Expressions;

namespace Orbits.GeneralProject.BLL.CircleReportService
{
    public class CircleReportBLL : BaseBLL, ICircleReportBLL
    {
        private readonly IRepository<CircleReport> _circleReportRepository;
        private readonly IRepository<TeacherReportRecord> _teacherReportRecordRepository;
        private readonly IRepository<StudentSubscribe> _studentSubscribeRecordRepository;
        private readonly IRepository<SubscribeType> _subscribeTypeRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Nationality> _nationalityRepository;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public CircleReportBLL(IMapper mapper, IRepository<CircleReport> circleReportRepository,
             IUnitOfWork unitOfWork,
             IHostEnvironment hostEnvironment, IRepository<ManagerCircle> managerCircleRepository, IRepository<User> userRepository, IRepository<TeacherReportRecord> teacherReportRecordRepository, IRepository<StudentSubscribe> studentSubscribeRecordRepository, IRepository<SubscribeType> subscribeTypeRepository, IRepository<Nationality> nationalityRepository) : base(mapper)
        {
            _circleReportRepository = circleReportRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userRepository = userRepository;
            _teacherReportRecordRepository = teacherReportRecordRepository;
            _studentSubscribeRecordRepository = studentSubscribeRecordRepository;
            _subscribeTypeRepository = subscribeTypeRepository;
            _nationalityRepository = nationalityRepository;
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
                .Select(u => new { u.Id, u.UserTypeId })
                .FirstOrDefault();

            if (me == null) return output.AppendError(MessageCodes.NotFound);

            var userType = (UserTypesEnum)(me.UserTypeId ?? 0);
            string? sw = pagedDto.SearchTerm?.Trim().ToLower();

            bool isAdmin = userType == UserTypesEnum.Admin;
            bool isManager = userType == UserTypesEnum.Manager;
            bool isTeacher = userType == UserTypesEnum.Teacher;

            // ?????? ??????: ??????? + ????? ????? + ???
            Expression<Func<CircleReport, bool>> predicate = r =>
                // -------- ??????? ----------
                !r.IsDeleted &&
                (
                    // Admin: ?? ????????
                    isAdmin

                    // Manager: ?????? ????? ?????? ??? ???????
                    || (isManager &&
                        (r.Student != null && r.Student.ManagerId == me.Id)
                       )

                    // Teacher: ?????? ????? ?? ????? ??????? ?????? ??????
                    || (isTeacher &&
                        (
                            
                             (r.Student != null && r.Student.TeacherId == me.Id)
                        )
                       )
                )

                // -------- ????? ??????? ----------
                && (!circleId.HasValue || r.CircleId == circleId.Value)
                && (!studentId.HasValue || r.StudentId == studentId.Value)
                && (!nationalityId.HasValue || (r.Student != null && r.Student.NationalityId == nationalityId.Value))

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

            var subscribeType = student.StudentSubscribes.LastOrDefault().StudentSubscribeType;
            var hourlyRate = ResolveHourlyRate(subscribeType, teacher);

            var teacherReportRecord = new TeacherReportRecord
            {
                CircleReportId = created.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                IsDeleted = false,
                Minutes = (int)created.Minutes!.Value,
                TeacherId = created.TeacherId,
                CircleSallary = CalculateTeacherSalary(hourlyRate, created.Minutes)
            };
            _teacherReportRecordRepository.Add(teacherReportRecord);
            studentSubscribe.ModefiedAt = DateTime.UtcNow;
            studentSubscribe.ModefiedBy = userId;
            studentSubscribe.RemainingMinutes = studentSubscribe.RemainingMinutes - (int)model.Minutes;

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
                var pricePerUnit = ResolveHourlyRate(subscribeType, teacher);

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

        private int CalculateTeacherSalary(decimal hourlyRate, double? minutes)
        {
            if (minutes == null)
            {
                return 0;
            }

            decimal minutesValue = Convert.ToDecimal(minutes.Value);
            var total = hourlyRate * minutesValue;
            return (int)Math.Round(total, MidpointRounding.AwayFromZero);
        }

        private int CalculateTeacherSalary(decimal hourlyRate, int minutes)
        {
            var total = hourlyRate * minutes;
            return (int)Math.Round(total, MidpointRounding.AwayFromZero);
        }

        private decimal ResolveHourlyRate(SubscribeType? subscribeType, User teacher)
        {
            if (subscribeType == null)
            {
                return 0m;
            }

            if (subscribeType.Type.HasValue)
            {
                return ResolveHourlyRateBySubscriptionType(subscribeType);
            }

            if (teacher.ForignTeacher == true)
            {
                return subscribeType.ForignPricePerHour
                    ?? subscribeType.ArabPricePerHour
                    ?? subscribeType.EgyptPricePerHour
                    ?? 0m;
            }

            if (IsEgyptianTeacher(teacher))
            {
                return subscribeType.EgyptPricePerHour
                    ?? subscribeType.ArabPricePerHour
                    ?? subscribeType.ForignPricePerHour
                    ?? 0m;
            }

            return subscribeType.ArabPricePerHour
                ?? subscribeType.EgyptPricePerHour
                ?? subscribeType.ForignPricePerHour
                ?? 0m;
        }

        private decimal ResolveHourlyRateBySubscriptionType(SubscribeType subscribeType)
        {
            return subscribeType.Type!.Value switch
            {
                SubscribeTypeCategory.Foreign => subscribeType.ForignPricePerHour
                    ?? subscribeType.ArabPricePerHour
                    ?? subscribeType.EgyptPricePerHour
                    ?? 0m,
                SubscribeTypeCategory.Arab => subscribeType.ArabPricePerHour
                    ?? subscribeType.EgyptPricePerHour
                    ?? subscribeType.ForignPricePerHour
                    ?? 0m,
                SubscribeTypeCategory.Egyptian => subscribeType.EgyptPricePerHour
                    ?? subscribeType.ArabPricePerHour
                    ?? subscribeType.ForignPricePerHour
                    ?? 0m,
                _ => subscribeType.ArabPricePerHour
                    ?? subscribeType.EgyptPricePerHour
                    ?? subscribeType.ForignPricePerHour
                    ?? 0m,
            };
        }

        private bool IsEgyptianTeacher(User teacher)
        {
            if (teacher == null)
            {
                return false;
            }

            if (teacher.GovernorateId.HasValue)
            {
                return true;
            }

            if (!teacher.NationalityId.HasValue)
            {
                return false;
            }

            var nationality = _nationalityRepository.GetById(teacher.NationalityId.Value);
            if (nationality == null)
            {
                return false;
            }

            if (nationality.TelCode.HasValue && nationality.TelCode.Value == 20)
            {
                return true;
            }

            var name = nationality.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            name = name.Trim();
            return name.Contains("egypt", StringComparison.OrdinalIgnoreCase)
                || name.Contains("egyptian", StringComparison.OrdinalIgnoreCase)
                || name.Contains("مصر", StringComparison.OrdinalIgnoreCase)
                || name.Contains("مصري", StringComparison.OrdinalIgnoreCase);
        }

    }
}
