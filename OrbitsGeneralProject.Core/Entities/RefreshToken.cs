using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class RefreshToken:EntityBase
    {
        public int Id { get; set; }
        public string? Token { get; set; }
        public DateTime ExpiresOn { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? RevokedOn { get; set; }
        public int UserId { get; set; }
        public bool Inactive { get; set; }

        public virtual User User { get; set; } = null!;
    }
}
