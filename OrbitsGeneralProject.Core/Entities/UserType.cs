using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class UserType:EntityBase
    {
        public UserType()
        {
            Roles = new HashSet<Role>();
        }

        public int Id { get; set; }
        public string? UserTypeName { get; set; }
        public bool? IsDeleted { get; set; }

        public virtual ICollection<Role> Roles { get; set; }
    }
}
