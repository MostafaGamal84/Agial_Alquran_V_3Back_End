using System;

namespace Orbits.GeneralProject.DTO.TeacherSallaryDtos
{
    public class TeacherInvoiceDto
    {
        public int Id { get; set; }
        public int? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public DateTime? Month { get; set; }
        public double? Salary { get; set; }
        public bool? IsPayed { get; set; }
        public DateTime? PayedAt { get; set; }
        public string? ReceiptPath { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ModefiedAt { get; set; }
    }
}
