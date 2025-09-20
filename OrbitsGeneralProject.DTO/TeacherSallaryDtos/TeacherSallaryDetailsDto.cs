namespace Orbits.GeneralProject.DTO.TeacherSallaryDtos
{
    /// <summary>
    /// Represents the detailed view of a teacher salary invoice including the
    /// original invoice information and an optional monthly summary snapshot.
    /// </summary>
    public class TeacherSallaryDetailsDto
    {
        /// <summary>
        /// The invoice information.
        /// </summary>
        public TeacherInvoiceDto Invoice { get; set; } = default!;

        /// <summary>
        /// Aggregated statistics for the invoice month. Can be <c>null</c> when the
        /// teacher or month context is unavailable (e.g. missing teacher reference).
        /// </summary>
        public TeacherMonthlySummaryDto? MonthlySummary { get; set; }
    }
}
