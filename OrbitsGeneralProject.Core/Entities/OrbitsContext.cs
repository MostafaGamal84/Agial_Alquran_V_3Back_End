using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class OrbitsContext : DbContext
    {
        public virtual DbSet<Challenge> Challenges { get; set; } = null!;
        public virtual DbSet<ChallengeParticipant> ChallengeParticipants { get; set; } = null!;
        public virtual DbSet<ChallengeRole> ChallengeRoles { get; set; } = null!;
        public virtual DbSet<Circle> Circles { get; set; } = null!;
        public virtual DbSet<CircleReport> CircleReports { get; set; } = null!;
        public virtual DbSet<Family> Families { get; set; } = null!;
        public virtual DbSet<Governorate> Governorates { get; set; } = null!;
        public virtual DbSet<ManagerCircle> ManagerCircles { get; set; } = null!;
        public virtual DbSet<ManagerReport> ManagerReports { get; set; } = null!;
        public virtual DbSet<ManagerReportType> ManagerReportTypes { get; set; } = null!;
        public virtual DbSet<ManagerSallary> ManagerSallaries { get; set; } = null!;
        public virtual DbSet<ManagerSchedule> ManagerSchedules { get; set; } = null!;
        public virtual DbSet<ManagerStudent> ManagerStudents { get; set; } = null!;
        public virtual DbSet<Nationality> Nationalities { get; set; } = null!;
        public virtual DbSet<Permission> Permissions { get; set; } = null!;
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public virtual DbSet<Role> Roles { get; set; } = null!;
        public virtual DbSet<StudentPayment> StudentPayments { get; set; } = null!;
        public virtual DbSet<StudentSubscribe> StudentSubscribes { get; set; } = null!;
        public virtual DbSet<StudentTime> StudentTimes { get; set; } = null!;
        public virtual DbSet<Subscribe> Subscribes { get; set; } = null!;
        public virtual DbSet<SubscribeType> SubscribeTypes { get; set; } = null!;
        public virtual DbSet<TeacherReportRecord> TeacherReportRecords { get; set; } = null!;
        public virtual DbSet<TeacherSallary> TeacherSallaries { get; set; } = null!;
        public virtual DbSet<TeacherSchedule> TeacherSchedules { get; set; } = null!;
        public virtual DbSet<Time> Times { get; set; } = null!;
        public virtual DbSet<User> Users { get; set; } = null!;
        public virtual DbSet<UserType> UserTypes { get; set; } = null!;

 public OrbitsContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("Data Source=Mostafa-Gamal;Initial Catalog=agial_alquran_V_3_DB;Integrated Security=True;TrustServerCertificate=True");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Challenge>(entity =>
            {
                entity.ToTable("Challenge");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.EndDate).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.Property(e => e.StartDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<ChallengeParticipant>(entity =>
            {
                entity.ToTable("ChallengeParticipant");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.HasOne(d => d.Challenge)
                    .WithMany(p => p.ChallengeParticipants)
                    .HasForeignKey(d => d.ChallengeId)
                    .HasConstraintName("FK_ChallengeParticipant_Challenge");

                entity.HasOne(d => d.Participant)
                    .WithMany(p => p.ChallengeParticipants)
                    .HasForeignKey(d => d.ParticipantId)
                    .HasConstraintName("FK_ChallengeParticipant_Participant");
            });

            modelBuilder.Entity<ChallengeRole>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.ChallengeRoles)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("FK_ChallengeRoles_Roles");
            });

            modelBuilder.Entity<Circle>(entity =>
            {
                entity.ToTable("Circle");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.HasOne(d => d.Teacher)
                    .WithMany(p => p.Circles)
                    .HasForeignKey(d => d.TeacherId)
                    .HasConstraintName("FK_Circle_Teacher");
            });

            modelBuilder.Entity<CircleReport>(entity =>
            {
                entity.HasOne(d => d.Circle)
                    .WithMany(p => p.CircleReports)
                    .HasForeignKey(d => d.CircleId)
                    .HasConstraintName("FK_CircleReports_Circle");

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.CircleReportStudents)
                    .HasForeignKey(d => d.StudentId)
                    .HasConstraintName("FK_CircleReports_Student");

                entity.HasOne(d => d.Teacher)
                    .WithMany(p => p.CircleReportTeachers)
                    .HasForeignKey(d => d.TeacherId)
                    .HasConstraintName("FK_CircleReports_Teacher");
            });

            modelBuilder.Entity<Family>(entity =>
            {
                entity.ToTable("Family");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<Governorate>(entity =>
            {
                entity.ToTable("Governorate");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<ManagerCircle>(entity =>
            {
                entity.ToTable("ManagerCircle");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.HasOne(d => d.Circle)
                    .WithMany(p => p.ManagerCircles)
                    .HasForeignKey(d => d.CircleId)
                    .HasConstraintName("FK_ManagerCircle_Circle");

                entity.HasOne(d => d.Manager)
                    .WithMany(p => p.ManagerCircles)
                    .HasForeignKey(d => d.ManagerId)
                    .HasConstraintName("FK_ManagerCircle_Manager");
            });

            modelBuilder.Entity<ManagerReport>(entity =>
            {
                entity.ToTable("ManagerReport");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.HasOne(d => d.Manager)
                    .WithMany(p => p.ManagerReportManagers)
                    .HasForeignKey(d => d.ManagerId)
                    .HasConstraintName("FK_ManagerReport_Manager");

                entity.HasOne(d => d.ManagerReportType)
                    .WithMany(p => p.ManagerReports)
                    .HasForeignKey(d => d.ManagerReportTypeId)
                    .HasConstraintName("FK_ManagerReport_ManagerReportType");

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.ManagerReportStudents)
                    .HasForeignKey(d => d.StudentId)
                    .HasConstraintName("FK_ManagerReport_Student");
            });

            modelBuilder.Entity<ManagerReportType>(entity =>
            {
                entity.ToTable("ManagerReportType");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<ManagerSallary>(entity =>
            {
                entity.ToTable("ManagerSallary");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.Property(e => e.Month).HasColumnType("datetime");

                entity.Property(e => e.PayedAt).HasColumnType("datetime");

                entity.HasOne(d => d.Manager)
                    .WithMany(p => p.ManagerSallaries)
                    .HasForeignKey(d => d.ManagerId)
                    .HasConstraintName("FK_ManagerSallary_Manager");
            });

            modelBuilder.Entity<ManagerSchedule>(entity =>
            {
                entity.ToTable("ManagerSchedule");

                entity.Property(e => e.AttendDate).HasColumnType("datetime");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.Property(e => e.ScheduleDate).HasColumnType("datetime");

                entity.HasOne(d => d.Manager)
                    .WithMany(p => p.ManagerScheduleManagers)
                    .HasForeignKey(d => d.ManagerId)
                    .HasConstraintName("FK_ManagerSchedule_Manager");

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.ManagerScheduleStudents)
                    .HasForeignKey(d => d.StudentId)
                    .HasConstraintName("FK_ManagerSchedule_Student");
            });

            modelBuilder.Entity<ManagerStudent>(entity =>
            {
                entity.ToTable("ManagerStudent");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.HasOne(d => d.Manager)
                    .WithMany(p => p.ManagerStudentManagers)
                    .HasForeignKey(d => d.ManagerId)
                    .HasConstraintName("FK_ManagerStudent_Manager");

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.ManagerStudentStudents)
                    .HasForeignKey(d => d.StudentId)
                    .HasConstraintName("FK_ManagerStudent_Student");
            });

            modelBuilder.Entity<Nationality>(entity =>
            {
                entity.ToTable("Nationality");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<Permission>(entity =>
            {
                entity.ToTable("Permission");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("RefreshToken");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.RefreshTokens)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.Property(e => e.Role1).HasColumnName("Role");

                entity.HasOne(d => d.UserType)
                    .WithMany(p => p.Roles)
                    .HasForeignKey(d => d.UserTypeId)
                    .HasConstraintName("FK_Roles_UserType");
            });

            modelBuilder.Entity<StudentPayment>(entity =>
            {
                entity.ToTable("StudentPayment");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.Property(e => e.PaymentDate).HasColumnType("datetime");

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.StudentPayments)
                    .HasForeignKey(d => d.StudentId)
                    .HasConstraintName("FK_StudentPayment_Student");

                entity.HasOne(d => d.StudentSubscribe)
                    .WithMany(p => p.StudentPayments)
                    .HasForeignKey(d => d.StudentSubscribeId)
                    .HasConstraintName("FK_StudentPayment_Subscribe");
            });

            modelBuilder.Entity<StudentSubscribe>(entity =>
            {
                entity.ToTable("StudentSubscribe");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.HasOne(d => d.CircleReport)
                    .WithMany(p => p.StudentSubscribes)
                    .HasForeignKey(d => d.CircleReportId)
                    .HasConstraintName("FK_StudentSubscribe_CircleReport");

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.StudentSubscribes)
                    .HasForeignKey(d => d.StudentId)
                    .HasConstraintName("FK_StudentSubscribe_Student");

                entity.HasOne(d => d.StudentPayment)
                    .WithMany(p => p.StudentSubscribes)
                    .HasForeignKey(d => d.StudentPaymentId)
                    .HasConstraintName("FK_StudentSubscribe_StudentPayment");

                entity.HasOne(d => d.StudentSubscribeNavigation)
                    .WithMany(p => p.StudentSubscribes)
                    .HasForeignKey(d => d.StudentSubscribeId)
                    .HasConstraintName("FK_StudentSubscribe_Subscribe");

                entity.HasOne(d => d.StudentSubscribeType)
                    .WithMany(p => p.StudentSubscribes)
                    .HasForeignKey(d => d.StudentSubscribeTypeId)
                    .HasConstraintName("FK_StudentSubscribe_StudentSubscribeType");
            });

            modelBuilder.Entity<StudentTime>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.StudentTimes)
                    .HasForeignKey(d => d.StudentId)
                    .HasConstraintName("FK_StudentTimes_Student");

                entity.HasOne(d => d.Time)
                    .WithMany(p => p.StudentTimes)
                    .HasForeignKey(d => d.TimeId)
                    .HasConstraintName("FK_StudentTimes_Times");
            });

            modelBuilder.Entity<Subscribe>(entity =>
            {
                entity.ToTable("Subscribe");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.Leprice)
                    .HasColumnType("decimal(5, 2)")
                    .HasColumnName("LEPrice");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.Property(e => e.Sarprice)
                    .HasColumnType("decimal(5, 2)")
                    .HasColumnName("SARPrice");

                entity.Property(e => e.Usdprice)
                    .HasColumnType("decimal(5, 2)")
                    .HasColumnName("USDPrice");

                entity.HasOne(d => d.SubscribeType)
                    .WithMany(p => p.Subscribes)
                    .HasForeignKey(d => d.SubscribeTypeId)
                    .HasConstraintName("FK_Subscribe_SubscribeType");
            });

            modelBuilder.Entity<SubscribeType>(entity =>
            {
                entity.ToTable("SubscribeType");

                entity.Property(e => e.ArabPricePerHour).HasColumnType("decimal(5, 2)");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ForignPricePerHour).HasColumnType("decimal(5, 2)");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<TeacherReportRecord>(entity =>
            {
                entity.ToTable("TeacherReportRecord");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.HasOne(d => d.CircleReport)
                    .WithMany(p => p.TeacherReportRecords)
                    .HasForeignKey(d => d.CircleReportId)
                    .HasConstraintName("FK_TeacherReportRecord_CircleReport");

                entity.HasOne(d => d.Teacher)
                    .WithMany(p => p.TeacherReportRecords)
                    .HasForeignKey(d => d.TeacherId)
                    .HasConstraintName("FK_TeacherReportRecord_Teacher");
            });

            modelBuilder.Entity<TeacherSallary>(entity =>
            {
                entity.ToTable("TeacherSallary");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.IsDeleted).HasDefaultValueSql("((0))");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.Property(e => e.Month).HasColumnType("datetime");

                entity.Property(e => e.PayedAt).HasColumnType("datetime");

                entity.HasOne(d => d.Teacher)
                    .WithMany(p => p.TeacherSallaries)
                    .HasForeignKey(d => d.TeacherId)
                    .HasConstraintName("FK_TeacherSallary_Teacher");
            });

            modelBuilder.Entity<TeacherSchedule>(entity =>
            {
                entity.ToTable("TeacherSchedule");

                entity.Property(e => e.AttendDate).HasColumnType("datetime");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.Property(e => e.ScheduleDate).HasColumnType("datetime");

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.TeacherScheduleStudents)
                    .HasForeignKey(d => d.StudentId)
                    .HasConstraintName("FK_TeacherSchedule_Student");

                entity.HasOne(d => d.Teacher)
                    .WithMany(p => p.TeacherScheduleTeachers)
                    .HasForeignKey(d => d.TeacherId)
                    .HasConstraintName("FK_TeacherSchedule_Teacher");
            });

            modelBuilder.Entity<Time>(entity =>
            {
                entity.ToTable("Time");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User");

                entity.Property(e => e.Code)
                    .HasMaxLength(10)
                    .IsFixedLength();

                entity.Property(e => e.CodeExpirationTime).HasColumnType("datetime");

                entity.Property(e => e.Email).HasMaxLength(50);

                entity.Property(e => e.Mobile).HasMaxLength(50);

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.Property(e => e.RegisterAt).HasColumnType("datetime");

                entity.HasOne(d => d.Circle)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.CircleId)
                    .HasConstraintName("FK_Student_Circle");

                entity.HasOne(d => d.Governorate)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.GovernorateId)
                    .HasConstraintName("FK_User_Governorate");

                entity.HasOne(d => d.Manager)
                    .WithMany(p => p.InverseManager)
                    .HasForeignKey(d => d.ManagerId)
                    .HasConstraintName("FK_User_Manager");

                entity.HasOne(d => d.Nationality)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.NationalityId)
                    .HasConstraintName("FK_User_Nationality");

                entity.HasOne(d => d.Teacher)
                    .WithMany(p => p.InverseTeacher)
                    .HasForeignKey(d => d.TeacherId)
                    .HasConstraintName("FK_User_Teacher");
            });

            modelBuilder.Entity<UserType>(entity =>
            {
                entity.ToTable("UserType");

                entity.Property(e => e.IsDeleted).HasDefaultValueSql("((0))");

                entity.Property(e => e.UserTypeName).HasMaxLength(50);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
