using Microsoft.AspNetCore.Http;

namespace Orbits.GeneralProject.DTO.TeacherSallaryDtos
{
    /// <summary>
    /// Form payload used when uploading a teacher salary receipt through the admin REST surface.
    /// </summary>
    public class TeacherSalaryReceiptUploadDto
    {
        /// <summary>
        /// Optional invoice identifier supplied by the client for payload validation.
        /// </summary>
        public int? InvoiceId { get; set; }

        /// <summary>
        /// Uploaded PDF receipt file.
        /// </summary>
        public IFormFile? Receipt { get; set; }

        /// <summary>
        /// Optional amount override.
        /// </summary>
        public double? Amount { get; set; }

        /// <summary>
        /// Optional payment status flag.
        /// </summary>
        public bool? PayStatue { get; set; }

        /// <summary>
        /// Optional cancellation flag.
        /// </summary>
        public bool? IsCancelled { get; set; }
    }
}
