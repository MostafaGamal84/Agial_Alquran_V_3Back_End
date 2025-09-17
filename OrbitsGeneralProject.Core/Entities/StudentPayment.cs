using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class StudentPayment:EntityBase
    {
        public StudentPayment()
        {
            StudentSubscribes = new HashSet<StudentSubscribe>();
        }

        public int Id { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public DateTime?ModefiedAt { get; set; }
        public int? StudentId { get; set; }
        public int? StudentSubscribeId { get; set; }
        public int? Amount { get; set; }
        public int? CurrencyId { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? ReceiptPath { get; set; }
        public bool? PayStatue { get; set; }
        public bool? IsCancelled { get; set; }

        public virtual User? Student { get; set; }
        public virtual Subscribe? StudentSubscribe { get; set; }
        public virtual ICollection<StudentSubscribe> StudentSubscribes { get; set; }
    }
}
