using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class Nationality:EntityBase
    {
        public Nationality()
        {
            UserNationalities = new HashSet<User>();
            UserResidents = new HashSet<User>();
        }

        public int Id { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public string? Name { get; set; }
        public int? TelCode { get; set; }

        public virtual ICollection<User> UserNationalities { get; set; }
        public virtual ICollection<User> UserResidents { get; set; }
    }
}
