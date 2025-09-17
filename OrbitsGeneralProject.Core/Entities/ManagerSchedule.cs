using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class ManagerSchedule:EntityBase
    {
        public int Id { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public int? ManagerId { get; set; }
        public int? StudentId { get; set; }
        public bool? IsManagerAttended { get; set; }
        public bool? IsStudentAttended { get; set; }
        public DateTime? ScheduleDate { get; set; }
        public DateTime? AttendDate { get; set; }
        public string? Notes { get; set; }

        public virtual User? Manager { get; set; }
        public virtual User? Student { get; set; }
    }
}
