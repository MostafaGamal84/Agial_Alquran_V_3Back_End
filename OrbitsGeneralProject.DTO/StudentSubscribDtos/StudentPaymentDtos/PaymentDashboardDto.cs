using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.StudentSubscribDtos.StudentPaymentDtos
{
    public class PaymentsFullDashboardDto
    {
        // ==== Month context ====
        public DateTime Month { get; set; }

        // ==== Paid (current month) ====
        public double TotalPaid { get; set; }
        public double TotalPaidCount { get; set; }
        public double TotalPaidMoMPercentage { get; set; }        // vs previous month (amount)

        // ==== Unpaid (current month) ====
        public double TotalUnPaid { get; set; }
        public double TotalUnPaidCount { get; set; }
        public double TotalUnPaidMoMPercentage { get; set; }      // vs previous month (amount)

        // ==== Overdue (subset of unpaid, current month) ====
        public double TotalOverdue { get; set; }
        public double TotalOverdueCount { get; set; }
        public double TotalOverdueMoMPercentage { get; set; }     // vs previous month (amount)

        // ==== Blue card: Total Receivables (across ALL time, based on 'now') ====
        public double CurrentReceivables { get; set; }            // unpaid with PaymentDate >= now
        public double OverdueReceivables { get; set; }            // unpaid with PaymentDate <  now
        public double TotalReceivables { get; set; }              // Current + Overdue
        public double CollectionRate { get; set; }                // paid / (paid + receivables) * 100
    }
}
