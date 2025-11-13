using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;

namespace SIMS.Controllers
{
    [Authorize]
    public class LecturerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public LecturerController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> MyCourses()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != "lecturer")
            {
                return Forbid();
            }

            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            if (lecturer == null)
            {
                return NotFound();
            }

            var courses = await _context.Courses
                .Where(c => c.LecturerId == lecturer.LecturerId)
                .Include(c => c.Subject)
                .Include(c => c.Semester)
                .Include(c => c.Major)
                .ToListAsync();

            return View(courses);
        }

        public async Task<IActionResult> ViewAssignedClasses()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != "lecturer")
            {
                return Forbid();
            }

            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            if (lecturer == null)
            {
                return NotFound();
            }

            var assignedClasses = await _context.StudentCourses
                .Where(sc => sc.Course.LecturerId == lecturer.LecturerId)
                .Include(sc => sc.Course)
                .ThenInclude(c => c.Subject)
                .Include(sc => sc.Course)
                .ThenInclude(c => c.Semester)
                .Include(sc => sc.Student)
                .ThenInclude(s => s.User)
                .Include(sc => sc.Student)
                .ThenInclude(s => s.Major)
                .GroupBy(sc => sc.Course)
                .Select(g => new
                {
                    Course = g.Key,
                    Students = g.Select(sc => sc.Student).ToList()
                })
                .ToListAsync();

            return View(assignedClasses);
        }

        public async Task<IActionResult> CourseStudents(int courseId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != "lecturer")
            {
                return Forbid();
            }

            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            if (lecturer == null)
            {
                return NotFound();
            }

            // Verify lecturer owns this course
            var course = await _context.Courses
                .Include(c => c.Subject)
                .Include(c => c.Semester)
                .FirstOrDefaultAsync(c => c.CourseId == courseId && c.LecturerId == lecturer.LecturerId);

            if (course == null)
            {
                return Forbid();
            }

            var students = await _context.StudentCourses
                .Where(sc => sc.CourseId == courseId)
                .Include(sc => sc.Student)
                .ThenInclude(s => s.User)
                .Include(sc => sc.Student)
                .ThenInclude(s => s.Major)
                .Select(sc => sc.Student)
                .ToListAsync();

            ViewBag.Course = course;
            return View(students);
        }

        public async Task<IActionResult> ViewProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var lecturer = await _context.Lecturers
                .Include(l => l.Department)
                .FirstOrDefaultAsync(l => l.UserId == user.Id);

            ViewBag.Lecturer = lecturer;
            return View(user);
        }
    }
}