using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.FilesUploaderService;
using Orbits.GeneralProject.BLL.Helpers;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO.CircleDto;
using Orbits.GeneralProject.DTO.LockUpDtos;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.StudentPaymentDtos;
using Orbits.GeneralProject.DTO.StudentSubscribDtos;
using Orbits.GeneralProject.DTO.StudentSubscribDtos.StudentPaymentDtos;
using Orbits.GeneralProject.DTO.SubscribeDtos;
using Orbits.GeneralProject.Repositroy.Base;
using System.Linq.Expressions;

namespace Orbits.GeneralProject.BLL.StudentPaymentService
{
    public class StudentPaymentBLL : BaseBLL, IStudentPaymentBLL
    {
        private readonly IMapper _mapper;
        private readonly IRepository<StudentPayment> _StudentPaymentRepo;
        private readonly IRepository<StudentSubscribe> _StudentSubscribeRepo;
        private readonly IRepository<User> _UserRepo;
        private readonly IRepository<Nationality> _nationalityRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileServiceBLL _fileService;



        public StudentPaymentBLL(IMapper mapper, IRepository<StudentPayment> studentPaymentRepo, IRepository<User> userRepo, IRepository<StudentSubscribe> studentSubscribeRepo, IRepository<Nationality> nationalityRepo, IUnitOfWork unitOfWork, IFileServiceBLL fileService) : base(mapper)
        {
            _mapper = mapper;
            _StudentPaymentRepo = studentPaymentRepo;
            _UserRepo = userRepo;
            _StudentSubscribeRepo = studentSubscribeRepo;
            _nationalityRepo = nationalityRepo;
            _unitOfWork = unitOfWork;
            _fileService = fileService;
        }
        public IResponse<PagedResultDto<StudentPaymentReDto>> GetStudentInvoices(
    FilteredResultRequestDto pagedDto,
    int userId,
    int? studentId = null,
    int? nationalityId = null,
    string? tab = null,                 // "paid" | "unpaid" | "overdue" | "cancelled" | null/"all"
    DateTime? createdFrom = null,
    DateTime? createdTo   = null,
    DateTime? dueFrom     = null,
    DateTime? dueTo       = null,
    DateTime? month       = null)       // month filter for the table
{
    var output = new Response<PagedResultDto<StudentPaymentReDto>>();

    var me = _UserRepo.GetById(userId);
    if (me == null) return output.AppendError(MessageCodes.NotFound);

    var now = DateTime.Now;
    var monthStart = month.HasValue
        ? new DateTime(month.Value.Year, month.Value.Month, 1)
        : new DateTime(now.Year, now.Month, 1);
    var monthEnd = monthStart.AddMonths(1);

    var sw = pagedDto?.SearchTerm?.Trim().ToLower();
    var residentGroup = ResidentGroupFilterHelper.Parse(pagedDto?.ResidentGroup);
    var residentIdsFilter = ResidentGroupFilterHelper.ResolveResidentIds(_nationalityRepo.GetAll(), residentGroup);
    bool applyResidentFilter = residentIdsFilter != null;
    tab = string.IsNullOrWhiteSpace(tab) ? null : tab.Trim().ToLower();

    // ONE EF-translatable predicate
    Expression<Func<StudentPayment, bool>> predicate = p =>
        // ----- role-based visibility (on the student)
        (me.UserTypeId != (int)UserTypesEnum.BranchLeader || (p.Student != null && p.Student.BranchId == me.BranchId)) &&
        (me.UserTypeId != (int)UserTypesEnum.Manager      || (p.Student != null && p.Student.ManagerId == me.Id)) &&
        (me.UserTypeId != (int)UserTypesEnum.Teacher      || (p.Student != null && p.Student.TeacherId == me.Id)) &&

        // ----- specific student
        (!(studentId.HasValue && studentId.Value > 0) || p.StudentId == studentId.Value) &&
        (!(nationalityId.HasValue && nationalityId.Value > 0) || (p.Student != null && p.Student.NationalityId == nationalityId.Value)) &&
        (!applyResidentFilter || (p.Student != null && p.Student.ResidentId.HasValue && residentIdsFilter!.Contains(p.Student.ResidentId.Value))) &&

        // ----- month filter:
        // overdue: unpaid & created before month; cancelled: created inside month (like other tabs)
        (
            (tab == "overdue"   && p.PayStatue != true && (p.IsCancelled == null || p.IsCancelled == false) && p.CreatedAt.HasValue && p.CreatedAt.Value < monthStart) ||
            (tab == "cancelled" && p.IsCancelled == true && (!month.HasValue || (p.CreatedAt.HasValue && p.CreatedAt.Value >= monthStart && p.CreatedAt.Value < monthEnd))) ||
            ((tab != "overdue" && tab != "cancelled") && (!month.HasValue || (p.CreatedAt.HasValue && p.CreatedAt.Value >= monthStart && p.CreatedAt.Value < monthEnd)))
        ) &&

        // ----- tabs (additional constraints)
        (
            string.IsNullOrEmpty(tab) ||
            (tab == "paid"      && p.PayStatue == true  && (p.IsCancelled == null || p.IsCancelled == false)) ||
            (tab == "unpaid"    && p.PayStatue != true  && (p.IsCancelled == null || p.IsCancelled == false)) ||  // null treated as unpaid
            (tab == "overdue"   && p.PayStatue != true  && (p.IsCancelled == null || p.IsCancelled == false)) ||
            (tab == "cancelled" && p.IsCancelled == true)
        ) &&

        // ----- created date range (optional)
        (!createdFrom.HasValue || (p.CreatedAt.HasValue && p.CreatedAt.Value >= createdFrom.Value)) &&
        (!createdTo.HasValue   || (p.CreatedAt.HasValue && p.CreatedAt.Value <  createdTo.Value.AddDays(1))) &&

        // ----- due date range (optional)
        (!dueFrom.HasValue || (p.PaymentDate.HasValue && p.PaymentDate.Value >= dueFrom.Value)) &&
        (!dueTo.HasValue   || (p.PaymentDate.HasValue && p.PaymentDate.Value <  dueTo.Value.AddDays(1))) &&

        // ----- search
        (
            string.IsNullOrEmpty(sw) ||
            p.Id.ToString().Contains(sw) ||
            (p.Student != null &&
                ((p.Student.FullName != null && p.Student.FullName.ToLower().Contains(sw)) ||
                 (p.Student.Email    != null && p.Student.Email.ToLower().Contains(sw)) ||
                 (p.Student.Mobile   != null && p.Student.Mobile.ToLower().Contains(sw))))
        );

    // Use your generic pager; include Student for mapping/search
    var paged = GetPagedList<StudentPaymentReDto, StudentPayment, int>(
        pagedDto,
        _StudentPaymentRepo,
        x => x.Id,
        predicate,
        sortDirection: "DESC",
        disableFilter: true,
        excluededColumns: null,
        includeProperties: x => x.Student
    );

    // ---- Normalize flags & StatusText with month-based rule; do NOT override "Paid" or "Cancelled"
    if (paged.Items != null && paged.Items.Count > 0)
    {
        var list = paged.Items.ToList();
        foreach (var it in list)
        {
            if (string.Equals(it.StatusText, "Paid", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(it.StatusText, "Cancelled", StringComparison.OrdinalIgnoreCase))
                continue;

            it.StatusText = it.CreateDate < monthStart ? "Overdue" : "Unpaid";
        }
        paged.Items = list;
    }

    return output.CreateResponse(paged);
}
        public async Task<PaymentsFullDashboardDto> GetPaymentDashboardAsync(
    int? studentId = null,
    int? currencyId = null,
    DateTime? dataMonth = null,          // month to report
    DateTime? compareMonth = null)       // month to compare against
        {
            var now = DateTime.Now;

            // Resolve current month window
            var curStart = dataMonth.HasValue
                ? new DateTime(dataMonth.Value.Year, dataMonth.Value.Month, 1)
                : new DateTime(now.Year, now.Month, 1);
            var curEnd = curStart.AddMonths(1);

            // Resolve compare month window (default: previous month of current)
            var cmpStart = compareMonth.HasValue
                ? new DateTime(compareMonth.Value.Year, compareMonth.Value.Month, 1)
                : curStart.AddMonths(-1);
            var cmpEnd = cmpStart.AddMonths(1);

            IQueryable<StudentPayment> baseQ = _StudentPaymentRepo.GetAll(); // Preferably AsNoTracking() inside

            if (studentId.HasValue && studentId.Value > 0)
                baseQ = baseQ.Where(p => p.StudentId == studentId.Value);

            if (currencyId.HasValue && currencyId.Value > 0)
                baseQ = baseQ.Where(p => p.CurrencyId == currencyId.Value);

            static double Pct(double current, double previous)
                => previous == 0 ? (current == 0 ? 0 : 100)
                                 : Math.Round(((current - previous) / previous) * 100.0, 1);

            // ---------- CURRENT (created inside cur month)
            var curAgg = await baseQ
                .Where(p => p.CreatedAt.HasValue && p.CreatedAt.Value >= curStart && p.CreatedAt.Value < curEnd)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    PaidAmt = g.Sum(p => p.PayStatue == true ? (double?)(p.Amount ?? 0) : 0) ?? 0,
                    UnpaidAmt = g.Sum(p => p.PayStatue != true && p.IsCancelled == false ? (double?)(p.Amount ?? 0) : 0) ?? 0,  // treats null as unpaid

                    PaidCnt = g.Count(p => p.PayStatue == true),
                    UnpaidCnt = g.Count(p => p.PayStatue != true && p.IsCancelled == false),
                })
                .FirstOrDefaultAsync();

