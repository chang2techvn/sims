using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;
using SIMS.Models.ViewModels;
using SIMS.Services.Interfaces;

namespace SIMS.Services.Implementations
{
    public class LecturerService : ILecturerService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public LecturerService(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<List<LecturerCourseViewModel>?> GetLecturerCoursesAsync(string userId)
        {
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == userId);
            if (lecturer == null)
                return null;

            var courses = await _context.Courses
                .Where(c => c.LecturerId == lecturer.LecturerId)
                .Include(c => c.Subject)
                .Include(c => c.Semester)
                .Include(c => c.Major)
                .ToListAsync();

            var courseViewModels = new List<LecturerCourseViewModel>();

            foreach (var course in courses)
            {
                var studentCount = await _context.StudentCourses
                    .CountAsync(sc => sc.CourseId == course.CourseId);

                courseViewModels.Add(new LecturerCourseViewModel
                {
                    Course = course,
                    StudentCount = studentCount
                });
            }

            return courseViewModels;
        }

        public async Task<(Course? Course, List<Student>? Students)> GetCourseStudentsAsync(string userId, int courseId)
        {
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == userId);
            if (lecturer == null)
                return (null, null);

            // Verify lecturer owns this course
            var course = await _context.Courses
                .Include(c => c.Subject)
                .Include(c => c.Semester)
                .Include(c => c.Major)
                .FirstOrDefaultAsync(c => c.CourseId == courseId && c.LecturerId == lecturer.LecturerId);

            if (course == null)
                return (null, null);

            var students = await _context.StudentCourses
                .Where(sc => sc.CourseId == courseId)
                .Include(sc => sc.Student)
                .ThenInclude(s => s.User)
                .Include(sc => sc.Student)
                .ThenInclude(s => s.Major)
                .Select(sc => sc.Student)
                .ToListAsync();

            return (course, students);
        }

        public async Task<(User? User, Lecturer? Lecturer)> GetLecturerProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return (null, null);

            var lecturer = await _context.Lecturers
                .Include(l => l.Department)
                .FirstOrDefaultAsync(l => l.UserId == userId);

            return (user, lecturer);
        }
    }
}