using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class StudentSubscribe:EntityBase
    {
        public int Id { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public int? StudentId { get; set; }
        public int? StudentSubscribeId { get; set; }
        public int? RemainingMinutes { get; set; }
        public int? StudentSubscribeTypeId { get; set; }
        public int? CircleReportId { get; set; }
        public bool? PayStatus { get; set; }
        public int? StudentPaymentId { get; set; }

        public virtual CircleReport? CircleReport { get; set; }
        public virtual User? Student { get; set; }
        public virtual StudentPayment? StudentPayment { get; set; }
        public virtual Subscribe? StudentSubscribeNavigation { get; set; }
        public virtual SubscribeType? StudentSubscribeType { get; set; }
    }
}
