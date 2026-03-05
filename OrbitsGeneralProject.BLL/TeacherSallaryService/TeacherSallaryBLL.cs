using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.FilesUploaderService;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO.TeacherSallaryDtos;
using Orbits.GeneralProject.Repositroy.Base;

namespace Orbits.GeneralProject.BLL.TeacherSallaryService
{
    public class TeacherSallaryBLL : BaseBLL, ITeacherSallaryBLL
    {
        private const int AttendStatusPresent = 1;
        private const int AttendStatusAbsentWithExcuse = 2;
        private const int AttendStatusAbsentWithoutExcuse = 3;

        private readonly IRepository<TeacherReportRecord> _teacherReportRepository;
        private readonly IRepository<TeacherSallary> _teacherSallaryRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<ManagerTeacher> _managerTeacherRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileServiceBLL _fileService;

        public TeacherSallaryBLL(
            IMapper mapper,
            IRepository<TeacherReportRecord> teacherReportRepository,
            IRepository<TeacherSallary> teacherSallaryRepository,
            IRepository<User> userRepository,
            IRepository<ManagerTeacher> managerTeacherRepository,
            IUnitOfWork unitOfWork,
            IFileServiceBLL fileService) : base(mapper)
        {
            _teacherReportRepository = teacherReportRepository;
            _teacherSallaryRepository = teacherSallaryRepository;
            _userRepository = userRepository;
            _managerTeacherRepository = managerTeacherRepository;
            _unitOfWork = unitOfWork;
            _fileService = fileService;
        }

        public async Task<IResponse<TeacherSallaryGenerationResultDto>> GenerateMonthlyInvoicesAsync(DateTime? month = null, int? createdBy = null)
        {
            var response = new Response<TeacherSallaryGenerationResultDto>();

            try
            {
                DateTime reference = month ?? DateTime.UtcNow;
                DateTime monthStart = new(reference.Year, reference.Month, 1);

                // When no explicit month is supplied we assume the job is running at the beginning
                // of a new month and therefore should generate invoices for the previous month.
                if (!month.HasValue)
                {
                    monthStart = monthStart.AddMonths(-1);
                }

                DateTime monthEnd = monthStart.AddMonths(1);

                var groupedTeacherRecords = await _teacherReportRepository
                    .Where(record =>
                        record.TeacherId.HasValue &&
                        record.CreatedAt.HasValue &&
                        record.CreatedAt.Value >= monthStart &&
                        record.CreatedAt.Value < monthEnd)
                    .GroupBy(record => record.TeacherId!.Value)
                    .Select(group => new
                    {
                        TeacherId = group.Key,
                        TotalMinutes = group.Sum(r => r.Minutes ?? 0),
                        TotalSalary = group.Sum(r => (double?)(r.CircleSallary ?? 0)) ?? 0d
                    })
                    .ToListAsync();

                var result = new TeacherSallaryGenerationResultDto
                {
                    Month = monthStart
                };

                if (groupedTeacherRecords.Count == 0)
                {
                    return response.CreateResponse(result);
                }

                var existingInvoices = await _teacherSallaryRepository
                    .Where(invoice =>
                        invoice.TeacherId.HasValue &&
                        invoice.Month.HasValue &&
                        invoice.Month.Value.Year == monthStart.Year &&
                        invoice.Month.Value.Month == monthStart.Month)
                    .ToListAsync();

                var existingByTeacher = new Dictionary<int, TeacherSallary>();
                foreach (var invoice in existingInvoices)
                {
                    if (invoice.TeacherId.HasValue && !existingByTeacher.ContainsKey(invoice.TeacherId.Value))
                    {
                        existingByTeacher.Add(invoice.TeacherId.Value, invoice);
                    }
                }

                var newInvoices = new List<TeacherSallary>();
                bool hasChanges = false;

                foreach (var group in groupedTeacherRecords)
                {
                    if (group.TotalMinutes <= 0)
                    {
                        result.SkippedZeroValueInvoices++;
                        continue;
                    }

                    result.TotalTeachers++;
                    result.TotalMinutes += group.TotalMinutes;
                    var roundedAmount = Math.Round(group.TotalSalary, 2, MidpointRounding.AwayFromZero);
                    result.TotalSalary += roundedAmount;

                    if (existingByTeacher.TryGetValue(group.TeacherId, out var existingInvoice))
                    {
                        if (existingInvoice.IsPayed == true)
                        {
                            result.SkippedPaidInvoices++;
                            continue;
                        }

                        bool shouldUpdate = false;

                        if (!existingInvoice.Month.HasValue || existingInvoice.Month.Value.Date != monthStart.Date)
                        {
                            existingInvoice.Month = monthStart;
                            shouldUpdate = true;
                        }

                        if (existingInvoice.Sallary == null || Math.Abs(existingInvoice.Sallary.Value - roundedAmount) > 0.01)
                        {
                            existingInvoice.Sallary = roundedAmount;
                            shouldUpdate = true;
                        }

                        if (shouldUpdate)
                        {
                            existingInvoice.ModefiedAt = DateTime.UtcNow;
                            existingInvoice.ModefiedBy = createdBy;
                            result.UpdatedInvoices++;
                            hasChanges = true;
                        }
                    }
                    else
                    {
                        var invoice = new TeacherSallary
                        {
                            TeacherId = group.TeacherId,
                            Month = monthStart,
                            Sallary = roundedAmount,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = createdBy,
                            IsPayed = false
                        };

                        newInvoices.Add(invoice);
                        result.CreatedInvoices++;
                        hasChanges = true;
                    }
                }

                if (newInvoices.Count > 0)
                {
                    _teacherSallaryRepository.Add(newInvoices);
                }

                if (hasChanges)
                {
                    await _unitOfWork.CommitAsync();
                }

                return response.CreateResponse(result);
            }
            catch (Exception ex)
            {
                return response.CreateResponse(ex);
            }
        }

