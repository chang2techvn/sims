using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIMS.Models
{
    public class Department
    {
        [Key]
        public int DepartmentId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<Major> Majors { get; set; } = new List<Major>();
        public virtual ICollection<Lecturer> Lecturers { get; set; } = new List<Lecturer>();
    }

    public class Major
    {
        [Key]
        public int MajorId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [ForeignKey("Department")]
        public int DepartmentId { get; set; }
        public virtual Department Department { get; set; } = null!;

        // Navigation properties
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    }

    public class Student
    {
        [Key]
        public int StudentId { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; } = string.Empty;
        public virtual User User { get; set; } = null!;

        [ForeignKey("Major")]
        public int MajorId { get; set; }
        public virtual Major Major { get; set; } = null!;

        // Navigation properties
        public virtual ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
    }

    public class Lecturer
    {
        [Key]
        public int LecturerId { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; } = string.Empty;
        public virtual User User { get; set; } = null!;

        [ForeignKey("Department")]
        public int DepartmentId { get; set; }
        public virtual Department Department { get; set; } = null!;

        // Navigation properties
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    }

    public class Admin
    {
        [Key]
        public int AdminId { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; } = string.Empty;
        public virtual User User { get; set; } = null!;
    }
}