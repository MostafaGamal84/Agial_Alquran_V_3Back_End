﻿using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class Circle:EntityBase
    {
        public Circle()
        {
            CircleReports = new HashSet<CircleReport>();
            ManagerCircles = new HashSet<ManagerCircle>();
            Users = new HashSet<User>();
        }

        public int Id { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public string? Name { get; set; }
        public bool? IsDeleted { get; set; }
        public int? TeacherId { get; set; }
        public int? Time { get; set; }

        public virtual User? Teacher { get; set; }
        public virtual ICollection<CircleReport> CircleReports { get; set; }
        public virtual ICollection<ManagerCircle> ManagerCircles { get; set; }
        public virtual ICollection<User> Users { get; set; }
    }
}
