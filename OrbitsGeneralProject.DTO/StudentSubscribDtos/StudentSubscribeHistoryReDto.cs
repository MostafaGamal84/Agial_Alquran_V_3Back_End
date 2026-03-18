using System;

namespace Orbits.GeneralProject.DTO.StudentSubscribDtos
{
    public class StudentSubscribeHistoryReDto
    {
        public int Id { get; set; }
        public int? StudentId { get; set; }
        public int? StudentSubscribeRecordId { get; set; }
        public string? ActionType { get; set; }
        public string? ChangedByName { get; set; }
        public DateTime? ChangedAt { get; set; }
        public string? OldPlanName { get; set; }
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
    }
}
