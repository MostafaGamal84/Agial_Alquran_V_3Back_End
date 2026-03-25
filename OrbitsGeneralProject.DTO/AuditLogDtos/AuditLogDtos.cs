using Orbits.GeneralProject.DTO.Paging;

namespace Orbits.GeneralProject.DTO.AuditLogDtos
{
    public class AuditLogFilterDto : FilteredResultRequestDto
    {
        public string? ActionType { get; set; }
        public string? EntityType { get; set; }
        public int? ActorUserId { get; set; }
        public int? ManagerId { get; set; }
        public int? TeacherId { get; set; }
        public int? SubscribeId { get; set; }
        public int? SubscribeTypeId { get; set; }
        public int? CircleId { get; set; }
        public int? StudentPaymentId { get; set; }
        public int? CircleReportId { get; set; }
    }

    public class AuditLogListItemDto
    {
        public int Id { get; set; }
        public string? ActionType { get; set; }
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
        public string? EntityLabel { get; set; }
        public string? EntityDisplayName { get; set; }
        public string? Summary { get; set; }
        public int? ActorUserId { get; set; }
        public string? ActorName { get; set; }
        public int? ActorRoleId { get; set; }
        public string? SourceScreen { get; set; }
        public string? SourceRoute { get; set; }
        public string? RequestPath { get; set; }
        public string? HttpMethod { get; set; }
        public DateTime CreatedAt { get; set; }
        public IReadOnlyList<AuditLogChangeDto> Changes { get; set; } = Array.Empty<AuditLogChangeDto>();
        public IReadOnlyList<AuditLogParticipantDto> Participants { get; set; } = Array.Empty<AuditLogParticipantDto>();
    }

    public class AuditLogChangeDto
    {
        public string PropertyName { get; set; } = string.Empty;
        public string PropertyLabel { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }

    public class AuditLogParticipantDto
    {
        public string? ParticipantType { get; set; }
        public int? ParticipantId { get; set; }
        public string? ParticipantLabel { get; set; }
        public string? DisplayName { get; set; }
    }

    public class AuditLogFilterOptionDto
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class AuditLogFilterOptionsDto
    {
        public IReadOnlyList<AuditLogFilterOptionDto> ActionTypes { get; set; } = Array.Empty<AuditLogFilterOptionDto>();
        public IReadOnlyList<AuditLogFilterOptionDto> EntityTypes { get; set; } = Array.Empty<AuditLogFilterOptionDto>();
    }
}
