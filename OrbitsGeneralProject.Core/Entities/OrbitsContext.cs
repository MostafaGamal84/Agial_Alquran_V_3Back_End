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
        public virtual DbSet<Student> Students { get; set; } = null!;
        public virtual DbSet<StudentPayment> StudentPayments { get; set; } = null!;
        public virtual DbSet<StudentTime> StudentTimes { get; set; } = null!;
        public virtual DbSet<Subscribe> Subscribes { get; set; } = null!;
        public virtual DbSet<SubscribeType> SubscribeTypes { get; set; } = null!;
        public virtual DbSet<Teacher> Teachers { get; set; } = null!;
        public virtual DbSet<TeacherCircle> TeacherCircles { get; set; } = null!;
        public virtual DbSet<TeacherSallary> TeacherSallaries { get; set; } = null!;
        public virtual DbSet<TeacherSchedule> TeacherSchedules { get; set; } = null!;
        public virtual DbSet<TheHolyQuranSurah> TheHolyQuranSurahs { get; set; } = null!;
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
            });

            modelBuilder.Entity<ChallengeRole>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");
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
            });

            modelBuilder.Entity<ManagerReport>(entity =>
            {
                entity.ToTable("ManagerReport");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");
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
            });

            modelBuilder.Entity<ManagerSchedule>(entity =>
            {
                entity.ToTable("ManagerSchedule");

                entity.Property(e => e.AttendDate).HasColumnType("datetime");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.Property(e => e.ScheduleDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<ManagerStudent>(entity =>
            {
                entity.ToTable("ManagerStudent");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");
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
            });

            modelBuilder.Entity<Student>(entity =>
            {
                entity.ToTable("Student");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.HasOne(d => d.IdNavigation)
                    .WithOne(p => p.Student)
                    .HasForeignKey<Student>(d => d.Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Student_User");
            });

            modelBuilder.Entity<StudentPayment>(entity =>
            {
                entity.ToTable("StudentPayment");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.Property(e => e.PaymentDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<StudentTime>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");
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
            });

            modelBuilder.Entity<SubscribeType>(entity =>
            {
                entity.ToTable("SubscribeType");

                entity.Property(e => e.ArabPricePerHour).HasColumnType("decimal(5, 2)");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ForignPricePerHour).HasColumnType("decimal(5, 2)");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<Teacher>(entity =>
            {
                entity.ToTable("Teacher");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CircleId).HasMaxLength(100);

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.HasOne(d => d.IdNavigation)
                    .WithOne(p => p.Teacher)
                    .HasForeignKey<Teacher>(d => d.Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Teacher__Id__55009F39");
            });

            modelBuilder.Entity<TeacherCircle>(entity =>
            {
                entity.ToTable("TeacherCircle");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<TeacherSallary>(entity =>
            {
                entity.ToTable("TeacherSallary");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.Property(e => e.Month).HasColumnType("datetime");

                entity.Property(e => e.PayedAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<TeacherSchedule>(entity =>
            {
                entity.ToTable("TeacherSchedule");

                entity.Property(e => e.AttendDate).HasColumnType("datetime");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.Property(e => e.ScheduleDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<TheHolyQuranSurah>(entity =>
            {
                entity.ToTable("TheHolyQuranSurah");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");
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
