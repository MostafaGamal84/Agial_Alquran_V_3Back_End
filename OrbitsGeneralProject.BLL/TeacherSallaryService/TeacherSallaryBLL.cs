using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;
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
        private const string FoundationSectionName = "تأسيس";
        private const string RepetitionSectionName = "ترديد";
        private const string MemorizationSectionName = "حفظ";
        private const string OtherSectionName = "أخرى";
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> GenerationLocks = new();

        private readonly IRepository<CircleReport> _circleReportRepository;
        private readonly IRepository<TeacherSallary> _teacherSallaryRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<ManagerTeacher> _managerTeacherRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileServiceBLL _fileService;

        public TeacherSallaryBLL(
            IMapper mapper,
            IRepository<CircleReport> circleReportRepository,
            IRepository<TeacherSallary> teacherSallaryRepository,
            IRepository<User> userRepository,
            IRepository<ManagerTeacher> managerTeacherRepository,
            IUnitOfWork unitOfWork,
            IFileServiceBLL fileService) : base(mapper)
        {
            _circleReportRepository = circleReportRepository;
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
                DateTime reference = month ?? BusinessDateTime.CairoNow;
                DateTime monthStart = new(reference.Year, reference.Month, 1);

                // When no explicit month is supplied we assume the job is running at the beginning
                // of a new month and therefore should generate invoices for the previous month.
                if (!month.HasValue)
                {
                    monthStart = monthStart.AddMonths(-1);
                }

                var generationKey = $"{monthStart.Year:D4}-{monthStart.Month:D2}";
                var generationLock = GenerationLocks.GetOrAdd(generationKey, _ => new SemaphoreSlim(1, 1));
                await generationLock.WaitAsync();

                try
                {
                    var (monthStartUtc, monthEndUtc) = BusinessDateTime.GetCairoMonthRangeUtc(monthStart.Year, monthStart.Month);
                    await BackfillMissingSalarySnapshotsAsync(monthStartUtc, monthEndUtc);

                    // Keep invoice generation aligned with the monthly details endpoints,
                    // which only include active (non-deleted) teacher report records.
                    var reportSnapshots = await _circleReportRepository
                        .Where(report =>
                            report.TeacherId.HasValue &&
                            !report.IsDeleted &&
                            report.CreationTime >= monthStartUtc &&
                            report.CreationTime < monthEndUtc)
                        .Select(report => new
                        {
                            TeacherId = report.TeacherId!.Value,
                            AttendStatusId = report.AttendStatueId,
                            RawMinutes = report.Minutes,
                            SnapshotMinutes = report.TeacherSalaryMinutes,
                            SnapshotSalary = report.TeacherSalaryAmount,
                            HourPrice = report.Student != null
                                ? report.Student.StudentSubscribes
                                    .Where(s =>
                                        s.CreatedAt.HasValue &&
                                        s.CreatedAt.Value >= monthStartUtc &&
                                        s.CreatedAt.Value < monthEndUtc)
                                    .OrderByDescending(s => s.CreatedAt ?? DateTime.MinValue)
                                    .ThenByDescending(s => s.Id)
                                    .Select(s => s.StudentSubscribeType != null ? s.StudentSubscribeType.HourPrice : null)
                                    .FirstOrDefault()
                                : null,
                            FallbackHourPrice = report.Student != null
                                ? report.Student.StudentSubscribes
                                    .OrderByDescending(s => s.CreatedAt ?? DateTime.MinValue)
                                    .ThenByDescending(s => s.Id)
                                    .Select(s => s.StudentSubscribeType != null ? s.StudentSubscribeType.HourPrice : null)
                                    .FirstOrDefault()
                                : null
                        })
                        .ToListAsync();

                    var groupedTeacherRecords = reportSnapshots
                        .GroupBy(report => report.TeacherId)
                        .Select(group => new
                        {
                            TeacherId = group.Key,
                            TotalMinutes = group.Sum(r => ResolveSalaryMinutes(r.AttendStatusId, r.RawMinutes, r.SnapshotMinutes)),
                            TotalSalary = group.Sum(r => ResolveSalaryAmount(
                                r.AttendStatusId,
                                r.RawMinutes,
                                r.SnapshotMinutes,
                                r.SnapshotSalary,
                                r.HourPrice ?? r.FallbackHourPrice))
                        })
                        .ToList();

                    var result = new TeacherSallaryGenerationResultDto
                    {
                        Month = monthStart
                    };

                    if (groupedTeacherRecords.Count == 0)
                    {
                        return response.CreateResponse(result);
                    }

                    var expectedSalaryByTeacher = groupedTeacherRecords.ToDictionary(
                        group => group.TeacherId,
                        group => (double)Math.Round(group.TotalSalary, 2, MidpointRounding.AwayFromZero));

                    var existingInvoices = await FilterActiveInvoices(_teacherSallaryRepository
                        .Where(invoice =>
                            invoice.TeacherId.HasValue &&
                            invoice.Month.HasValue &&
                            invoice.Month.Value.Year == monthStart.Year &&
                            invoice.Month.Value.Month == monthStart.Month))
                        .OrderByDescending(invoice => invoice.IsPayed == true)
                        .ThenByDescending(invoice => invoice.ModefiedAt ?? invoice.CreatedAt ?? DateTime.MinValue)
                        .ThenByDescending(invoice => invoice.Id)
                        .ToListAsync();

                    var duplicateInvoices = new List<TeacherSallary>();
                    var existingByTeacher = new Dictionary<int, TeacherSallary>();
                    foreach (var teacherGroup in existingInvoices
                        .Where(invoice => invoice.TeacherId.HasValue)
                        .GroupBy(invoice => invoice.TeacherId!.Value))
                    {
                        expectedSalaryByTeacher.TryGetValue(teacherGroup.Key, out var expectedSalary);
                        var canonicalInvoice = SelectCanonicalInvoice(
                            teacherGroup,
                            expectedSalaryByTeacher.ContainsKey(teacherGroup.Key) ? expectedSalary : (double?)null);

                        existingByTeacher[teacherGroup.Key] = canonicalInvoice;

                        foreach (var duplicateInvoice in teacherGroup.Where(invoice => invoice.Id != canonicalInvoice.Id))
                        {
                            duplicateInvoices.Add(duplicateInvoice);
                        }
                    }

                    var newInvoices = new List<TeacherSallary>();
                    bool hasChanges = false;

                    foreach (var duplicateInvoice in duplicateInvoices)
                    {
                        if (TryMarkInvoiceAsDeleted(duplicateInvoice, createdBy))
                        {
                            hasChanges = true;
                        }
                    }

                    foreach (var group in groupedTeacherRecords)
                    {
                        result.TotalTeachers++;
                        result.TotalMinutes += (double)group.TotalMinutes;
                        var roundedAmount = Math.Round(group.TotalSalary, 2, MidpointRounding.AwayFromZero);
                        var roundedAmountAsDouble = (double)roundedAmount;
                        result.TotalSalary += roundedAmountAsDouble;

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

                            if (existingInvoice.Sallary == null || Math.Abs(existingInvoice.Sallary.Value - roundedAmountAsDouble) > 0.01)
                            {
                                existingInvoice.Sallary = roundedAmountAsDouble;
                                shouldUpdate = true;
                            }

                            if (shouldUpdate)
                            {
                                existingInvoice.ModefiedAt = BusinessDateTime.UtcNow;
                                existingInvoice.ModefiedBy = createdBy;
                                result.UpdatedInvoices++;
                                hasChanges = true;
                            }

                            if (group.TotalMinutes <= 0m)
                            {
                                result.SkippedZeroValueInvoices++;
                            }
                        }
                        else
                        {
                            if (group.TotalMinutes <= 0m)
                            {
                                result.SkippedZeroValueInvoices++;
                                continue;
                            }

                            var invoice = new TeacherSallary
                            {
                                TeacherId = group.TeacherId,
                                Month = monthStart,
                                Sallary = roundedAmountAsDouble,
                                CreatedAt = BusinessDateTime.UtcNow,
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
                finally
                {
                    generationLock.Release();
                }
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
                if (month.HasValue)
                {
                    await GenerateMonthlyInvoicesAsync(month, requesterUserId);
                }

                var scopeResponse = await ResolveSalaryAccessScopeAsync(requesterUserId);
                if (!scopeResponse.IsSuccess || scopeResponse.Data == null)
                {
                    return response.AppendErrors(scopeResponse.Errors);
                }

                var query = ApplyInvoiceScope(
                    FilterActiveInvoices(_teacherSallaryRepository
                        .Where(x => x.TeacherId != null)),
                    scopeResponse.Data);

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

                query = SelectCanonicalInvoices(query);

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
                DateTime reference = month ?? BusinessDateTime.CairoNow;
                DateTime monthStart = new(reference.Year, reference.Month, 1);

                if (!month.HasValue)

                {
                    return response.CreateResponse(MessageCodes.Failed);
                }

                await GenerateMonthlyInvoicesAsync(monthStart, requesterUserId);

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
                        SectionBreakdown = BuildSectionBreakdown(Array.Empty<(string? SectionName, decimal Minutes, decimal Salary)>()),
                        Invoice = null
                    });
                }

                var (monthStartUtc, monthEndUtc) = BusinessDateTime.GetCairoMonthRangeUtc(monthStart.Year, monthStart.Month);
                await BackfillMissingSalarySnapshotsAsync(monthStartUtc, monthEndUtc, teacherId);

                var teacherRecords = await _circleReportRepository
                    .Where(report =>
                        report.TeacherId.HasValue &&
                        activeTeacherIds.Contains(report.TeacherId.Value) &&
                        !report.IsDeleted &&
                        report.CreationTime >= monthStartUtc &&
                        report.CreationTime < monthEndUtc)
                    .Select(report => new
                    {
                        RawMinutes = report.Minutes,
                        SnapshotMinutes = report.TeacherSalaryMinutes,
                        SnapshotSalary = report.TeacherSalaryAmount,
                        SectionName = report.Student != null
                            ? report.Student.StudentSubscribes
                                .Where(s =>
                                    s.CreatedAt.HasValue &&
                                    s.CreatedAt.Value >= monthStartUtc &&
                                    s.CreatedAt.Value < monthEndUtc)
                                .OrderByDescending(s => s.CreatedAt ?? DateTime.MinValue)
                                .ThenByDescending(s => s.Id)
                                .Select(s => s.StudentSubscribeType != null ? s.StudentSubscribeType.Name : null)
                                .FirstOrDefault()
                            : null,
                        FallbackSectionName = report.Student != null
                            ? report.Student.StudentSubscribes
                                .OrderByDescending(s => s.CreatedAt ?? DateTime.MinValue)
                                .ThenByDescending(s => s.Id)
                                .Select(s => s.StudentSubscribeType != null ? s.StudentSubscribeType.Name : null)
                                .FirstOrDefault()
                            : null,
                        HourPrice = report.Student != null
                            ? report.Student.StudentSubscribes
                                .Where(s =>
                                    s.CreatedAt.HasValue &&
                                    s.CreatedAt.Value >= monthStartUtc &&
                                    s.CreatedAt.Value < monthEndUtc)
                                .OrderByDescending(s => s.CreatedAt ?? DateTime.MinValue)
                                .ThenByDescending(s => s.Id)
                                .Select(s => s.StudentSubscribeType != null ? s.StudentSubscribeType.HourPrice : null)
                                .FirstOrDefault()
                            : null,
                        FallbackHourPrice = report.Student != null
                            ? report.Student.StudentSubscribes
                                .OrderByDescending(s => s.CreatedAt ?? DateTime.MinValue)
                                .ThenByDescending(s => s.Id)
                                .Select(s => s.StudentSubscribeType != null ? s.StudentSubscribeType.HourPrice : null)
                                .FirstOrDefault()
                            : null,
                        AttendStatusId = report.AttendStatueId
                    })
                    .ToListAsync();

                var normalizedTeacherRecords = teacherRecords
                    .Select(record => new
                    {
                        Minutes = ResolveSalaryMinutes(record.AttendStatusId, record.RawMinutes, record.SnapshotMinutes),
                        Salary = ResolveSalaryAmount(
                            record.AttendStatusId,
                            record.RawMinutes,
                            record.SnapshotMinutes,
                            record.SnapshotSalary,
                            record.HourPrice ?? record.FallbackHourPrice),
                        record.AttendStatusId,
                        SectionName = ResolveTeachingSectionName(record.SectionName ?? record.FallbackSectionName)
                    })
                    .ToList();

                var totalSalary = (double)Math.Round(normalizedTeacherRecords.Sum(r => r.Salary), 2, MidpointRounding.AwayFromZero);

                var aggregateSummary = new TeacherMonthlySummaryDto
                {
                    TeacherId = 0,
                    TeacherName = null,
                    Month = monthStart,
                    TotalReports = normalizedTeacherRecords.Count,
                    TotalMinutes = normalizedTeacherRecords.Sum(r => (double)r.Minutes),
                    PresentCount = normalizedTeacherRecords.Count(r => r.AttendStatusId == AttendStatusPresent),
                    AbsentWithExcuseCount = normalizedTeacherRecords.Count(r => r.AttendStatusId == AttendStatusAbsentWithExcuse),
                    AbsentWithoutExcuseCount = normalizedTeacherRecords.Count(r => r.AttendStatusId == AttendStatusAbsentWithoutExcuse),
                    TotalSalary = totalSalary,
                SectionBreakdown = BuildSectionBreakdown(
                    normalizedTeacherRecords.Select(record => ((string?)record.SectionName, record.Minutes, record.Salary))),
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
                await GenerateMonthlyInvoicesAsync(monthStart, requesterUserId);
                var (monthStartUtc, monthEndUtc) = BusinessDateTime.GetCairoMonthRangeUtc(monthStart.Year, monthStart.Month);
                await BackfillMissingSalarySnapshotsAsync(monthStartUtc, monthEndUtc, teacherId);

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

                var records = await _circleReportRepository
                    .Where(report =>
                        report.TeacherId == teacherId &&
                        !report.IsDeleted &&
                        report.CreationTime >= monthStartUtc &&
                        report.CreationTime < monthEndUtc)
                    .Select(report => new
                    {
                        Id = report.Id,
                        TeacherId = report.TeacherId ?? 0,
                        TeacherName = report.Teacher != null ? report.Teacher.FullName : null,
                        CircleReportId = report.Id,
                        CircleId = report.CircleId,
                        StudentId = report.StudentId,
                        StudentName = report.Student != null ? report.Student.FullName : null,
                        SectionName = report.Student != null
                            ? report.Student.StudentSubscribes
                                .Where(s =>
                                    s.CreatedAt.HasValue &&
                                    s.CreatedAt.Value >= monthStartUtc &&
                                    s.CreatedAt.Value < monthEndUtc)
                                .OrderByDescending(s => s.CreatedAt ?? DateTime.MinValue)
                                .ThenByDescending(s => s.Id)
                                .Select(s => s.StudentSubscribeType != null ? s.StudentSubscribeType.Name : null)
                                .FirstOrDefault()
                            : null,
                        FallbackSectionName = report.Student != null
                            ? report.Student.StudentSubscribes
                                .OrderByDescending(s => s.CreatedAt ?? DateTime.MinValue)
                                .ThenByDescending(s => s.Id)
                                .Select(s => s.StudentSubscribeType != null ? s.StudentSubscribeType.Name : null)
                                .FirstOrDefault()
                            : null,
                        RawMinutes = report.Minutes,
                        SnapshotMinutes = report.TeacherSalaryMinutes,
                        SnapshotSalary = report.TeacherSalaryAmount,
                        HourPrice = report.Student != null
                            ? report.Student.StudentSubscribes
                                .Where(s =>
                                    s.CreatedAt.HasValue &&
                                    s.CreatedAt.Value >= monthStartUtc &&
                                    s.CreatedAt.Value < monthEndUtc)
                                .OrderByDescending(s => s.CreatedAt ?? DateTime.MinValue)
                                .ThenByDescending(s => s.Id)
                                .Select(s => s.StudentSubscribeType != null ? s.StudentSubscribeType.HourPrice : null)
                                .FirstOrDefault()
                            : null,
                        FallbackHourPrice = report.Student != null
                            ? report.Student.StudentSubscribes
                                .OrderByDescending(s => s.CreatedAt ?? DateTime.MinValue)
                                .ThenByDescending(s => s.Id)
                                .Select(s => s.StudentSubscribeType != null ? s.StudentSubscribeType.HourPrice : null)
                                .FirstOrDefault()
                            : null,
                        AttendStatusId = report.AttendStatueId,
                        RecordCreatedAt = report.CreationTime,
                        CircleReportCreatedAt = report.CreationTime
                    })
                    .ToListAsync();

                var normalizedRecords = records
                    .Select(record => new TeacherMonthlyReportRecordDto
                    {
                        Id = record.Id,
                        TeacherId = record.TeacherId,
                        TeacherName = record.TeacherName,
                        CircleReportId = record.CircleReportId,
                        CircleId = record.CircleId,
                        StudentId = record.StudentId,
                        StudentName = record.StudentName,
                        SectionName = ResolveTeachingSectionName(record.SectionName ?? record.FallbackSectionName),
                        Minutes = (double)ResolveSalaryMinutes(record.AttendStatusId, record.RawMinutes, record.SnapshotMinutes),
                        Salary = (double)ResolveSalaryAmount(
                            record.AttendStatusId,
                            record.RawMinutes,
                            record.SnapshotMinutes,
                            record.SnapshotSalary,
                            record.HourPrice ?? record.FallbackHourPrice),
                        AttendStatusId = record.AttendStatusId,
                        RecordCreatedAt = record.RecordCreatedAt,
                        CircleReportCreatedAt = record.CircleReportCreatedAt
                    })
                    .OrderByDescending(record => record.RecordCreatedAt)
                    .ThenByDescending(record => record.Id)
                    .ToList();

                return response.CreateResponse(normalizedRecords);
            }
            catch (Exception ex)
            {
                return response.CreateResponse(ex);
            }
        }

        private static bool CountsForBilling(int? attendStatusId)
        {
            return attendStatusId == AttendStatusPresent || attendStatusId == AttendStatusAbsentWithoutExcuse;
        }

        private static decimal ResolveSalaryMinutes(int? attendStatusId, double? rawMinutes, decimal? snapshotMinutes)
        {
            if (snapshotMinutes.HasValue)
            {
                return snapshotMinutes.Value;
            }

            if (!CountsForBilling(attendStatusId) || !rawMinutes.HasValue)
            {
                return 0m;
            }

            return Math.Round(Convert.ToDecimal(rawMinutes.Value), 2, MidpointRounding.AwayFromZero);
        }

        private static decimal ResolveSalaryAmount(
            int? attendStatusId,
            double? rawMinutes,
            decimal? snapshotMinutes,
            decimal? snapshotSalary,
            decimal? hourPrice)
        {
            if (snapshotSalary.HasValue)
            {
                return snapshotSalary.Value;
            }

            var minutes = ResolveSalaryMinutes(attendStatusId, rawMinutes, snapshotMinutes);
            if (minutes <= 0m || !hourPrice.HasValue)
            {
                return 0m;
            }

            return Math.Round((hourPrice.Value / 60m) * minutes, 2, MidpointRounding.AwayFromZero);
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

                var invoiceData = await ApplyInvoiceScope(
                        FilterActiveInvoices(_teacherSallaryRepository
                            .Where(invoice => invoice.Id == invoiceId)),
                        scopeResponse.Data)

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
                var invoice = await FilterActiveInvoices(_teacherSallaryRepository
                    .Where(i => i.Id == invoiceId))

                    .Include(i => i.Teacher)
                    .FirstOrDefaultAsync();

                if (invoice == null)
                {
                    return response.CreateResponse(MessageCodes.NotFound);
                }

                invoice.IsPayed = dto.IsPayed;
                invoice.PayedAt = dto.IsPayed
                    ? dto.PayedAt.HasValue ? BusinessDateTime.NormalizeClientDateTimeToUtc(dto.PayedAt.Value) : BusinessDateTime.UtcNow
                    : null;
                invoice.ModefiedAt = BusinessDateTime.UtcNow;
                invoice.ModefiedBy = userId;

                await _unitOfWork.CommitAsync();

                var updated = await ProjectInvoices(
                        FilterActiveInvoices(_teacherSallaryRepository.Where(i => i.Id == invoice.Id)))

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
                if (invoice == null || IsInvoiceDeleted(invoice))
                {
                    return response.CreateResponse(MessageCodes.NotFound);
                }

                invoice.ModefiedBy = userId;
                invoice.ModefiedAt = BusinessDateTime.UtcNow;

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
                        invoice.PayedAt = dto.PayStatue.Value ? BusinessDateTime.UtcNow : null;
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

                var invoice = await ApplyInvoiceScope(
                        FilterActiveInvoices(_teacherSallaryRepository.Where(i => i.Id == invoiceId)),
                        scopeResponse.Data)
                    .FirstOrDefaultAsync();
                if (invoice == null)
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
                        ApplyInvoiceScope(
                            FilterActiveInvoices(_teacherSallaryRepository.Where(invoice => invoice.Id == invoiceId)),
                            scopeResponse.Data))

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
            await GenerateMonthlyInvoicesAsync(monthStart, requesterUserId);
            var (monthStartUtc, monthEndUtc) = BusinessDateTime.GetCairoMonthRangeUtc(monthStart.Year, monthStart.Month);
            await BackfillMissingSalarySnapshotsAsync(monthStartUtc, monthEndUtc, teacherId);

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

                var teacherRecords = await _circleReportRepository
                    .Where(report =>
                        report.TeacherId == teacherId &&
                        !report.IsDeleted &&
                        report.CreationTime >= monthStartUtc &&
                        report.CreationTime < monthEndUtc)
                    .Select(report => new
                    {
                        RawMinutes = report.Minutes,
                        SnapshotMinutes = report.TeacherSalaryMinutes,
                        SnapshotSalary = report.TeacherSalaryAmount,
                        SectionName = report.Student != null
                            ? report.Student.StudentSubscribes
                                .Where(s =>
                                    s.CreatedAt.HasValue &&
                                    s.CreatedAt.Value >= monthStartUtc &&
                                    s.CreatedAt.Value < monthEndUtc)
                                .OrderByDescending(s => s.CreatedAt ?? DateTime.MinValue)
                                .ThenByDescending(s => s.Id)
                                .Select(s => s.StudentSubscribeType != null ? s.StudentSubscribeType.Name : null)
                                .FirstOrDefault()
                            : null,
                        FallbackSectionName = report.Student != null
                            ? report.Student.StudentSubscribes
                                .OrderByDescending(s => s.CreatedAt ?? DateTime.MinValue)
                                .ThenByDescending(s => s.Id)
                                .Select(s => s.StudentSubscribeType != null ? s.StudentSubscribeType.Name : null)
                                .FirstOrDefault()
                            : null,
                        HourPrice = report.Student != null
                            ? report.Student.StudentSubscribes
                                .Where(s =>
                                    s.CreatedAt.HasValue &&
                                    s.CreatedAt.Value >= monthStartUtc &&
                                    s.CreatedAt.Value < monthEndUtc)
                                .OrderByDescending(s => s.CreatedAt ?? DateTime.MinValue)
                                .ThenByDescending(s => s.Id)
                                .Select(s => s.StudentSubscribeType != null ? s.StudentSubscribeType.HourPrice : null)
                                .FirstOrDefault()
                            : null,
                        FallbackHourPrice = report.Student != null
                            ? report.Student.StudentSubscribes
                                .OrderByDescending(s => s.CreatedAt ?? DateTime.MinValue)
                                .ThenByDescending(s => s.Id)
                                .Select(s => s.StudentSubscribeType != null ? s.StudentSubscribeType.HourPrice : null)
                                .FirstOrDefault()
                            : null,
                        AttendStatusId = report.AttendStatueId
                    })
                    .ToListAsync();

            var normalizedTeacherRecords = teacherRecords
                .Select(record => new
                {
                    Minutes = ResolveSalaryMinutes(record.AttendStatusId, record.RawMinutes, record.SnapshotMinutes),
                    Salary = ResolveSalaryAmount(
                        record.AttendStatusId,
                        record.RawMinutes,
                        record.SnapshotMinutes,
                        record.SnapshotSalary,
                        record.HourPrice ?? record.FallbackHourPrice),
                    record.AttendStatusId,
                    SectionName = ResolveTeachingSectionName(record.SectionName ?? record.FallbackSectionName)
                })
                .ToList();

            int totalReports = normalizedTeacherRecords.Count;
            double totalMinutes = normalizedTeacherRecords.Sum(r => (double)r.Minutes);
            double totalSalary = (double)Math.Round(normalizedTeacherRecords.Sum(r => r.Salary), 2, MidpointRounding.AwayFromZero);

            int presentCount = normalizedTeacherRecords.Count(r => r.AttendStatusId == AttendStatusPresent);
            int absentWithExcuseCount = normalizedTeacherRecords.Count(r => r.AttendStatusId == AttendStatusAbsentWithExcuse);
            int absentWithoutExcuseCount = normalizedTeacherRecords.Count(r => r.AttendStatusId == AttendStatusAbsentWithoutExcuse);

            var invoice = await ProjectInvoices(
                    SelectCanonicalInvoices(
                        ApplyInvoiceScope(
                            FilterActiveInvoices(_teacherSallaryRepository.Where(invoice =>
                                invoice.TeacherId == teacherId &&
                                invoice.Month.HasValue &&
                                invoice.Month.Value.Year == monthStart.Year &&
                                invoice.Month.Value.Month == monthStart.Month)),
                            scopeResponse.Data)))
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
                SectionBreakdown = BuildSectionBreakdown(
                    normalizedTeacherRecords.Select(record => ((string?)record.SectionName, record.Minutes, record.Salary))),
                Invoice = invoice
            };
        }

        private static List<TeacherSalarySectionBreakdownDto> BuildSectionBreakdown(
            IEnumerable<(string? SectionName, decimal Minutes, decimal Salary)> records)
        {
            var grouped = records
                .GroupBy(record => ResolveTeachingSectionName(record.SectionName))
                .ToDictionary(
                    group => group.Key,
                    group => new
                    {
                        Minutes = group.Sum(item => item.Minutes),
                        Salary = group.Sum(item => item.Salary)
                    });

            var results = new List<TeacherSalarySectionBreakdownDto>();
            foreach (var knownSection in GetOrderedSectionNames())
            {
                grouped.TryGetValue(knownSection, out var totals);
                results.Add(CreateSectionBreakdown(knownSection, totals?.Minutes ?? 0m, totals?.Salary ?? 0m));
            }

            foreach (var extraSection in grouped.Keys
                .Except(GetOrderedSectionNames(), StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal))
            {
                var totals = grouped[extraSection];
                results.Add(CreateSectionBreakdown(extraSection, totals.Minutes, totals.Salary));
            }

            return results;
        }

        private static TeacherSalarySectionBreakdownDto CreateSectionBreakdown(string sectionName, decimal totalMinutes, decimal totalSalary)
        {
            var roundedMinutes = Math.Round(totalMinutes, 2, MidpointRounding.AwayFromZero);
            var roundedHours = Math.Round(roundedMinutes / 60m, 2, MidpointRounding.AwayFromZero);
            var roundedSalary = Math.Round(totalSalary, 2, MidpointRounding.AwayFromZero);

            return new TeacherSalarySectionBreakdownDto
            {
                SectionName = sectionName,
                TotalMinutes = (double)roundedMinutes,
                TotalHours = (double)roundedHours,
                TotalSalary = (double)roundedSalary
            };
        }

        private static string ResolveTeachingSectionName(string? sectionName)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
            {
                return OtherSectionName;
            }

            var normalized = sectionName.Trim();
            if (normalized.IndexOf(FoundationSectionName, StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("foundation", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return FoundationSectionName;
            }

            if (normalized.IndexOf(RepetitionSectionName, StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("repeat", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("repetition", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return RepetitionSectionName;
            }

            if (normalized.IndexOf(MemorizationSectionName, StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("memor", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return MemorizationSectionName;
            }

            return normalized;
        }

        private static IReadOnlyList<string> GetOrderedSectionNames()
        {
            return new[]
            {
                FoundationSectionName,
                RepetitionSectionName,
                MemorizationSectionName,
                OtherSectionName
            };
        }

        private async Task BackfillMissingSalarySnapshotsAsync(DateTime monthStartUtc, DateTime monthEndUtc, int? teacherId = null)
        {
            var businessMonthStart = BusinessDateTime.ToCairo(monthStartUtc);

            var paidTeacherIds = await FilterActiveInvoices(_teacherSallaryRepository
                .Where(invoice =>
                    invoice.TeacherId.HasValue &&
                    invoice.IsPayed == true &&
                    invoice.Month.HasValue &&
                    invoice.Month.Value.Year == businessMonthStart.Year &&
                    invoice.Month.Value.Month == businessMonthStart.Month &&
                    (!teacherId.HasValue || invoice.TeacherId == teacherId.Value)))
                .Select(invoice => invoice.TeacherId!.Value)
                .Distinct()
                .ToListAsync();

            if (teacherId.HasValue && paidTeacherIds.Contains(teacherId.Value))
            {
                return;
            }

            var reportsQuery = _circleReportRepository
                .Where(report =>
                    report.TeacherId.HasValue &&
                    !report.IsDeleted &&
                    report.CreationTime >= monthStartUtc &&
                    report.CreationTime < monthEndUtc &&
                    (!teacherId.HasValue || report.TeacherId == teacherId.Value) &&
                    !paidTeacherIds.Contains(report.TeacherId.Value) &&
                    (!report.TeacherSalaryMinutes.HasValue || !report.TeacherSalaryAmount.HasValue))
                .Include(report => report.Student)
                    .ThenInclude(student => student.StudentSubscribes)
                        .ThenInclude(subscribe => subscribe.StudentSubscribeType);

            var reportsToBackfill = await reportsQuery.ToListAsync();
            if (reportsToBackfill.Count == 0)
            {
                return;
            }

            foreach (var report in reportsToBackfill)
            {
                var subscribeType = report.Student?.StudentSubscribes?
                    .Where(s =>
                        s.CreatedAt.HasValue &&
                        s.CreatedAt.Value >= monthStartUtc &&
                        s.CreatedAt.Value < monthEndUtc)
                    .OrderByDescending(s => s.CreatedAt ?? DateTime.MinValue)
                    .ThenByDescending(s => s.Id)
                    .Select(s => s.StudentSubscribeType)
                    .FirstOrDefault()
                    ?? report.Student?.StudentSubscribes?
                    .OrderByDescending(s => s.CreatedAt ?? DateTime.MinValue)
                    .ThenByDescending(s => s.Id)
                    .Select(s => s.StudentSubscribeType)
                    .FirstOrDefault();

                report.TeacherSalaryMinutes = ResolveSalaryMinutes(report.AttendStatueId, report.Minutes, report.TeacherSalaryMinutes);
                report.TeacherSalaryAmount = ResolveSalaryAmount(
                    report.AttendStatueId,
                    report.Minutes,
                    report.TeacherSalaryMinutes,
                    report.TeacherSalaryAmount,
                    subscribeType?.HourPrice);
            }

            await _unitOfWork.CommitAsync();
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

        private static IQueryable<TeacherSallary> FilterActiveInvoices(IQueryable<TeacherSallary> query)
        {
            return query.Where(invoice => !EF.Property<bool>(invoice, nameof(EntityBase.IsDeleted)));
        }

        private static bool IsInvoiceDeleted(TeacherSallary invoice)
        {
            return ((EntityBase)invoice).IsDeleted;
        }

        private static IQueryable<TeacherSallary> SelectCanonicalInvoices(IQueryable<TeacherSallary> query)
        {
            var activeQuery = FilterActiveInvoices(query);

            return activeQuery.Where(invoice => !activeQuery.Any(other =>
                other.Id != invoice.Id &&
                other.TeacherId == invoice.TeacherId &&
                (
                    (!other.Month.HasValue && !invoice.Month.HasValue) ||
                    (other.Month.HasValue &&
                     invoice.Month.HasValue &&
                     other.Month.Value.Year == invoice.Month.Value.Year &&
                     other.Month.Value.Month == invoice.Month.Value.Month)
                ) &&
                (
                    ((other.IsPayed == true) && (invoice.IsPayed != true)) ||
                    ((other.IsPayed == true) == (invoice.IsPayed == true) &&
                     (
                         (other.ModefiedAt ?? other.CreatedAt ?? DateTime.MinValue) >
                         (invoice.ModefiedAt ?? invoice.CreatedAt ?? DateTime.MinValue) ||
                         ((other.ModefiedAt ?? other.CreatedAt ?? DateTime.MinValue) ==
                          (invoice.ModefiedAt ?? invoice.CreatedAt ?? DateTime.MinValue) &&
                          other.Id > invoice.Id)
                     ))
                )));
        }

        private static TeacherSallary SelectCanonicalInvoice(IEnumerable<TeacherSallary> invoices, double? expectedSalary = null)
        {
            var activeInvoices = invoices
                .Where(invoice => !IsInvoiceDeleted(invoice))
                .ToList();

            var candidateInvoices = activeInvoices.Count > 0 ? activeInvoices : invoices.ToList();

            return candidateInvoices
                .OrderByDescending(invoice => invoice.IsPayed == true)
                .ThenByDescending(invoice =>
                    expectedSalary.HasValue &&
                    invoice.Sallary.HasValue &&
                    Math.Abs(invoice.Sallary.Value - expectedSalary.Value) <= 0.01d)
                .ThenByDescending(invoice => invoice.ModefiedAt ?? invoice.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(invoice => invoice.Id)
                .First();
        }

        private static bool TryMarkInvoiceAsDeleted(TeacherSallary invoice, int? modifiedBy)
        {
            if (IsInvoiceDeleted(invoice))
            {
                return false;
            }

            var baseInvoice = (EntityBase)invoice;
            baseInvoice.IsDeleted = true;
            invoice.IsDeleted = true;
            invoice.ModefiedAt = BusinessDateTime.UtcNow;
            invoice.ModefiedBy = modifiedBy;
            return true;
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
                TeacherMobile = invoice.Teacher != null ? invoice.Teacher.Mobile : null,
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