            var cur = curAgg ?? new { PaidAmt = 0d, UnpaidAmt = 0d, PaidCnt = 0, UnpaidCnt = 0 };

            // Overdue for current: unpaid with CreatedAt < curStart (lives outside the month window by definition)
            var curOverdueAgg = await baseQ
                .Where(p => p.PayStatue != true && p.CreatedAt.HasValue && p.CreatedAt.Value < curStart)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    OverdueAmt = g.Sum(p => (double?)(p.Amount ?? 0)) ?? 0,
                    OverdueCnt = g.Count()
                })
                .FirstOrDefaultAsync();

            var curOverdue = curOverdueAgg ?? new { OverdueAmt = 0d, OverdueCnt = 0 };

            // ---------- COMPARE (created inside cmp month)
            var cmpAgg = await baseQ
                .Where(p => p.CreatedAt.HasValue && p.CreatedAt.Value >= cmpStart && p.CreatedAt.Value < cmpEnd)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    PaidAmt = g.Sum(p => p.PayStatue == true ? (double?)(p.Amount ?? 0) : 0) ?? 0,
                    UnpaidAmt = g.Sum(p => p.PayStatue != true && p.IsCancelled == false ? (double?)(p.Amount ?? 0) : 0) ?? 0,
                })
                .FirstOrDefaultAsync();

            var cmp = cmpAgg ?? new { PaidAmt = 0d, UnpaidAmt = 0d };

            // Overdue for compare: unpaid with CreatedAt < cmpStart
            var cmpOverdueAgg = await baseQ
                .Where(p => p.PayStatue != true && p.CreatedAt.HasValue && p.CreatedAt.Value < cmpStart)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    OverdueAmt = g.Sum(p => (double?)(p.Amount ?? 0)) ?? 0
                })
                .FirstOrDefaultAsync();

            var cmpOverdue = cmpOverdueAgg ?? new { OverdueAmt = 0d };

            // ---------- Receivables (as of now, across all time)
            var recvAgg = await baseQ
                .Where(p => p.PayStatue != true)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Current = g.Sum(p => (p.PaymentDate.HasValue && p.PaymentDate.Value >= now) ? (double?)(p.Amount ?? 0) : 0) ?? 0,
                    Overdue = g.Sum(p => (p.PaymentDate.HasValue && p.PaymentDate.Value < now) ? (double?)(p.Amount ?? 0) : 0) ?? 0
                })
                .FirstOrDefaultAsync();

            var recv = recvAgg ?? new { Current = 0d, Overdue = 0d };

            // total paid (all time)
            double totalPaidEver = await baseQ
                .Where(p => p.PayStatue == true)
                .Select(p => (double?)(p.Amount ?? 0))
                .SumAsync() ?? 0d;

            var totalReceivables = recv.Current + recv.Overdue;
            var collectionRate = (totalPaidEver + totalReceivables) == 0
                ? 0
                : Math.Round((totalPaidEver / (totalPaidEver + totalReceivables)) * 100.0, 1);

            return new PaymentsFullDashboardDto
            {
                Month = curStart,

                // Amounts + counts for the month
                TotalPaid = cur.PaidAmt,
                TotalPaidCount = cur.PaidCnt,
                TotalPaidMoMPercentage = Pct(cur.PaidAmt, cmp.PaidAmt),

                TotalUnPaid = cur.UnpaidAmt,
                TotalUnPaidCount = cur.UnpaidCnt,
                TotalUnPaidMoMPercentage = Pct(cur.UnpaidAmt, cmp.UnpaidAmt),

                // Overdue is unpaid created before month start
                TotalOverdue = curOverdue.OverdueAmt,
                TotalOverdueCount = curOverdue.OverdueCnt,
                TotalOverdueMoMPercentage = Pct(curOverdue.OverdueAmt, cmpOverdue.OverdueAmt),

                // Blue card (as-of-now)
                CurrentReceivables = recv.Current,
                OverdueReceivables = recv.Overdue,
                TotalReceivables = totalReceivables,
                CollectionRate = collectionRate
            };
        }

        public async Task<IResponse<StudentPaymentReDto>> GetPayment(int id)
        {
            var output = new Response<StudentPaymentReDto>();
            StudentPayment entity = await _StudentPaymentRepo.GetByIdAsync(id);
            if (entity == null)
                return output.CreateResponse(MessageCodes.NotFound);
            return output.CreateResponse(_mapper.Map<StudentPaymentReDto>(entity));
        }

        public async Task<IResponse<bool>> UpdatePayment(UpdatePaymentDto dto, int userId)
        {
            Response<bool> output = new Response<bool>();

            StudentPayment entity = _StudentPaymentRepo.GetById(dto.Id);
            var studentSubscribe = _StudentSubscribeRepo.GetById(entity.StudentSubscribes.Where(x => x.StudentPaymentId == entity.Id).FirstOrDefault().Id);

            entity.ModefiedBy = userId;
            entity.ModefiedAt = DateTime.Now;
            if (dto.PayStatue == true)
            {

                entity.Amount = dto.Amount;
                entity.ReceiptPath = dto.ReceiptPath!=null ? _fileService.CreateFileAsync(dto.ReceiptPath,"StudentInvoices/").Result.Data.FilePath : null;
                entity.PaymentDate = DateTime.Now;
                entity.ModefiedBy = userId;
                entity.ModefiedAt = DateTime.Now;
                entity.PayStatue = dto.PayStatue;
                studentSubscribe.ModefiedAt = DateTime.Now;
                studentSubscribe.ModefiedBy = userId;
                studentSubscribe.PayStatus = dto.PayStatue;
            }

            if (dto.IsCancelled == true)
            {
                entity.IsCancelled = true;
                entity.PayStatue = false;
                entity.ModefiedBy = userId;
                entity.ModefiedAt = DateTime.Now;
                _StudentSubscribeRepo.Delete(studentSubscribe);
            }

            if (dto.PayStatue == false && dto.IsCancelled == false)
            {
                entity.IsCancelled = false;
                entity.PayStatue = false;
                entity.ModefiedBy = userId;
                entity.ModefiedAt = DateTime.Now;
            }

            await _unitOfWork.CommitAsync();
            return output.CreateResponse(true);
        }

    }

}
