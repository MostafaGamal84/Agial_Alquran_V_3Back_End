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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileServiceBLL _fileService;

        public TeacherSallaryBLL(
            IMapper mapper,
            IRepository<TeacherReportRecord> teacherReportRepository,
            IRepository<TeacherSallary> teacherSallaryRepository,
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork,
            IFileServiceBLL fileService) : base(mapper)
        {
            _teacherReportRepository = teacherReportRepository;
            _teacherSallaryRepository = teacherSallaryRepository;
            _userRepository = userRepository;
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

        public async Task<IResponse<IEnumerable<TeacherInvoiceDto>>> GetInvoicesAsync(DateTime? month = null, int? teacherId = null)
        {
            var response = new Response<IEnumerable<TeacherInvoiceDto>>();

            try
            {
                var query = _teacherSallaryRepository
                    .Where(x=>x.TeacherId != null);

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

        public async Task<IResponse<TeacherMonthlySummaryDto>> GetMonthlySummaryAsync(int? teacherId = null, DateTime? month = null)
        {
            var response = new Response<IEnumerable<TeacherMonthlySummaryDto>>();

            try
            {
                DateTime reference = month ?? DateTime.UtcNow;
                DateTime monthStart = new(reference.Year, reference.Month, 1);

                if (!month.HasValue)

                {
                    return response.CreateResponse(Array.Empty<TeacherMonthlySummaryDto>());
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

                    var summary = await BuildMonthlySummaryAsync(teacher.Id, teacher.FullName, monthStart);
                    return response.CreateResponse(summary);
                }

                var activeTeacherIds = await _userRepository
                    .Where(user => !user.IsDeleted && user.UserTypeId == (int)UserTypesEnum.Teacher)
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

        public async Task<IResponse<TeacherSallaryDetailsDto>> GetInvoiceDetailsAsync(int invoiceId)
        {
            var response = new Response<TeacherSallaryDetailsDto>();

            try
            {
                var invoiceData = await _teacherSallaryRepository
                    .Where(invoice => invoice.Id == invoiceId)

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
                    summary = await BuildMonthlySummaryAsync(invoiceDto.TeacherId.Value, teacherName, monthStart);

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
                if (invoice == null || invoice.IsDeleted)
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

        public async Task<IResponse<string?>> GetPaymentReceiptPathAsync(int invoiceId)
        {
            var response = new Response<string?>();

            try
            {
                var invoice = await _teacherSallaryRepository.GetByIdAsync(invoiceId);
                if (invoice == null || invoice.IsDeleted)
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

        public async Task<IResponse<TeacherInvoiceDto>> GetInvoiceByIdAsync(int invoiceId)
        {
            var response = new Response<TeacherInvoiceDto>();

            try
            {
                var invoice = await ProjectInvoices(
                        _teacherSallaryRepository.Where(invoice => invoice.Id == invoiceId))

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

        private async Task<TeacherMonthlySummaryDto> BuildMonthlySummaryAsync(int teacherId, string? teacherName, DateTime monthStart)
        {
            DateTime monthEnd = monthStart.AddMonths(1);

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
            double totalSalary = teacherRecords.Sum(r => r.Salary);
            int presentCount = teacherRecords.Count(r => r.AttendStatusId == AttendStatusPresent);
            int absentWithExcuseCount = teacherRecords.Count(r => r.AttendStatusId == AttendStatusAbsentWithExcuse);
            int absentWithoutExcuseCount = teacherRecords.Count(r => r.AttendStatusId == AttendStatusAbsentWithoutExcuse);

            totalSalary = Math.Round(totalSalary, 2, MidpointRounding.AwayFromZero);

            var invoice = await ProjectInvoices(
                    _teacherSallaryRepository.Where(invoice =>
                        invoice.TeacherId == teacherId &&

                        invoice.Month.HasValue &&
                        invoice.Month.Value.Year == monthStart.Year &&
                        invoice.Month.Value.Month == monthStart.Month))
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
