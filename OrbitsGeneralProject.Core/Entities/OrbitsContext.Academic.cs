using Microsoft.EntityFrameworkCore;

namespace Orbits.GeneralProject.Core.Entities
{
    public partial class OrbitsContext
    {
        public virtual DbSet<AcademicCircle> AcademicCircles { get; set; } = null!;
        public virtual DbSet<AcademicCircleStudent> AcademicCircleStudents { get; set; } = null!;
        public virtual DbSet<AcademicManagerCircle> AcademicManagerCircles { get; set; } = null!;
        public virtual DbSet<AcademicManagerTeacher> AcademicManagerTeachers { get; set; } = null!;
        public virtual DbSet<AcademicManagerStudent> AcademicManagerStudents { get; set; } = null!;
        public virtual DbSet<AcademicReport> AcademicReports { get; set; } = null!;
        public virtual DbSet<AcademicSubject> AcademicSubjects { get; set; } = null!;

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AcademicSubject>(entity =>
            {
                entity.ToTable("AcademicSubject");

                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.CreatedAt).HasColumnType("datetime");
                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<AcademicCircle>(entity =>
            {
                entity.ToTable("AcademicCircle");

                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.CreatedAt).HasColumnType("datetime");
                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.HasOne(d => d.Teacher)
                    .WithMany(p => p.AcademicCircles)
                    .HasForeignKey(d => d.TeacherId)
                    .HasConstraintName("FK_AcademicCircle_Teacher");
            });

            modelBuilder.Entity<AcademicCircleStudent>(entity =>
            {
                entity.ToTable("AcademicCircleStudent");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");
                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.HasOne(d => d.AcademicCircle)
                    .WithMany(p => p.AcademicCircleStudents)
                    .HasForeignKey(d => d.AcademicCircleId)
                    .HasConstraintName("FK_AcademicCircleStudent_Circle");

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.AcademicCircleStudentStudents)
                    .HasForeignKey(d => d.StudentId)
                    .HasConstraintName("FK_AcademicCircleStudent_Student");
            });

            modelBuilder.Entity<AcademicManagerCircle>(entity =>
            {
                entity.ToTable("AcademicManagerCircle");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");
                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.HasOne(d => d.Manager)
                    .WithMany(p => p.AcademicManagerCircles)
                    .HasForeignKey(d => d.ManagerId)
                    .HasConstraintName("FK_AcademicManagerCircle_Manager");

                entity.HasOne(d => d.AcademicCircle)
                    .WithMany(p => p.AcademicManagerCircles)
                    .HasForeignKey(d => d.AcademicCircleId)
                    .HasConstraintName("FK_AcademicManagerCircle_Circle");
            });

            modelBuilder.Entity<AcademicManagerTeacher>(entity =>
            {
                entity.ToTable("AcademicManagerTeacher");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");
                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.HasOne(d => d.Manager)
                    .WithMany(p => p.AcademicManagerTeacherManagers)
                    .HasForeignKey(d => d.ManagerId)
                    .HasConstraintName("FK_AcademicManagerTeacher_Manager");

                entity.HasOne(d => d.Teacher)
                    .WithMany(p => p.AcademicManagerTeacherTeachers)
                    .HasForeignKey(d => d.TeacherId)
                    .HasConstraintName("FK_AcademicManagerTeacher_Teacher");
            });

            modelBuilder.Entity<AcademicManagerStudent>(entity =>
            {
                entity.ToTable("AcademicManagerStudent");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");
                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");

                entity.HasOne(d => d.Manager)
                    .WithMany(p => p.AcademicManagerStudentManagers)
                    .HasForeignKey(d => d.ManagerId)
                    .HasConstraintName("FK_AcademicManagerStudent_Manager");

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.AcademicManagerStudentStudents)
                    .HasForeignKey(d => d.StudentId)
                    .HasConstraintName("FK_AcademicManagerStudent_Student");
            });

            modelBuilder.Entity<AcademicReport>(entity =>
            {
                entity.ToTable("AcademicReport");

                entity.Property(e => e.ReportDate).HasColumnType("datetime");
                entity.Property(e => e.CreatedAt).HasColumnType("datetime");
                entity.Property(e => e.ModefiedAt).HasColumnType("datetime");
                entity.Property(e => e.LessonTitle).HasMaxLength(500);
                entity.Property(e => e.NextHomework).HasMaxLength(1000);
                entity.Property(e => e.TeacherNotes).HasMaxLength(2000);

                entity.HasOne(d => d.AcademicCircle)
                    .WithMany(p => p.AcademicReports)
                    .HasForeignKey(d => d.AcademicCircleId)
                    .HasConstraintName("FK_AcademicReport_Circle");

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.AcademicReportStudents)
                    .HasForeignKey(d => d.StudentId)
                    .HasConstraintName("FK_AcademicReport_Student");

                entity.HasOne(d => d.Teacher)
                    .WithMany(p => p.AcademicReportTeachers)
                    .HasForeignKey(d => d.TeacherId)
                    .HasConstraintName("FK_AcademicReport_Teacher");

                entity.HasOne(d => d.Subject)
                    .WithMany(p => p.AcademicReports)
                    .HasForeignKey(d => d.SubjectId)
                    .HasConstraintName("FK_AcademicReport_Subject");
            });
        }
    }
}
