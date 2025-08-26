using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class ManagerReport:EntityBase
    {
        public int Id { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public int? ManagerId { get; set; }
        public int? StudentId { get; set; }
        public string? Notes { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
