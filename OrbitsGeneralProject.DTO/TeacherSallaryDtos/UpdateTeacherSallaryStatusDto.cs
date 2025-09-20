using System;

namespace Orbits.GeneralProject.DTO.TeacherSallaryDtos
{
    /// <summary>
    /// Payload used to update the payment status of a teacher salary invoice.
    /// </summary>
    public class UpdateTeacherSallaryStatusDto
    {
        /// <summary>
        /// Indicates whether the invoice is marked as paid.
        /// </summary>
        public bool IsPayed { get; set; }

        /// <summary>
        /// Optional custom paid timestamp. When omitted and <see cref="IsPayed"/> is true
        /// the current UTC timestamp is applied.
        /// </summary>
        public DateTime? PayedAt { get; set; }
    }
}
