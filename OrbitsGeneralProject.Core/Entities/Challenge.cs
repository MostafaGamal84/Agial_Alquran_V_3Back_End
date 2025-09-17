using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class Challenge:EntityBase
    {
        public Challenge()
        {
            ChallengeParticipants = new HashSet<ChallengeParticipant>();
        }

        public int Id { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public string? Name { get; set; }
        public int? RoleId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? WinningScore { get; set; }
        public bool? IsActive { get; set; }
        public int? Winner { get; set; }

        public virtual ICollection<ChallengeParticipant> ChallengeParticipants { get; set; }
    }
}
