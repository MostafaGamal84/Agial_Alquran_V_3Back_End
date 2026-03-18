using System;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class StudentSubscribeHistory : Orbits.GeneralProject.Core.Infrastructure.EntityBase
    {
        public int Id { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? StudentId { get; set; }
        public int? StudentSubscribeRecordId { get; set; }
        public string? ActionType { get; set; }
        public int? OldSubscribeId { get; set; }
        public string? OldPlanName { get; set; }
        public int? NewSubscribeId { get; set; }
        public string? NewPlanName { get; set; }
        public int? OldRemainingMinutes { get; set; }
        public int? NewRemainingMinutes { get; set; }
        public int? UsedMinutes { get; set; }
        public decimal? OldAmount { get; set; }
        public decimal? NewAmount { get; set; }
        public decimal? AmountPaidBeforeChange { get; set; }
        public decimal? RemainingAmountAfterChange { get; set; }
        public int? OldCurrencyId { get; set; }
        public int? NewCurrencyId { get; set; }
        public bool? OldPayStatus { get; set; }
        public bool? NewPayStatus { get; set; }

        public virtual User? CreatedByUser { get; set; }
        public virtual User? Student { get; set; }
        public virtual StudentSubscribe? StudentSubscribeRecord { get; set; }
    }
}
