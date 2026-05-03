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
        private readonly IRepository<ManagerStudent> _managerStudentRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileServiceBLL _fileService;



        public StudentPaymentBLL(IMapper mapper, IRepository<StudentPayment> studentPaymentRepo, IRepository<User> userRepo, IRepository<StudentSubscribe> studentSubscribeRepo, IRepository<Nationality> nationalityRepo, IUnitOfWork unitOfWork, IFileServiceBLL fileService, IRepository<ManagerStudent> managerStudentRepo) : base(mapper)
        {
            _mapper = mapper;
            _StudentPaymentRepo = studentPaymentRepo;
            _UserRepo = userRepo;
            _StudentSubscribeRepo = studentSubscribeRepo;
            _nationalityRepo = nationalityRepo;
            _managerStudentRepo = managerStudentRepo;
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

    var (monthStart, monthEnd) = month.HasValue
        ? BusinessDateTime.GetCairoMonthRangeUtc(month.Value.Year, month.Value.Month)
        : BusinessDateTime.GetCurrentCairoMonthRangeUtc();
    var createdFromUtc = createdFrom.HasValue ? BusinessDateTime.GetCairoDayRangeUtc(createdFrom.Value).StartUtc : (DateTime?)null;
    var createdToExclusiveUtc = createdTo.HasValue ? BusinessDateTime.GetCairoDayRangeUtc(createdTo.Value).EndUtc : (DateTime?)null;
    var dueFromUtc = dueFrom.HasValue ? BusinessDateTime.GetCairoDayRangeUtc(dueFrom.Value).StartUtc : (DateTime?)null;
    var dueToExclusiveUtc = dueTo.HasValue ? BusinessDateTime.GetCairoDayRangeUtc(dueTo.Value).EndUtc : (DateTime?)null;

    var sw = pagedDto?.SearchTerm?.Trim().ToLower();
    var residentGroup = ResidentGroupFilterHelper.Parse(pagedDto?.ResidentGroup);
    var residentIdsFilter = ResidentGroupFilterHelper.ResolveResidentIds(_nationalityRepo.GetAll(), residentGroup);
    bool applyResidentFilter = residentIdsFilter != null;
    tab = string.IsNullOrWhiteSpace(tab) ? null : tab.Trim().ToLower();
    var managerStudentsQuery = _managerStudentRepo.GetAll();

    // ONE EF-translatable predicate
    Expression<Func<StudentPayment, bool>> predicate = p =>
        // ----- skip payments linked to soft-deleted students
        (p.Student != null && p.Student.IsDeleted == false) &&

        // ----- role-based visibility (on the student)
        (me.UserTypeId != (int)UserTypesEnum.BranchLeader || (p.Student != null && p.Student.BranchId == me.BranchId)) &&
        (me.UserTypeId != (int)UserTypesEnum.Manager      || managerStudentsQuery.Any(ms => ms.ManagerId == me.Id && ms.StudentId == p.StudentId)) &&
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
        (!createdFromUtc.HasValue || (p.CreatedAt.HasValue && p.CreatedAt.Value >= createdFromUtc.Value)) &&
        (!createdToExclusiveUtc.HasValue   || (p.CreatedAt.HasValue && p.CreatedAt.Value <  createdToExclusiveUtc.Value)) &&

        // ----- due date range (optional)
        (!dueFromUtc.HasValue || (p.PaymentDate.HasValue && p.PaymentDate.Value >= dueFromUtc.Value)) &&
        (!dueToExclusiveUtc.HasValue   || (p.PaymentDate.HasValue && p.PaymentDate.Value <  dueToExclusiveUtc.Value)) &&

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
            var now = BusinessDateTime.UtcNow;
            var cairoNow = BusinessDateTime.CairoNow;
            var currentMonthReference = dataMonth.HasValue
                ? new DateTime(dataMonth.Value.Year, dataMonth.Value.Month, 1)
                : new DateTime(cairoNow.Year, cairoNow.Month, 1);
            var compareMonthReference = compareMonth.HasValue
                ? new DateTime(compareMonth.Value.Year, compareMonth.Value.Month, 1)
                : currentMonthReference.AddMonths(-1);

            // Resolve current month window
            var (curStart, curEnd) = BusinessDateTime.GetCairoMonthRangeUtc(currentMonthReference.Year, currentMonthReference.Month);

            // Resolve compare month window (default: previous month of current)
            var (cmpStart, cmpEnd) = BusinessDateTime.GetCairoMonthRangeUtc(compareMonthReference.Year, compareMonthReference.Month);

            IQueryable<StudentPayment> baseQ = _StudentPaymentRepo.GetAll(); // Preferably AsNoTracking() inside

            if (studentId.HasValue && studentId.Value > 0)
                baseQ = baseQ.Where(p => p.StudentId == studentId.Value);

            if (currencyId.HasValue && currencyId.Value > 0)
                baseQ = baseQ.Where(p => p.CurrencyId == currencyId.Value);

            static double Pct(decimal current, decimal previous)
                => previous == 0m
                    ? (current == 0m ? 0d : 100d)
                    : (double)Math.Round(((current - previous) / previous) * 100m, 1, MidpointRounding.AwayFromZero);

            // ---------- CURRENT (created inside cur month)
            var curAgg = await baseQ
                .Where(p => p.CreatedAt.HasValue && p.CreatedAt.Value >= curStart && p.CreatedAt.Value < curEnd)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    PaidAmt = g.Sum(p => p.PayStatue == true ? (p.Amount ?? 0m) : 0m),
                    UnpaidAmt = g.Sum(p => p.PayStatue != true && p.IsCancelled == false ? (p.Amount ?? 0m) : 0m),

                    PaidCnt = g.Count(p => p.PayStatue == true),
                    UnpaidCnt = g.Count(p => p.PayStatue != true && p.IsCancelled == false),
                })
                .FirstOrDefaultAsync();

            var cur = curAgg ?? new { PaidAmt = 0m, UnpaidAmt = 0m, PaidCnt = 0, UnpaidCnt = 0 };

            // Overdue for current: unpaid with CreatedAt < curStart (lives outside the month window by definition)
            var curOverdueAgg = await baseQ
                .Where(p => p.PayStatue != true && p.CreatedAt.HasValue && p.CreatedAt.Value < curStart)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    OverdueAmt = g.Sum(p => p.Amount ?? 0m),
                    OverdueCnt = g.Count()
                })
                .FirstOrDefaultAsync();

            var curOverdue = curOverdueAgg ?? new { OverdueAmt = 0m, OverdueCnt = 0 };

            // ---------- COMPARE (created inside cmp month)
            var cmpAgg = await baseQ
                .Where(p => p.CreatedAt.HasValue && p.CreatedAt.Value >= cmpStart && p.CreatedAt.Value < cmpEnd)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    PaidAmt = g.Sum(p => p.PayStatue == true ? (p.Amount ?? 0m) : 0m),
                    UnpaidAmt = g.Sum(p => p.PayStatue != true && p.IsCancelled == false ? (p.Amount ?? 0m) : 0m),
                })
                .FirstOrDefaultAsync();

            var cmp = cmpAgg ?? new { PaidAmt = 0m, UnpaidAmt = 0m };

            // Overdue for compare: unpaid with CreatedAt < cmpStart
            var cmpOverdueAgg = await baseQ
                .Where(p => p.PayStatue != true && p.CreatedAt.HasValue && p.CreatedAt.Value < cmpStart)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    OverdueAmt = g.Sum(p => p.Amount ?? 0m)
                })
                .FirstOrDefaultAsync();

            var cmpOverdue = cmpOverdueAgg ?? new { OverdueAmt = 0m };

            // ---------- Receivables (as of now, across all time)
            var recvAgg = await baseQ
                .Where(p => p.PayStatue != true)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Current = g.Sum(p => (p.PaymentDate.HasValue && p.PaymentDate.Value >= now) ? (p.Amount ?? 0m) : 0m),
                    Overdue = g.Sum(p => (p.PaymentDate.HasValue && p.PaymentDate.Value < now) ? (p.Amount ?? 0m) : 0m)
                })
                .FirstOrDefaultAsync();

            var recv = recvAgg ?? new { Current = 0m, Overdue = 0m };

            // total paid (all time)
            decimal totalPaidEver = await baseQ
                .Where(p => p.PayStatue == true)
                .Select(p => p.Amount ?? 0m)
                .SumAsync();

            var totalReceivables = recv.Current + recv.Overdue;
            var collectionRate = (totalPaidEver + totalReceivables) == 0m
                ? 0d
                : (double)Math.Round((totalPaidEver / (totalPaidEver + totalReceivables)) * 100m, 1, MidpointRounding.AwayFromZero);

            return new PaymentsFullDashboardDto
            {
                Month = currentMonthReference,

                // Amounts + counts for the month
                TotalPaid = (double)cur.PaidAmt,
                TotalPaidCount = cur.PaidCnt,
                TotalPaidMoMPercentage = Pct(cur.PaidAmt, cmp.PaidAmt),

                TotalUnPaid = (double)cur.UnpaidAmt,
                TotalUnPaidCount = cur.UnpaidCnt,
                TotalUnPaidMoMPercentage = Pct(cur.UnpaidAmt, cmp.UnpaidAmt),

                // Overdue is unpaid created before month start
                TotalOverdue = (double)curOverdue.OverdueAmt,
                TotalOverdueCount = curOverdue.OverdueCnt,
                TotalOverdueMoMPercentage = Pct(curOverdue.OverdueAmt, cmpOverdue.OverdueAmt),

                // Blue card (as-of-now)
                CurrentReceivables = (double)recv.Current,
                OverdueReceivables = (double)recv.Overdue,
                TotalReceivables = (double)totalReceivables,
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
            entity.ModefiedAt = BusinessDateTime.UtcNow;
            if (dto.PayStatue == true)
            {

                entity.Amount = dto.Amount.HasValue
                    ? Math.Round(dto.Amount.Value, 2, MidpointRounding.AwayFromZero)
                    : null;
                entity.ReceiptPath = dto.ReceiptPath!=null ? _fileService.CreateFileAsync(dto.ReceiptPath,"StudentInvoices/").Result.Data.FilePath : null;
                entity.PaymentDate = BusinessDateTime.UtcNow;
                // ✅ يحدث فقط لو فيها قيمة
                if (dto.CurrencyId.HasValue)
                {
                    entity.CurrencyId = dto.CurrencyId.Value;
                }
                entity.ModefiedBy = userId;
                entity.ModefiedAt = BusinessDateTime.UtcNow;
                entity.PayStatue = dto.PayStatue;
                entity.IsCancelled = false;
                studentSubscribe.ModefiedAt = BusinessDateTime.UtcNow;
                studentSubscribe.ModefiedBy = userId;
                studentSubscribe.PayStatus = dto.PayStatue;
            }

            if (dto.IsCancelled == true)
            {
                entity.IsCancelled = true;
                entity.PayStatue = false;
                entity.ModefiedBy = userId;
                entity.ModefiedAt = BusinessDateTime.UtcNow;
                //_StudentSubscribeRepo.Delete(studentSubscribe);
            }

            if (dto.PayStatue == false && dto.IsCancelled == false)
            {
                entity.IsCancelled = false;
                entity.PayStatue = false;
                entity.ModefiedBy = userId;
                entity.ModefiedAt = BusinessDateTime.UtcNow;
            }

            await _unitOfWork.CommitAsync();
            return output.CreateResponse(true);
        }

    }

}