        public async Task<IResponse<IEnumerable<TeacherInvoiceDto>>> GetInvoicesAsync(int requesterUserId, DateTime? month = null, int? teacherId = null)
        {
            var response = new Response<IEnumerable<TeacherInvoiceDto>>();

            try
            {
                var scopeResponse = await ResolveSalaryAccessScopeAsync(requesterUserId);
                if (!scopeResponse.IsSuccess || scopeResponse.Data == null)
                {
                    return response.AppendErrors(scopeResponse.Errors);
                }

                var query = ApplyInvoiceScope(_teacherSallaryRepository
                    .Where(x => x.TeacherId != null), scopeResponse.Data);

                if (teacherId.HasValue)
                {
                    query = query.Where(invoice => invoice.TeacherId == teacherId.Value);
                }

                if (month.HasValue)
                {
                    var monthStart = new DateTime(month.Value.Year, month.Value.Month, 1);
                    var monthEnd = monthStart.AddMonths(1);

                    query = query.Where(invoice =>
                        invoice.Month.HasValue &&
                        invoice.Month.Value >= monthStart &&
                        invoice.Month.Value < monthEnd);
                }

                var invoices = await ProjectInvoices(query)
                    .OrderByDescending(invoice => invoice.Month)
                    .ThenBy(invoice => invoice.TeacherName)
                    .ToListAsync();

                return response.CreateResponse(invoices);
            }
            catch (Exception ex)
            {
                return response.CreateResponse(ex);
            }
        }

