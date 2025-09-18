using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO.TeacherSallaryDtos;
using Orbits.GeneralProject.Repositroy.Base;

namespace Orbits.GeneralProject.BLL.TeacherSallaryService
{
    public class TeacherSallaryBLL : BaseBLL, ITeacherSallaryBLL
    {
        private readonly IRepository<TeacherReportRecord> _teacherReportRepository;
        private readonly IRepository<TeacherSallary> _teacherSallaryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public TeacherSallaryBLL(
            IMapper mapper,
            IRepository<TeacherReportRecord> teacherReportRepository,
            IRepository<TeacherSallary> teacherSallaryRepository,
            IUnitOfWork unitOfWork) : base(mapper)
        {
            _teacherReportRepository = teacherReportRepository;
            _teacherSallaryRepository = teacherSallaryRepository;
            _unitOfWork = unitOfWork;
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
    }
}
