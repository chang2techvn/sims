using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIMS.Models
{
    public class Semester
    {
        [Key]
        public int SemesterId { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Year { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    }

    public class Subject
    {
        [Key]
        public int SubjectId { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    }

    public class Course
    {
        [Key]
        public int CourseId { get; set; }

        [Required]
        [StringLength(200)]
        public string CourseName { get; set; } = string.Empty;

        [ForeignKey("Subject")]
        public int SubjectId { get; set; }
        public virtual Subject Subject { get; set; } = null!;

        [ForeignKey("Semester")]
        public int SemesterId { get; set; }
        public virtual Semester Semester { get; set; } = null!;

        [ForeignKey("Major")]
        public int MajorId { get; set; }
        public virtual Major Major { get; set; } = null!;

        [ForeignKey("Lecturer")]
        public int LecturerId { get; set; }
        public virtual Lecturer Lecturer { get; set; } = null!;

        // Navigation properties
        public virtual ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
    }

    public class StudentCourse
    {
        [Key]
        [Column(Order = 0)]
        public int StudentId { get; set; }

        [Key]
        [Column(Order = 1)]
        public int CourseId { get; set; }

        public virtual Student Student { get; set; } = null!;
        public virtual Course Course { get; set; } = null!;
    }
}