        public async Task<IResponse<TeacherMonthlySummaryDto>> GetMonthlySummaryAsync(int requesterUserId, int? teacherId = null, DateTime? month = null)
        {
            var response = new Response<TeacherMonthlySummaryDto>();

            try
            {
                DateTime reference = month ?? DateTime.UtcNow;
                DateTime monthStart = new(reference.Year, reference.Month, 1);

                if (!month.HasValue)

                {
                    return response.CreateResponse(MessageCodes.Failed);
                }

                if (teacherId.HasValue)
                {
                    var teacher = await _userRepository
                        .Where(user => user.Id == teacherId.Value && !user.IsDeleted)
                        .Select(user => new { user.Id, user.FullName })
                        .FirstOrDefaultAsync();

                    if (teacher == null)
                    {
                        return response.CreateResponse(MessageCodes.TeacherNotFound);
                    }

                    var scopeResponse = await ResolveSalaryAccessScopeAsync(requesterUserId);
                    if (!scopeResponse.IsSuccess || scopeResponse.Data == null)
                    {
                        return response.AppendErrors(scopeResponse.Errors);
                    }

                    var scopedTeacher = await ApplyTeachersScope(_userRepository.Where(u => u.Id == teacher.Id), scopeResponse.Data)
                        .Select(u => u.Id)
                        .FirstOrDefaultAsync();

                    if (scopedTeacher == 0)
                    {
                        return response.CreateResponse(MessageCodes.UnAuthorizedAccess);
                    }

                    var summary = await BuildMonthlySummaryAsync(teacher.Id, teacher.FullName, monthStart, requesterUserId);
                    return response.CreateResponse(summary);
                }

                var scopeForAllResponse = await ResolveSalaryAccessScopeAsync(requesterUserId);
                if (!scopeForAllResponse.IsSuccess || scopeForAllResponse.Data == null)
                {
                    return response.AppendErrors(scopeForAllResponse.Errors);
                }

                var activeTeacherIds = await ApplyTeachersScope(_userRepository.GetAll(), scopeForAllResponse.Data)
                    .Select(user => user.Id)
                    .ToListAsync();

                if (activeTeacherIds.Count == 0)
                {
                    return response.CreateResponse(new TeacherMonthlySummaryDto
                    {
                        TeacherId = 0,
                        TeacherName = null,
                        Month = monthStart,
                        TotalReports = 0,
                        TotalMinutes = 0,
                        PresentCount = 0,
                        AbsentWithExcuseCount = 0,
                        AbsentWithoutExcuseCount = 0,
                        TotalSalary = 0,
                        Invoice = null
                    });
                }

                DateTime monthEnd = monthStart.AddMonths(1);

                var teacherRecords = await _teacherReportRepository
                    .Where(record =>
                        record.TeacherId.HasValue &&
                        activeTeacherIds.Contains(record.TeacherId.Value) &&
                        record.IsDeleted != true &&
                        record.CreatedAt.HasValue &&
                        record.CreatedAt.Value >= monthStart &&
                        record.CreatedAt.Value < monthEnd)
                    .Select(record => new
                    {
                        Minutes = record.Minutes ?? 0,
                        Salary = (double?)(record.CircleSallary ?? 0) ?? 0d,
                        AttendStatusId = record.CircleReport != null ? record.CircleReport.AttendStatueId : null
                    })
                    .ToListAsync();

                var totalSalary = Math.Round(teacherRecords.Sum(r => r.Salary), 2, MidpointRounding.AwayFromZero);

                var aggregateSummary = new TeacherMonthlySummaryDto
                {
                    TeacherId = 0,
                    TeacherName = null,
                    Month = monthStart,
                    TotalReports = teacherRecords.Count,
                    TotalMinutes = teacherRecords.Sum(r => r.Minutes),
                    PresentCount = teacherRecords.Count(r => r.AttendStatusId == AttendStatusPresent),
                    AbsentWithExcuseCount = teacherRecords.Count(r => r.AttendStatusId == AttendStatusAbsentWithExcuse),
                    AbsentWithoutExcuseCount = teacherRecords.Count(r => r.AttendStatusId == AttendStatusAbsentWithoutExcuse),
                    TotalSalary = totalSalary,
                    Invoice = null
                };

                return response.CreateResponse(aggregateSummary);
            }
            catch (Exception ex)
            {
                return response.CreateResponse(ex);
            }
        }


