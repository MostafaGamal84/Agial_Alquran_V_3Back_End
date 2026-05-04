using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class User
    {
        public virtual ICollection<AcademicCircle> AcademicCircles { get; set; } = new HashSet<AcademicCircle>();
        public virtual ICollection<AcademicCircleStudent> AcademicCircleStudentStudents { get; set; } = new HashSet<AcademicCircleStudent>();
        public virtual ICollection<AcademicManagerCircle> AcademicManagerCircles { get; set; } = new HashSet<AcademicManagerCircle>();
        public virtual ICollection<AcademicManagerStudent> AcademicManagerStudentManagers { get; set; } = new HashSet<AcademicManagerStudent>();
        public virtual ICollection<AcademicManagerStudent> AcademicManagerStudentStudents { get; set; } = new HashSet<AcademicManagerStudent>();
        public virtual ICollection<AcademicManagerTeacher> AcademicManagerTeacherManagers { get; set; } = new HashSet<AcademicManagerTeacher>();
        public virtual ICollection<AcademicManagerTeacher> AcademicManagerTeacherTeachers { get; set; } = new HashSet<AcademicManagerTeacher>();
        public virtual ICollection<AcademicReport> AcademicReportStudents { get; set; } = new HashSet<AcademicReport>();
        public virtual ICollection<AcademicReport> AcademicReportTeachers { get; set; } = new HashSet<AcademicReport>();
    }
}
