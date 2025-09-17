using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class Role:EntityBase
    {
        public Role()
        {
            ChallengeRoles = new HashSet<ChallengeRole>();
        }

        public int Id { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public int? UserTypeId { get; set; }
        public string? Role1 { get; set; }
        public int? CollectedScore { get; set; }

        public virtual UserType? UserType { get; set; }
        public virtual ICollection<ChallengeRole> ChallengeRoles { get; set; }
    }
}
