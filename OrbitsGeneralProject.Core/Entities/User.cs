using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class User:EntityBase
    {
        public User()
        {
            ChallengeParticipants = new HashSet<ChallengeParticipant>();
            CircleReportStudents = new HashSet<CircleReport>();
            CircleReportTeachers = new HashSet<CircleReport>();
            Circles = new HashSet<Circle>();
            InverseManager = new HashSet<User>();
            InverseTeacher = new HashSet<User>();
            ManagerCircles = new HashSet<ManagerCircle>();
            ManagerReportManagers = new HashSet<ManagerReport>();
            ManagerReportStudents = new HashSet<ManagerReport>();
            ManagerSallaries = new HashSet<ManagerSallary>();
            ManagerStudentManagers = new HashSet<ManagerStudent>();
            ManagerStudentStudents = new HashSet<ManagerStudent>();
            RefreshTokens = new HashSet<RefreshToken>();
            StudentPayments = new HashSet<StudentPayment>();
            StudentSubscribes = new HashSet<StudentSubscribe>();
            TeacherReportRecords = new HashSet<TeacherReportRecord>();
            TeacherSallaries = new HashSet<TeacherSallary>();
            TeacherScheduleStudents = new HashSet<TeacherSchedule>();
            TeacherScheduleTeachers = new HashSet<TeacherSchedule>();
        }

        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? SecondMobile { get; set; }
        public DateTime? RegisterAt { get; set; }
        public string? PasswordHash { get; set; }
        public int? UserTypeId { get; set; }
        public int? ResidentId { get; set; }
        public int? NationalityId { get; set; }
        public int? GovernorateId { get; set; }
        public int? BranchId { get; set; }
        public bool Inactive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? ModefiedAt { get; set; }
        public int? ModefiedBy { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CodeExpirationTime { get; set; }
        public string? Code { get; set; }
        public int? CircleId { get; set; }
        public int? TeacherId { get; set; }
        public int? ManagerId { get; set; }

        public virtual Circle? Circle { get; set; }
        public virtual Governorate? Governorate { get; set; }
        public virtual User? Manager { get; set; }
        public virtual Nationality? Nationality { get; set; }
        public virtual Nationality? Resident { get; set; }
        public virtual User? Teacher { get; set; }
        public virtual ICollection<ChallengeParticipant> ChallengeParticipants { get; set; }
        public virtual ICollection<CircleReport> CircleReportStudents { get; set; }
        public virtual ICollection<CircleReport> CircleReportTeachers { get; set; }
        public virtual ICollection<Circle> Circles { get; set; }
        public virtual ICollection<User> InverseManager { get; set; }
        public virtual ICollection<User> InverseTeacher { get; set; }
        public virtual ICollection<ManagerCircle> ManagerCircles { get; set; }
        public virtual ICollection<ManagerReport> ManagerReportManagers { get; set; }
        public virtual ICollection<ManagerReport> ManagerReportStudents { get; set; }
        public virtual ICollection<ManagerSallary> ManagerSallaries { get; set; }
        public virtual ICollection<ManagerStudent> ManagerStudentManagers { get; set; }
        public virtual ICollection<ManagerStudent> ManagerStudentStudents { get; set; }
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; }
        public virtual ICollection<StudentPayment> StudentPayments { get; set; }
        public virtual ICollection<StudentSubscribe> StudentSubscribes { get; set; }
        public virtual ICollection<TeacherReportRecord> TeacherReportRecords { get; set; }
        public virtual ICollection<TeacherSallary> TeacherSallaries { get; set; }
        public virtual ICollection<TeacherSchedule> TeacherScheduleStudents { get; set; }
        public virtual ICollection<TeacherSchedule> TeacherScheduleTeachers { get; set; }
    }
}