        public async Task<IResponse<IEnumerable<TeacherMonthlyReportRecordDto>>> GetMonthlyReportRecordsAsync(int requesterUserId, int teacherId, DateTime? month)
        {
            var response = new Response<IEnumerable<TeacherMonthlyReportRecordDto>>();

            try
            {
                if (!month.HasValue)
                {
                    return response.CreateResponse(MessageCodes.Failed);
                }

                DateTime monthStart = new(month.Value.Year, month.Value.Month, 1);
                DateTime monthEnd = monthStart.AddMonths(1);

                var scopeResponse = await ResolveSalaryAccessScopeAsync(requesterUserId);
                if (!scopeResponse.IsSuccess || scopeResponse.Data == null)
                {
                    return response.AppendErrors(scopeResponse.Errors);
                }

                bool teacherAllowed = await ApplyTeachersScope(_userRepository.Where(u => u.Id == teacherId), scopeResponse.Data)
                    .AnyAsync();

                if (!teacherAllowed)
                {
                    return response.CreateResponse(MessageCodes.UnAuthorizedAccess);
                }

                var records = await _teacherReportRepository
                    .Where(record =>
                        record.TeacherId == teacherId &&
                        record.IsDeleted != true &&
                        record.CreatedAt.HasValue &&
                        record.CreatedAt.Value >= monthStart &&
                        record.CreatedAt.Value < monthEnd)
                    .Include(record => record.Teacher)
                    .Include(record => record.CircleReport)
                    .ThenInclude(report => report.Student)
                    .Select(record => new TeacherMonthlyReportRecordDto
                    {
                        Id = record.Id,
                        TeacherId = record.TeacherId ?? 0,
                        TeacherName = record.Teacher != null ? record.Teacher.FullName : null,
                        CircleReportId = record.CircleReportId,
                        CircleId = record.CircleReport != null ? record.CircleReport.CircleId : null,
                        StudentId = record.CircleReport != null ? record.CircleReport.StudentId : null,
                        StudentName = record.CircleReport != null && record.CircleReport.Student != null ? record.CircleReport.Student.FullName : null,
                        Minutes = record.Minutes ?? 0,
                        Salary = (double?)(record.CircleSallary ?? 0) ?? 0d,
                        AttendStatusId = record.CircleReport != null ? record.CircleReport.AttendStatueId : null,
                        RecordCreatedAt = record.CreatedAt,
                        CircleReportCreatedAt = record.CircleReport != null ? record.CircleReport.CreatedAt : null
                    })
                    .OrderByDescending(record => record.RecordCreatedAt)
                    .ThenByDescending(record => record.Id)
                    .ToListAsync();

                return response.CreateResponse(records);
            }
            catch (Exception ex)
            {
                return response.CreateResponse(ex);
            }
        }

        public async Task<IResponse<TeacherSallaryDetailsDto>> GetInvoiceDetailsAsync(int requesterUserId, int invoiceId)
        {
            var response = new Response<TeacherSallaryDetailsDto>();

            try
            {
                var scopeResponse = await ResolveSalaryAccessScopeAsync(requesterUserId);
                if (!scopeResponse.IsSuccess || scopeResponse.Data == null)
                {
                    return response.AppendErrors(scopeResponse.Errors);
                }

                var invoiceData = await ApplyInvoiceScope(_teacherSallaryRepository
                    .Where(invoice => invoice.Id == invoiceId), scopeResponse.Data)

                    .Select(invoice => new
                    {
                        Invoice = invoice,
                        TeacherName = invoice.Teacher != null ? invoice.Teacher.FullName : null
                    })
                    .FirstOrDefaultAsync();

                if (invoiceData == null)
                {
                    return response.CreateResponse(MessageCodes.NotFound);
                }

                var invoiceDto = new TeacherInvoiceDto
                {
                    Id = invoiceData.Invoice.Id,
                    TeacherId = invoiceData.Invoice.TeacherId,
                    TeacherName = invoiceData.TeacherName,
                    Month = invoiceData.Invoice.Month,
                    Salary = invoiceData.Invoice.Sallary,
                    IsPayed = invoiceData.Invoice.IsPayed,
                    PayedAt = invoiceData.Invoice.PayedAt,
                    ReceiptPath = invoiceData.Invoice.ReceiptPath,
                    CreatedAt = invoiceData.Invoice.CreatedAt,
                    ModefiedAt = invoiceData.Invoice.ModefiedAt
                };

                TeacherMonthlySummaryDto? summary = null;

                if (invoiceDto.TeacherId.HasValue && invoiceDto.Month.HasValue)
                {
                    var teacherName = invoiceDto.TeacherName;

                    if (string.IsNullOrWhiteSpace(teacherName))
                    {
                        teacherName = await _userRepository
                            .Where(user => user.Id == invoiceDto.TeacherId.Value && !user.IsDeleted)
                            .Select(user => user.FullName)
                            .FirstOrDefaultAsync();
                    }

                    var monthStart = new DateTime(invoiceDto.Month.Value.Year, invoiceDto.Month.Value.Month, 1);
                    summary = await BuildMonthlySummaryAsync(invoiceDto.TeacherId.Value, teacherName, monthStart, requesterUserId);

                    if (summary != null && summary.Invoice == null)
                    {
                        summary.Invoice = invoiceDto;
                    }
                }

                var details = new TeacherSallaryDetailsDto
                {
                    Invoice = invoiceDto,
                    MonthlySummary = summary
                };

                return response.CreateResponse(details);
            }
            catch (Exception ex)
            {
                return response.CreateResponse(ex);
            }
        }

