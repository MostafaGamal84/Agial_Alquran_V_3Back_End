using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class User:EntityBase
    {
        public User()
        {
            RefreshTokens = new HashSet<RefreshToken>();
        }

        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? SecondMobile { get; set; }
        public DateTime? RegisterAt { get; set; }
        public string? PasswordHash { get; set; }
        public int? UserTypeId { get; set; }
        public int? NationalityId { get; set; }
        public int? GovernorateId { get; set; }
        public int? BranchId { get; set; }
        public bool Inactive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CodeExpirationTime { get; set; }
        public string? Code { get; set; }

        public virtual Student Student { get; set; } = null!;
        public virtual Teacher Teacher { get; set; } = null!;
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; }
    }
}
