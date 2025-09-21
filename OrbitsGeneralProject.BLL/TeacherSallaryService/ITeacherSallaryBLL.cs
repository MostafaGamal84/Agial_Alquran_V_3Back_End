using System;
using System.Collections.Generic;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO.TeacherSallaryDtos;

namespace Orbits.GeneralProject.BLL.TeacherSallaryService
{
    public interface ITeacherSallaryBLL
    {
        /// <summary>
        /// Generates (or refreshes) teacher salary invoices for the specified month.
        /// If no month is provided the previous calendar month is used.
        /// </summary>
        /// <param name="month">The month to generate invoices for. The day component is ignored.</param>
        /// <param name="createdBy">Optional user id used for audit fields.</param>
        /// <returns>A response describing how many invoices were created/updated.</returns>
        Task<IResponse<TeacherSallaryGenerationResultDto>> GenerateMonthlyInvoicesAsync(DateTime? month = null, int? createdBy = null);

        /// <summary>
        /// Calculates teacher activity summaries for a specific month including attendance breakdowns and salary totals.
        /// </summary>
        /// <param name="teacherId">Optional teacher identifier filter.</param>
        /// <param name="month">Optional month filter. When omitted the previous calendar month is used.</param>
        Task<IResponse<IEnumerable<TeacherMonthlySummaryDto>>> GetMonthlySummaryAsync(int? teacherId = null, DateTime? month = null);

        /// <summary>
        /// Returns salary invoices optionally filtered by teacher and/or month.
        /// </summary>
        /// <param name="month">Optional month filter. The day component is ignored.</param>
        /// <param name="teacherId">Optional teacher filter.</param>
        Task<IResponse<IEnumerable<TeacherInvoiceDto>>> GetInvoicesAsync(DateTime? month = null, int? teacherId = null);

        /// <summary>
        /// Retrieves a salary invoice along with contextual summary information.
        /// </summary>
        /// <param name="invoiceId">The invoice identifier.</param>
        Task<IResponse<TeacherSallaryDetailsDto>> GetInvoiceDetailsAsync(int invoiceId);

        /// <summary>
        /// Retrieves a salary invoice by its identifier.
        /// </summary>
        /// <param name="invoiceId">The invoice identifier.</param>
        Task<IResponse<TeacherInvoiceDto>> GetInvoiceByIdAsync(int invoiceId);

        /// <summary>
        /// Updates the payment status of a salary invoice.
        /// </summary>
        /// <param name="invoiceId">The invoice identifier.</param>
        /// <param name="dto">Payload describing the desired status.</param>
        /// <param name="userId">User identifier for audit columns.</param>
        Task<IResponse<TeacherInvoiceDto>> UpdateInvoiceStatusAsync(int invoiceId, UpdateTeacherSallaryStatusDto dto, int userId);

        /// <summary>
        /// Updates payment information for a salary invoice including optional receipt upload.
        /// </summary>
        /// <param name="dto">Form payload containing the payment data.</param>
        /// <param name="userId">User identifier for audit columns.</param>
        Task<IResponse<bool>> UpdatePaymentAsync(UpdateTeacherPaymentDto dto, int userId);

        /// <summary>
        /// Resolves the stored receipt path for the requested invoice.
        /// </summary>
        /// <param name="invoiceId">The invoice identifier.</param>
        Task<IResponse<string?>> GetPaymentReceiptPathAsync(int invoiceId);
    }
}
