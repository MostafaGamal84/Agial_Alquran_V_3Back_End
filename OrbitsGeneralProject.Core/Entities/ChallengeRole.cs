using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class ChallengeRole:EntityBase
    {
        public int Id { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public int? ChallengeId { get; set; }
        public int? RoleId { get; set; }
    }
}
