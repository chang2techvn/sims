using SIMS.Data;
using SIMS.Models;
using SIMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace SIMS.Services.Implementations
{
    public class CourseService : ICourseService
    {
        private readonly ApplicationDbContext _context;

        public CourseService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Course>> GetAllCoursesAsync()
        {
            return await _context.Courses
                .Include(c => c.Subject)
                .Include(c => c.Semester)
                .Include(c => c.Major)
                .Include(c => c.Lecturer)
                .ThenInclude(l => l.User)
                .ToListAsync();
        }

        public async Task CreateCourseAsync(Course course)
        {
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCourseAsync(int id, Course course)
        {
            var existingCourse = await _context.Courses.FindAsync(id);
            if (existingCourse != null)
            {
                existingCourse.SubjectId = course.SubjectId;
                existingCourse.SemesterId = course.SemesterId;
                existingCourse.MajorId = course.MajorId;
                existingCourse.LecturerId = course.LecturerId;
                existingCourse.CourseName = course.CourseName;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteCourseAsync(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Course?> GetCourseAsync(int id)
        {
            return await _context.Courses
                .Include(c => c.Subject)
                .Include(c => c.Semester)
                .Include(c => c.Major)
                .Include(c => c.Lecturer)
                .FirstOrDefaultAsync(c => c.CourseId == id);
        }
    }
}