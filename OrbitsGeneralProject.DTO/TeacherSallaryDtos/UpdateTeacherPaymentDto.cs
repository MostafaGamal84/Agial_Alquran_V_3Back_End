using Microsoft.AspNetCore.Http;

namespace Orbits.GeneralProject.DTO.TeacherSallaryDtos
{
    /// <summary>
    /// Payload used to update teacher salary payment metadata including the optional receipt file.
    /// </summary>
    public class UpdateTeacherPaymentDto
    {
        /// <summary>
        /// Identifier of the teacher salary invoice to update.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Optional salary amount override.
        /// </summary>
        public double? Amount { get; set; }

        /// <summary>
        /// Uploaded receipt file (PDF). When provided, the stored receipt path is replaced.
        /// </summary>
        public IFormFile? ReceiptPath { get; set; }

        /// <summary>
        /// Indicates whether the invoice should be marked as paid.
        /// </summary>
        public bool? PayStatue { get; set; }

        /// <summary>
        /// Indicates whether the payment has been cancelled.
        /// </summary>
        public bool? IsCancelled { get; set; }
    }
}

