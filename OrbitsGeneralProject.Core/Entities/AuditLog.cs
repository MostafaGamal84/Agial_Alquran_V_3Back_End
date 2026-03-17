using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class AuditLog : EntityBase
    {
        public AuditLog()
        {
            Participants = new HashSet<AuditLogParticipant>();
        }

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
        public DateTime CreatedAt { get; set; }
        public string? ChangesJson { get; set; }

        public virtual ICollection<AuditLogParticipant> Participants { get; set; }
    }
}
