namespace Orbits.GeneralProject.Core.Entities
{
    public partial class AuditLogParticipant : EntityBase
    {
        public int Id { get; set; }
        public int AuditLogId { get; set; }
        public string? ParticipantType { get; set; }
        public int? ParticipantId { get; set; }
        public string? ParticipantLabel { get; set; }
        public string? DisplayName { get; set; }

        public virtual AuditLog? AuditLog { get; set; }
    }
}
