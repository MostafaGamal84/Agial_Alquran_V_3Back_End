using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.StudentSubscribDtos.StudentPaymentDtos
{
    public class StudentPaymentReDto
    {
        
            public int InvoiceId { get; set; }
            public int StudentId { get; set; }

            public string? UserName { get; set; }
            public string? UserEmail { get; set; }
            public string? Subscribe { get; set; }

        public DateTime CreateDate { get; set; }
            public DateTime? DueDate { get; set; }
        public DateTime? PaymentDate { get; set; }
        
        public decimal Amount { get; set; }

            // "Paid" | "Unpaid" | "Overdue"
            public string StatusText { get; set; } = string.Empty;
        

    }
}