        public async Task<IResponse<TeacherInvoiceDto>> UpdateInvoiceStatusAsync(int invoiceId, UpdateTeacherSallaryStatusDto dto, int userId)
        {
            var response = new Response<TeacherInvoiceDto>();

            try
            {
                var invoice = await _teacherSallaryRepository
                    .Where(i => i.Id == invoiceId)

                    .Include(i => i.Teacher)
                    .FirstOrDefaultAsync();

                if (invoice == null)
                {
                    return response.CreateResponse(MessageCodes.NotFound);
                }

                invoice.IsPayed = dto.IsPayed;
                invoice.PayedAt = dto.IsPayed
                    ? dto.PayedAt ?? DateTime.UtcNow
                    : null;
                invoice.ModefiedAt = DateTime.UtcNow;
                invoice.ModefiedBy = userId;

                await _unitOfWork.CommitAsync();

                var updated = await ProjectInvoices(
                        _teacherSallaryRepository.Where(i => i.Id == invoice.Id))

                    .FirstOrDefaultAsync();

                if (updated == null)
                {
                    return response.CreateResponse(MessageCodes.NotFound);
                }

                return response.CreateResponse(updated);
            }
            catch (Exception ex)
            {
                return response.CreateResponse(ex);
            }
        }

        public async Task<IResponse<bool>> UpdatePaymentAsync(UpdateTeacherPaymentDto dto, int userId)
        {
            var response = new Response<bool>();

            try
            {
                var invoice = await _teacherSallaryRepository.GetByIdAsync(dto.Id);
                if (invoice == null || invoice.IsDeleted.Value)
                {
                    return response.CreateResponse(MessageCodes.NotFound);
                }

                invoice.ModefiedBy = userId;
                invoice.ModefiedAt = DateTime.UtcNow;

                if (dto.Amount.HasValue)
                {
                    invoice.Sallary = Math.Round(dto.Amount.Value, 2, MidpointRounding.AwayFromZero);
                }

                var isCancelled = dto.IsCancelled == true;

                if (isCancelled)
                {
                    invoice.IsPayed = false;
                    invoice.PayedAt = null;
                    invoice.ReceiptPath = null;
                }
                else
                {
                    if (dto.PayStatue.HasValue)
                    {
                        invoice.IsPayed = dto.PayStatue.Value;
                        invoice.PayedAt = dto.PayStatue.Value ? DateTime.UtcNow : null;
                    }

                    if (dto.ReceiptPath != null)
                    {
                        var uploadResult = await _fileService.CreateFileAsync(dto.ReceiptPath, "TeacherInvoices/");
                        if (!uploadResult.IsSuccess || uploadResult.Data == null || string.IsNullOrWhiteSpace(uploadResult.Data.FilePath))
                        {
                            if (uploadResult.Errors != null && uploadResult.Errors.Count > 0)
                            {
                                return response.AppendErrors(uploadResult.Errors);
                            }

                            return response.AppendError(MessageCodes.Failed, nameof(dto.ReceiptPath), "Failed to store receipt file.");
                        }

                        invoice.ReceiptPath = uploadResult.Data.FilePath;
                    }
                }

                await _unitOfWork.CommitAsync();

                return response.CreateResponse(true);
            }
            catch (Exception ex)
            {
                return response.CreateResponse(ex);
            }
        }

        public async Task<IResponse<string?>> GetPaymentReceiptPathAsync(int requesterUserId, int invoiceId)
        {
            var response = new Response<string?>();

            try
            {
                var scopeResponse = await ResolveSalaryAccessScopeAsync(requesterUserId);
                if (!scopeResponse.IsSuccess || scopeResponse.Data == null)
                {
                    return response.AppendErrors(scopeResponse.Errors);
                }

                var invoice = await ApplyInvoiceScope(_teacherSallaryRepository.Where(i => i.Id == invoiceId), scopeResponse.Data)
                    .FirstOrDefaultAsync();
                if (invoice == null || invoice.IsDeleted.Value)
                {
                    return response.CreateResponse(MessageCodes.NotFound);
                }

                if (string.IsNullOrWhiteSpace(invoice.ReceiptPath))
                {
                    return response.AppendError(MessageCodes.NotFound, nameof(TeacherSallary.ReceiptPath), "Receipt not found.");
                }

                return response.CreateResponse(invoice.ReceiptPath);
            }
            catch (Exception ex)
            {
                return response.CreateResponse(ex);
            }
        }

