using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.TeacherSallaryService;
using Orbits.GeneralProject.DTO.TeacherSallaryDtos;

namespace OrbitsProject.API.Controllers
{
    /// <summary>
    /// REST surface serving the teacher salary administration experience.
    /// </summary>
    [ApiController]
    [Route("admin/teacher-salary")]
    [EnableRateLimiting("FixedPolicy")]
    public class AdminTeacherSalaryController : ControllerBase
    {
        private readonly ITeacherSallaryBLL _teacherSallaryBll;

        public AdminTeacherSalaryController(ITeacherSallaryBLL teacherSallaryBll)
        {
            _teacherSallaryBll = teacherSallaryBll;
        }

        /// <summary>
        /// Retrieves paged teacher salary invoices optionally filtered by month and teacher.
        /// </summary>
        /// <param name="month">Optional month filter (day component ignored).</param>
        /// <param name="teacherId">Optional teacher identifier.</param>
        [HttpGet("invoices")]
        [ProducesResponseType(typeof(IResponse<IEnumerable<TeacherInvoiceDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInvoices([FromQuery] DateTime? month = null, [FromQuery] int? teacherId = null)
        {
            var response = await _teacherSallaryBll.GetInvoicesAsync(month, teacherId);
            return Ok(response);
        }

        /// <summary>
        /// Retrieves enriched invoice details for a specific teacher salary invoice.
        /// </summary>
        /// <param name="invoiceId">The invoice identifier.</param>
        [HttpGet("invoices/{invoiceId:int}/details")]
        [ProducesResponseType(typeof(IResponse<TeacherSallaryDetailsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInvoiceDetails(int invoiceId)
        {
            var response = await _teacherSallaryBll.GetInvoiceDetailsAsync(invoiceId);
            return Ok(response);
        }

        /// <summary>
        /// Updates the payment status metadata for a teacher salary invoice.
        /// </summary>
        /// <param name="invoiceId">The invoice identifier.</param>
        /// <param name="dto">Payload describing the new payment status.</param>
        [HttpPatch("invoices/{invoiceId:int}/payment")]
        [ProducesResponseType(typeof(IResponse<TeacherInvoiceDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateInvoicePayment(int invoiceId, [FromBody] UpdateTeacherSallaryStatusDto dto)
        {
            var response = await _teacherSallaryBll.UpdateInvoiceStatusAsync(invoiceId, dto, UserId);
            return Ok(response);
        }

        /// <summary>
        /// Uploads (or replaces) the payment receipt for a teacher salary invoice while optionally
        /// updating payment metadata.
        /// </summary>
        /// <param name="invoiceId">The invoice identifier.</param>
        /// <param name="form">Multipart form payload containing the receipt and metadata.</param>
        [HttpPost("invoices/{invoiceId:int}/receipt")]
        [ProducesResponseType(typeof(IResponse<TeacherInvoiceDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UploadInvoiceReceipt(int invoiceId, [FromForm] TeacherSalaryReceiptUploadDto form)
        {
            if (form.InvoiceId.HasValue && form.InvoiceId.Value != invoiceId)
            {
                var mismatchResponse = new Response<TeacherInvoiceDto>();
                return Ok(mismatchResponse.AppendError(
                    MessageCodes.InputValidationError,
                    nameof(form.InvoiceId),
                    "The invoice id in the payload does not match the requested resource."));
            }

            var updateDto = new UpdateTeacherPaymentDto
            {
                Id = invoiceId,
                Amount = form.Amount,
                PayStatue = form.PayStatue,
                IsCancelled = form.IsCancelled,
                ReceiptPath = form.Receipt
            };

            var updateResponse = await _teacherSallaryBll.UpdatePaymentAsync(updateDto, UserId);
            if (!updateResponse.IsSuccess)
            {
                var failureResponse = new Response<TeacherInvoiceDto>();
                if (updateResponse.Errors != null && updateResponse.Errors.Count > 0)
                {
                    return Ok(failureResponse.AppendErrors(updateResponse.Errors));
                }

                return Ok(failureResponse.CreateResponse(MessageCodes.Failed, "Unable to update the invoice payment."));
            }

            var invoiceResponse = await _teacherSallaryBll.GetInvoiceByIdAsync(invoiceId);
            return Ok(invoiceResponse);
        }

        /// <summary>
        /// Returns monthly attendance and salary summaries.
        /// </summary>
        /// <param name="teacherId">Optional teacher identifier. When omitted summaries for all teachers are returned.</param>
        /// <param name="month">Optional month filter (day component ignored).</param>
        [HttpGet("monthly-summary")]
        [ProducesResponseType(typeof(IResponse<IEnumerable<TeacherMonthlySummaryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMonthlySummary([FromQuery] int? teacherId = null, [FromQuery] DateTime? month = null)
        {
            var response = await _teacherSallaryBll.GetMonthlySummaryAsync(teacherId, month);
            return Ok(response);
        }

        /// <summary>
        /// Generates teacher salary invoices for the requested month (or the previous month by default).
        /// </summary>
        /// <param name="month">Optional month filter (day component ignored).</param>
        [HttpPost("generate")]
        [ProducesResponseType(typeof(IResponse<TeacherSallaryGenerationResultDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateInvoices([FromQuery] DateTime? month = null)
        {
            var response = await _teacherSallaryBll.GenerateMonthlyInvoicesAsync(month, UserId);
            return Ok(response);
        }

        private int UserId
        {
            get
            {
                var raw = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(raw))
                {
                    throw new UnauthorizedAccessException("User id claim is missing from the token.");
                }

                if (!int.TryParse(raw, out var id))
                {
                    throw new UnauthorizedAccessException("User id claim is invalid (not a number).");
                }

                return id;
            }
        }
    }
}
