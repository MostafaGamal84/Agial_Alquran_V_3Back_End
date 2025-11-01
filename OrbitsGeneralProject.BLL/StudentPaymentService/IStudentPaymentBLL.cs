using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.StudentPaymentDtos;
using Orbits.GeneralProject.DTO.StudentSubscribDtos.StudentPaymentDtos;
using Orbits.GeneralProject.DTO.SubscribeDtos;

namespace Orbits.GeneralProject.BLL.StudentPaymentService
{
    public interface IStudentPaymentBLL
    {

        Task<PaymentsFullDashboardDto> GetPaymentDashboardAsync(
  int? studentId = null,
  int? currencyId = null,
  DateTime? dataMonth = null,          // month to report
  DateTime? compareMonth = null);     // month to compare against
                                      //        IResponse<PagedResultDto<StudentPaymentReDto>> GetStudentPayment(FilteredResultRequestDto pagedDto, int userId, int? paymentId);
   IResponse<PagedResultDto<StudentPaymentReDto>> GetStudentInvoices(
   FilteredResultRequestDto pagedDto,
   int userId,
   int? studentId = null,
   int? nationalityId = null,
   string? tab = null,                 // "paid" | "unpaid" | "overdue" | null/"all"
   DateTime? createdFrom = null,
   DateTime? createdTo = null,
   DateTime? dueFrom = null,
   DateTime? dueTo = null,
   DateTime? month = null);


        Task<IResponse<StudentPaymentReDto>> GetPayment(int Id);
        Task<IResponse<bool>> UpdatePayment(UpdatePaymentDto dto, int userId);

    }
}