        public async Task<IResponse<TeacherInvoiceDto>> GetInvoiceByIdAsync(int requesterUserId, int invoiceId)
        {
            var response = new Response<TeacherInvoiceDto>();

            try
            {
                var scopeResponse = await ResolveSalaryAccessScopeAsync(requesterUserId);
                if (!scopeResponse.IsSuccess || scopeResponse.Data == null)
                {
                    return response.AppendErrors(scopeResponse.Errors);
                }

                var invoice = await ProjectInvoices(
                        ApplyInvoiceScope(_teacherSallaryRepository.Where(invoice => invoice.Id == invoiceId), scopeResponse.Data))

                    .FirstOrDefaultAsync();

                if (invoice == null)
                {
                    return response.CreateResponse(MessageCodes.NotFound);
                }

                return response.CreateResponse(invoice);
            }
            catch (Exception ex)
            {
                return response.CreateResponse(ex);
            }
        }

        private async Task<TeacherMonthlySummaryDto> BuildMonthlySummaryAsync(int teacherId, string? teacherName, DateTime monthStart, int requesterUserId)
        {
            DateTime monthEnd = monthStart.AddMonths(1);

            var scopeResponse = await ResolveSalaryAccessScopeAsync(requesterUserId);
            if (!scopeResponse.IsSuccess || scopeResponse.Data == null)
            {
                return new TeacherMonthlySummaryDto
                {
                    TeacherId = teacherId,
                    TeacherName = teacherName,
                    Month = monthStart
                };
            }

            bool teacherAllowed = await ApplyTeachersScope(_userRepository.Where(u => u.Id == teacherId), scopeResponse.Data)
                .AnyAsync();
            if (!teacherAllowed)
            {
                return new TeacherMonthlySummaryDto
                {
                    TeacherId = teacherId,
                    TeacherName = teacherName,
                    Month = monthStart
                };
            }

            var teacherRecords = await _teacherReportRepository
                .Where(record =>
                    record.TeacherId == teacherId &&
                    record.IsDeleted != true &&
                    record.CreatedAt.HasValue &&
                    record.CreatedAt.Value >= monthStart &&
                    record.CreatedAt.Value < monthEnd)
                .Include(record => record.CircleReport)
                .Select(record => new
                {
                    Minutes = record.Minutes ?? 0,
                    Salary = (double?)(record.CircleSallary ?? 0) ?? 0d,
                    AttendStatusId = record.CircleReport != null ? record.CircleReport.AttendStatueId : null
                })
                .ToListAsync();

            int totalReports = teacherRecords.Count;
            int totalMinutes = teacherRecords.Sum(r => r.Minutes);
            double totalSalary = Math.Round(teacherRecords.Sum(r => r.Salary), 2, MidpointRounding.AwayFromZero);

            int presentCount = teacherRecords.Count(r => r.AttendStatusId == AttendStatusPresent);
            int absentWithExcuseCount = teacherRecords.Count(r => r.AttendStatusId == AttendStatusAbsentWithExcuse);
            int absentWithoutExcuseCount = teacherRecords.Count(r => r.AttendStatusId == AttendStatusAbsentWithoutExcuse);

            var invoice = await ProjectInvoices(
                    ApplyInvoiceScope(
                        _teacherSallaryRepository.Where(invoice =>
                            invoice.TeacherId == teacherId &&
                            invoice.Month.HasValue &&
                            invoice.Month.Value.Year == monthStart.Year &&
                            invoice.Month.Value.Month == monthStart.Month),
                        scopeResponse.Data))
                .FirstOrDefaultAsync();

            if (invoice != null && string.IsNullOrWhiteSpace(invoice.TeacherName))
            {
                invoice.TeacherName = teacherName;
            }

            return new TeacherMonthlySummaryDto
            {
                TeacherId = teacherId,
                TeacherName = teacherName,
                Month = monthStart,
                TotalReports = totalReports,
                TotalMinutes = totalMinutes,
                PresentCount = presentCount,
                AbsentWithExcuseCount = absentWithExcuseCount,
                AbsentWithoutExcuseCount = absentWithoutExcuseCount,
                TotalSalary = totalSalary,
                Invoice = invoice
            };
        }


