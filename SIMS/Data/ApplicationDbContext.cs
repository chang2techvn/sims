using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SIMS.Models;

namespace SIMS.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Department> Departments { get; set; }
        public DbSet<Major> Majors { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Lecturer> Lecturers { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Semester> Semesters { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<StudentCourse> StudentCourses { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure composite key for StudentCourse
            builder.Entity<StudentCourse>()
                .HasKey(sc => new { sc.StudentId, sc.CourseId });

            // Configure relationships with NO ACTION to avoid cascade conflicts
            builder.Entity<StudentCourse>()
                .HasOne(sc => sc.Student)
                .WithMany(s => s.StudentCourses)
                .HasForeignKey(sc => sc.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StudentCourse>()
                .HasOne(sc => sc.Course)
                .WithMany(c => c.StudentCourses)
                .HasForeignKey(sc => sc.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Course relationships with NO ACTION to avoid cascade conflicts
            builder.Entity<Course>()
                .HasOne(c => c.Major)
                .WithMany(m => m.Courses)
                .HasForeignKey(c => c.MajorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Course>()
                .HasOne(c => c.Lecturer)
                .WithMany(l => l.Courses)
                .HasForeignKey(c => c.LecturerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Major relationships
            builder.Entity<Major>()
                .HasOne(m => m.Department)
                .WithMany(d => d.Majors)
                .HasForeignKey(m => m.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Lecturer relationships
            builder.Entity<Lecturer>()
                .HasOne(l => l.Department)
                .WithMany(d => d.Lecturers)
                .HasForeignKey(l => l.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Course relationships
            builder.Entity<Course>()
                .HasOne(c => c.Subject)
                .WithMany(s => s.Courses)
                .HasForeignKey(c => c.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Course>()
                .HasOne(c => c.Semester)
                .WithMany(s => s.Courses)
                .HasForeignKey(c => c.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-one relationships
            builder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId);

            builder.Entity<Lecturer>()
                .HasOne(l => l.User)
                .WithOne(u => u.Lecturer)
                .HasForeignKey<Lecturer>(l => l.UserId);

            builder.Entity<Admin>()
                .HasOne(a => a.User)
                .WithOne(u => u.Admin)
                .HasForeignKey<Admin>(a => a.UserId);

            // Seed data
            builder.Entity<Department>().HasData(
                new Department { DepartmentId = 1, Name = "Computer Science" },
                new Department { DepartmentId = 2, Name = "Business Administration" },
                new Department { DepartmentId = 3, Name = "Engineering" }
            );

            builder.Entity<Major>().HasData(
                new Major { MajorId = 1, Name = "Software Engineering", DepartmentId = 1 },
                new Major { MajorId = 2, Name = "Information Technology", DepartmentId = 1 },
                new Major { MajorId = 3, Name = "Marketing", DepartmentId = 2 },
                new Major { MajorId = 4, Name = "Mechanical Engineering", DepartmentId = 3 }
            );

            builder.Entity<Semester>().HasData(
                new Semester { SemesterId = 1, Name = "Fall", StartDate = new DateTime(2024, 9, 1), EndDate = new DateTime(2024, 12, 31) },
                new Semester { SemesterId = 2, Name = "Spring", StartDate = new DateTime(2025, 1, 15), EndDate = new DateTime(2025, 5, 30) }
            );

            builder.Entity<Subject>().HasData(
                new Subject { SubjectId = 1, Code = "CS101", Name = "Introduction to Programming" },
                new Subject { SubjectId = 2, Code = "CS201", Name = "Data Structures and Algorithms" },
                new Subject { SubjectId = 3, Code = "BU101", Name = "Business Fundamentals" }
            );
        }
    }
}