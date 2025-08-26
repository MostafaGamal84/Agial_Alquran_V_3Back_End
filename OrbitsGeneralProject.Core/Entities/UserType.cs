using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class UserType:EntityBase
    {
        public int Id { get; set; }
        public string? UserTypeName { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
