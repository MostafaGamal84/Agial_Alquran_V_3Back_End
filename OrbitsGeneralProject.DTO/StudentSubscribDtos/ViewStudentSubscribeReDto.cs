using Orbits.GeneralProject.Core.Enums;
using System;

namespace Orbits.GeneralProject.DTO.StudentSubscribDtos
{
    public class ViewStudentSubscribeReDto
    {
        public int? Id { get; set; }
        public int? StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? StudentMobile { get; set; }
        public bool? PayStatus { get; set; }
        public string? Plan { get; set; }
        public int? RemainingMinutes { get; set; }
        public int? ConsumedMinutes { get; set; }
        public int? ConsumedSessionsCount { get; set; }
        public decimal? ConsumedHours { get; set; }
        public DateTime? StartDate { get; set; }
        public int? StudentPaymentId { get; set; }
        public SubscribeTypeCategory? SubscribeTypeGroup { get; set; }
    }
}
