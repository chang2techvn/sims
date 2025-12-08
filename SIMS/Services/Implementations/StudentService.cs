using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;
using SIMS.Services.Interfaces;

namespace SIMS.Services.Implementations
{
    public class StudentService : GenericRepository<Student>, IStudentService
    {
        private readonly UserManager<User> _userManager;

        public StudentService(ApplicationDbContext context, UserManager<User> userManager)
            : base(context)
        {
            _userManager = userManager;
        }

        public async Task<List<Course>?> GetStudentCoursesAsync(string userId)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null)
                return null;

            // Load course IDs first, then query Courses with explicit includes
            var courseIds = await _context.StudentCourses
                .Where(sc => sc.StudentId == student.StudentId)
                .Select(sc => sc.CourseId)
                .ToListAsync();

            var courses = await _context.Courses
                .Where(c => courseIds.Contains(c.CourseId))
                .Include(c => c.Subject)
                .Include(c => c.Lecturer)
                    .ThenInclude(l => l.User)
                .Include(c => c.Semester)
                .Include(c => c.Major)
                .ToListAsync();

            return courses;
        }

        public async Task<(Course? Course, List<Student>? EnrolledStudents)> GetCourseDetailsAsync(string userId, int courseId)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null)
                return (null, null);

            // Check if student is enrolled in this course
            var isEnrolled = await _context.StudentCourses
                .AnyAsync(sc => sc.StudentId == student.StudentId && sc.CourseId == courseId);

            if (!isEnrolled)
                return (null, null);

            // Get course details
            var course = await _context.Courses
                .Include(c => c.Subject)
                .Include(c => c.Lecturer)
                    .ThenInclude(l => l.User)
                .Include(c => c.Semester)
                .Include(c => c.Major)
                .FirstOrDefaultAsync(c => c.CourseId == courseId);

            if (course == null)
                return (null, null);

            // Get enrolled students
            var enrolledStudents = await _context.StudentCourses
                .Where(sc => sc.CourseId == courseId)
                .Include(sc => sc.Student)
                    .ThenInclude(s => s.User)
                .Include(sc => sc.Student)
                    .ThenInclude(s => s.Major)
                .Select(sc => sc.Student)
                .ToListAsync();

            return (course, enrolledStudents);
        }

        public async Task<(User? User, Student? Student)> GetStudentProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return (null, null);

            var student = await _context.Students
                .Include(s => s.Major)
                .ThenInclude(m => m.Department)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            return (user, student);
        }
    }
}