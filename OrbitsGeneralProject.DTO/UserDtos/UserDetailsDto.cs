using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.DTO.RegionDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.UserDtos
{
    public class UserDetailsDto
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? SecondMobile { get; set; }
        public DateTime? RegisterAt { get; set; }
        public int? NationalityId { get; set; }
        public int? ResidentId { get; set; }
        public int? GovernorateId { get; set; }
        public int? BranchId { get; set; }
        public bool Inactive { get; set; }
        public int? CircleId { get; set; }
        public int? TeacherId { get; set; }
        public int? ManagerId { get; set; }

        public virtual string? CircleName { get; set; }
        public virtual string? GovernorateName { get; set; }
        public virtual string? ManagerName { get; set; }
        public virtual RegionDto? Nationality { get; set; }
        public virtual RegionDto? Resident { get; set; }
        public virtual string? TeacherName { get; set; }
        public virtual ICollection<ChallengeParticipant> ChallengeParticipants { get; set; }
        public virtual ICollection<CircleReport> CircleReportCircleNavigations { get; set; }
        public virtual ICollection<CircleReport> CircleReportTeachers { get; set; }
        public virtual ICollection<Circle> Circles { get; set; }
        public virtual ICollection<User> InverseManager { get; set; }
        public virtual ICollection<User> InverseTeacher { get; set; }
        public virtual ICollection<ManagerCircle> ManagerCircles { get; set; }
        public virtual ICollection<ManagerReport> ManagerReportManagers { get; set; }
        public virtual ICollection<ManagerReport> ManagerReportStudents { get; set; }
        public virtual ICollection<ManagerSallary> ManagerSallaries { get; set; }
        public virtual ICollection<ManagerSchedule> ManagerScheduleManagers { get; set; }
        public virtual ICollection<ManagerSchedule> ManagerScheduleStudents { get; set; }
        public virtual ICollection<ManagerStudent> ManagerStudentManagers { get; set; }
        public virtual ICollection<ManagerStudent> ManagerStudentStudents { get; set; }
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; }
        public virtual ICollection<StudentPayment> StudentPayments { get; set; }
        public virtual ICollection<StudentTime> StudentTimes { get; set; }
        public virtual ICollection<TeacherSallary> TeacherSallaries { get; set; }
        public virtual ICollection<TeacherSchedule> TeacherScheduleStudents { get; set; }
        public virtual ICollection<TeacherSchedule> TeacherScheduleTeachers { get; set; }
    }
}
