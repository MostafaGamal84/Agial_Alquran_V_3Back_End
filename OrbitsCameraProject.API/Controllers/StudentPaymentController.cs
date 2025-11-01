using HandlebarsDotNet;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.StudentPaymentService;
using Orbits.GeneralProject.BLL.StudentSubscribeService;
using Orbits.GeneralProject.BLL.UserService;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.DTO;
using Orbits.GeneralProject.DTO.CircleDto;
using Orbits.GeneralProject.DTO.LockUpDtos;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.StudentPaymentDtos;
using Orbits.GeneralProject.DTO.StudentSubscribDtos;
using Orbits.GeneralProject.DTO.StudentSubscribDtos.StudentPaymentDtos;
using Orbits.GeneralProject.DTO.SubscribeDtos;
using Orbits.GeneralProject.DTO.UserDto;
using Orbits.GeneralProject.DTO.UserDtos;

namespace OrbitsProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentPaymentController : AppBaseController
    {
        //To do Injection for Services BLL
        private readonly IStudentPaymentBLL _studentPaymentBll;
        public StudentPaymentController(IStudentPaymentBLL studentPaymentBLL)
        {
            _studentPaymentBll = studentPaymentBLL;
        }

        //[HttpGet("GetPayment"), ProducesResponseType(typeof(IResponse<PagedResultDto<ViewStudentSubscribeReDto>>), 200)]
        //public async Task<IActionResult> GetPayment([FromQuery] FilteredResultRequestDto paginationFilterModel,int paymentId)
        //       => Ok(_studentPaymentBll.GetStudentPayment(paginationFilterModel, UserId, paymentId));

        /// <summary>
        /// Dashboard stats for the four cards (Paid / Unpaid / Overdue / Receivables).
        /// dataMonth = month to report; compareMonth = month to compare (defaults to previous month).
        /// </summary>
        /// <example>
        /// GET /api/StudentPayment/Dashboard?dataMonth=2025-09-01&amp;compareMonth=2025-08-01&amp;studentId=1120&amp;currencyId=1
        /// </example>
        [HttpGet("Dashboard")]
        [ProducesResponseType(typeof(PaymentsFullDashboardDto), 200)]
        public async Task<IActionResult> GetDashboard(
            [FromQuery] int? studentId,
            [FromQuery] int? currencyId,
            [FromQuery] DateTime? dataMonth,
            [FromQuery] DateTime? compareMonth)
        {
            var dto = await _studentPaymentBll.GetPaymentDashboardAsync(
                studentId, currencyId, dataMonth, compareMonth);

            return Ok(dto);
        }

        /// <summary>
        /// Paged table data filtered to a month (defaults to current month).
        /// Tabs: paid | unpaid | overdue (Overdue = unpaid with CreatedAt &lt; monthStart).
        /// </summary>
        /// <example>
        /// GET /api/StudentPayment/Invoices?month=2025-09-01&amp;tab=overdue&amp;SortBy=CreateDate&amp;SortDirection=DESC&amp;PageNumber=1&amp;PageSize=20
        /// </example>
        [HttpGet("Invoices")]
        [ProducesResponseType(typeof(IResponse<PagedResultDto<StudentPaymentReDto>>), 200)]
        public IActionResult GetInvoices(
            [FromQuery] FilteredResultRequestDto pagedDto,
            [FromQuery] string? tab,
            [FromQuery] int? studentId,
            [FromQuery] int? nationalityId,
            [FromQuery] DateTime? createdFrom,
            [FromQuery] DateTime? createdTo,
            [FromQuery] DateTime? dueFrom,
            [FromQuery] DateTime? dueTo,
            [FromQuery] DateTime? month)
        {
            // If you don't have a UserId property, you can extract it from claims like this:
            // var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

            var resp = _studentPaymentBll.GetStudentInvoices(
                pagedDto: pagedDto,
                userId: UserId,          // keep as-is if your base controller exposes UserId
                studentId: studentId,
                nationalityId: nationalityId,
                tab: tab,
                createdFrom: createdFrom,
                createdTo: createdTo,
                dueFrom: dueFrom,
                dueTo: dueTo,
                month: month);

            return Ok(resp);
        }

        [HttpGet("GetPayment"), ProducesResponseType(typeof(IResponse<StudentPaymentReDto>), 200)]
        public async Task<IActionResult> GetPayment(int paymentId)
            => Ok(await _studentPaymentBll.GetPayment(paymentId));



        [HttpPost("UpdatePayment"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> UpdatePayment([FromForm] UpdatePaymentDto model) => Ok(await _studentPaymentBll.UpdatePayment(model, UserId));
            
    }
}