        private async Task<IResponse<SalaryAccessScope>> ResolveSalaryAccessScopeAsync(int requesterUserId)
        {
            var response = new Response<SalaryAccessScope>();

            var requester = await _userRepository
                .Where(u => u.Id == requesterUserId && !u.IsDeleted)
                .Select(u => new { u.Id, u.UserTypeId, u.BranchId })
                .FirstOrDefaultAsync();

            if (requester == null || !requester.UserTypeId.HasValue)
            {
                return response.CreateResponse(MessageCodes.UnAuthorizedAccess);
            }

            var role = (UserTypesEnum)requester.UserTypeId.Value;

            if (role == UserTypesEnum.Admin)
            {
                return response.CreateResponse(new SalaryAccessScope
                {
                    Mode = SalaryAccessMode.AllTeachers
                });
            }

            if (role == UserTypesEnum.BranchLeader)
            {
                if (!requester.BranchId.HasValue)
                {
                    return response.AppendError(MessageCodes.UnAuthorizedAccess, nameof(User.BranchId), "Branch leader does not have an assigned branch.");
                }

                return response.CreateResponse(new SalaryAccessScope
                {
                    Mode = SalaryAccessMode.BranchTeachers,
                    BranchId = requester.BranchId
                });
            }

            if (role == UserTypesEnum.Manager)
            {
                return response.CreateResponse(new SalaryAccessScope
                {
                    Mode = SalaryAccessMode.ManagerTeachers,
                    ManagerId = requester.Id
                });
            }

            return response.CreateResponse(MessageCodes.UnAuthorizedAccess);
        }

        private IQueryable<User> ApplyTeachersScope(IQueryable<User> query, SalaryAccessScope scope)
        {
            query = query.Where(u => u.UserTypeId == (int)UserTypesEnum.Teacher && !u.IsDeleted);

            if (scope.Mode == SalaryAccessMode.BranchTeachers && scope.BranchId.HasValue)
            {
                int branchId = scope.BranchId.Value;
                query = query.Where(u => u.BranchId == branchId);
            }
            else if (scope.Mode == SalaryAccessMode.ManagerTeachers && scope.ManagerId.HasValue)
            {
                int managerId = scope.ManagerId.Value;
                query = query.Where(u => u.ManagerTeacherTeachers.Any(mt => mt.ManagerId == managerId));
            }

            return query;
        }

        private IQueryable<TeacherSallary> ApplyInvoiceScope(IQueryable<TeacherSallary> query, SalaryAccessScope scope)
        {
            if (scope.Mode == SalaryAccessMode.BranchTeachers && scope.BranchId.HasValue)
            {
                int branchId = scope.BranchId.Value;
                query = query.Where(invoice => invoice.Teacher != null && invoice.Teacher.BranchId == branchId);
            }
            else if (scope.Mode == SalaryAccessMode.ManagerTeachers && scope.ManagerId.HasValue)
            {
                int managerId = scope.ManagerId.Value;
                query = query.Where(invoice =>
                    invoice.Teacher != null &&
                    invoice.Teacher.ManagerTeacherTeachers.Any(mt => mt.ManagerId == managerId));
            }

            return query;
        }

        private sealed class SalaryAccessScope
        {
            public SalaryAccessMode Mode { get; set; }
            public int? BranchId { get; set; }
            public int? ManagerId { get; set; }
        }

        private enum SalaryAccessMode
        {
            AllTeachers = 1,
            BranchTeachers = 2,
            ManagerTeachers = 3
        }
        private static IQueryable<TeacherInvoiceDto> ProjectInvoices(IQueryable<TeacherSallary> query)
        {
            return query.Select(invoice => new TeacherInvoiceDto
            {
                Id = invoice.Id,
                TeacherId = invoice.TeacherId,
                TeacherName = invoice.Teacher != null ? invoice.Teacher.FullName : null,
                Month = invoice.Month,
                Salary = invoice.Sallary,
                IsPayed = invoice.IsPayed,
                PayedAt = invoice.PayedAt,
                ReceiptPath = invoice.ReceiptPath,
                CreatedAt = invoice.CreatedAt,
                ModefiedAt = invoice.ModefiedAt
            });
        }
    }
}
