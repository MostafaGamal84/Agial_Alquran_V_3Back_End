using Microsoft.AspNetCore.Mvc;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.TeacherSallaryService;
using Orbits.GeneralProject.DTO.TeacherSallaryDtos;

namespace OrbitsProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeacherSallaryController : AppBaseController
    {
        private readonly ITeacherSallaryBLL _teacherSallaryBll;

        public TeacherSallaryController(ITeacherSallaryBLL teacherSallaryBll)
        {
            _teacherSallaryBll = teacherSallaryBll;
        }

        /// <summary>
        /// Generates teacher salary invoices for the specified month. If no month is provided
        /// the previous calendar month is used.
        /// </summary>
        /// <param name="month">Optional month (the day component is ignored).</param>
        [HttpPost("GenerateMonthly")]
        [ProducesResponseType(typeof(IResponse<TeacherSallaryGenerationResultDto>), 200)]
        public async Task<IActionResult> GenerateMonthly([FromQuery] DateTime? month = null)
        {
            var result = await _teacherSallaryBll.GenerateMonthlyInvoicesAsync(month, UserId);
            return Ok(result);
        }

        /// <summary>
        /// Returns salary invoices optionally filtered by month or teacher.
        /// </summary>
        /// <param name="month">Optional month filter.</param>
        /// <param name="teacherId">Optional teacher filter.</param>
        [HttpGet("Invoices")]
        [ProducesResponseType(typeof(IResponse<IEnumerable<TeacherInvoiceDto>>), 200)]
        public async Task<IActionResult> GetInvoices([FromQuery] DateTime? month = null, [FromQuery] int? teacherId = null)
        {
            var result = await _teacherSallaryBll.GetInvoicesAsync(month, teacherId);
            return Ok(result);
        }

        /// <summary>
        /// Returns a monthly summary for a teacher including attendance breakdown and salary totals.
        /// </summary>
        /// <param name="teacherId">Optional teacher identifier. Defaults to the authenticated user.</param>
        /// <param name="month">Optional month (the day component is ignored).</param>
        [HttpGet("MonthlySummary")]
        [ProducesResponseType(typeof(IResponse<TeacherMonthlySummaryDto>), 200)]
        public async Task<IActionResult> GetMonthlySummary([FromQuery] int? teacherId = null, [FromQuery] DateTime? month = null)
        {
            var targetTeacherId = teacherId ?? UserId;
            var result = await _teacherSallaryBll.GetMonthlySummaryAsync(targetTeacherId, month);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves an individual teacher salary invoice.
        /// </summary>
        /// <param name="invoiceId">The invoice identifier.</param>
        [HttpGet("Invoice/{invoiceId:int}")]
        [ProducesResponseType(typeof(IResponse<TeacherInvoiceDto>), 200)]
        public async Task<IActionResult> GetInvoice(int invoiceId)
        {
            var result = await _teacherSallaryBll.GetInvoiceByIdAsync(invoiceId);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves detailed information for a specific salary invoice.
        /// </summary>
        /// <param name="invoiceId">The invoice identifier.</param>
        [HttpGet("Invoice/{invoiceId:int}/Details")]
        [ProducesResponseType(typeof(IResponse<TeacherSallaryDetailsDto>), 200)]
        public async Task<IActionResult> GetInvoiceDetails(int invoiceId)
        {
            var result = await _teacherSallaryBll.GetInvoiceDetailsAsync(invoiceId);
            return Ok(result);
        }

        /// <summary>
        /// Updates the payment status of a salary invoice.
        /// </summary>
        /// <param name="invoiceId">The invoice identifier.</param>
        /// <param name="dto">Payload describing the desired status.</param>
        [HttpPut("Invoice/{invoiceId:int}/Status")]
        [ProducesResponseType(typeof(IResponse<TeacherInvoiceDto>), 200)]
        public async Task<IActionResult> UpdateInvoiceStatus(int invoiceId, [FromBody] UpdateTeacherSallaryStatusDto dto)
        {
            var result = await _teacherSallaryBll.UpdateInvoiceStatusAsync(invoiceId, dto, UserId);
            return Ok(result);
        }
    }
}
