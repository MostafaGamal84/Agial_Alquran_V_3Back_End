using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class Role:EntityBase
    {
        public int Id { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public string? UserTypeId { get; set; }
        public string? Role1 { get; set; }
        public int? CollectedScore { get; set; }
    }
}
