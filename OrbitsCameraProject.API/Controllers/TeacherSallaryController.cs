using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Orbits.GeneralProject.BLL.Constants;
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
        private readonly IHostEnvironment _hostEnvironment;

        public TeacherSallaryController(ITeacherSallaryBLL teacherSallaryBll, IHostEnvironment hostEnvironment)
        {
            _teacherSallaryBll = teacherSallaryBll;
            _hostEnvironment = hostEnvironment;
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
        /// Returns monthly summaries including attendance breakdown and salary totals.
        /// </summary>
        /// <param name="teacherId">Optional teacher identifier. When omitted summaries for all teachers are returned.</param>
        /// <param name="month">Optional month (the day component is ignored).</param>
        [HttpGet("MonthlySummary")]
        [ProducesResponseType(typeof(IResponse<IEnumerable<TeacherMonthlySummaryDto>>), 200)]
        public async Task<IActionResult> GetMonthlySummary([FromQuery] int? teacherId = null, [FromQuery] DateTime? month = null)
        {
            var result = await _teacherSallaryBll.GetMonthlySummaryAsync(teacherId, month);
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

        /// <summary>
        /// Returns the stored PDF receipt for a specific teacher salary payment.
        /// </summary>
        /// <param name="invoiceId">The invoice identifier.</param>
        [HttpGet("GetPaymentReceipt")]
        public async Task<IActionResult> GetPaymentReceipt([FromQuery] int invoiceId)
        {
            var receiptResponse = await _teacherSallaryBll.GetPaymentReceiptPathAsync(invoiceId);

            if (!receiptResponse.IsSuccess)
            {
                return MapError(receiptResponse);
            }

            if (string.IsNullOrWhiteSpace(receiptResponse.Data))
            {
                return NotFound();
            }

            try
            {
                var rootPath = Path.GetFullPath(_hostEnvironment.ContentRootPath);
                if (!rootPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                {
                    rootPath += Path.DirectorySeparatorChar;
                }

                var sanitizedRelativePath = receiptResponse.Data
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar)
                    .TrimStart(Path.DirectorySeparatorChar);

                var physicalPath = Path.GetFullPath(Path.Combine(rootPath, sanitizedRelativePath));

                if (!physicalPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(physicalPath))
                {
                    return NotFound();
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
                var fileName = Path.GetFileName(physicalPath);

                return File(fileBytes, "application/pdf", fileName);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "Failed to read the requested receipt file."
                });
            }
        }

        /// <summary>
        /// Updates teacher salary payment metadata and uploads the optional receipt file.
        /// </summary>
        /// <param name="dto">Multipart form payload.</param>
        [HttpPost("UpdatePayment")]
        [ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> UpdatePayment([FromForm] UpdateTeacherPaymentDto dto)
        {
            var result = await _teacherSallaryBll.UpdatePaymentAsync(dto, UserId);
            return Ok(result);
        }

        private IActionResult MapError<T>(IResponse<T> response)
        {
            if (response.IsSuccess)
            {
                return Ok(response);
            }

            var errorCode = response.Errors?.FirstOrDefault()?.Code;

            if (errorCode == MessageCodes.NotFound.StringValue())
            {
                return NotFound(response);
            }

            if (errorCode == MessageCodes.UnAuthorizedAccess.StringValue())
            {
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (errorCode == MessageCodes.Exception.StringValue())
            {
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }

            return BadRequest(response);
        }
    }
}
