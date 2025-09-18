namespace Orbits.GeneralProject.DTO.TeacherSallaryDtos
{
    public class TeacherSallaryGenerationResultDto
    {
        public DateTime Month { get; set; }

        /// <summary>
        /// Number of invoices that were newly created for the requested month.
        /// </summary>
        public int CreatedInvoices { get; set; }

        /// <summary>
        /// Number of existing invoices that were updated with the latest totals.
        /// </summary>
        public int UpdatedInvoices { get; set; }

        /// <summary>
        /// Number of invoices that were skipped because they are already marked as paid.
        /// </summary>
        public int SkippedPaidInvoices { get; set; }

        /// <summary>
        /// Number of teachers that had no billable minutes for the requested month.
        /// </summary>
        public int SkippedZeroValueInvoices { get; set; }

        /// <summary>
        /// Total number of teachers that had billable activity for the requested month (excluding the zero-minute skips).
        /// </summary>
        public int TotalTeachers { get; set; }

        /// <summary>
        /// Sum of all billable minutes for the requested month.
        /// </summary>
        public int TotalMinutes { get; set; }

        /// <summary>
        /// Sum of all salary amounts calculated for the requested month.
        /// </summary>
        public double TotalSalary { get; set; }
    }
}
