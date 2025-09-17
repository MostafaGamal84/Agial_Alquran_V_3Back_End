using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class ManagerReportType:EntityBase
    {
        public ManagerReportType()
        {
            ManagerReports = new HashSet<ManagerReport>();
        }

        public int Id { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public string? ManagerReportTypeName { get; set; }
        public bool? IsDeleted { get; set; }

        public virtual ICollection<ManagerReport> ManagerReports { get; set; }
    }
}
