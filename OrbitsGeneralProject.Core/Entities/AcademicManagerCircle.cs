using System;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class AcademicManagerCircle : EntityBase
    {
        public int Id { get; set; }
        public int? ManagerId { get; set; }
        public int? AcademicCircleId { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public int? ModefiedBy { get; set; }

        public virtual User? Manager { get; set; }
        public virtual AcademicCircle? AcademicCircle { get; set; }
    }
}
