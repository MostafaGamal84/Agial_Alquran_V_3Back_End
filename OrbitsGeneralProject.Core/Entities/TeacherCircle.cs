using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class TeacherCircle:EntityBase
    {
        public int Id { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public int? TeacherId { get; set; }
        public int? CircleId { get; set; }
    }
}
