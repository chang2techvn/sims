using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;
using SIMS.Models.ViewModels;

namespace SIMS.Controllers
{
    [Authorize]
    public class LecturerController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public LecturerController(ApplicationDbContext context, UserManager<User> userManager) : base(userManager)
        {
            _context = context;
        }

        public async Task<IActionResult> MyCourses()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !string.Equals(user.Role, "Lecturer", StringComparison.OrdinalIgnoreCase))
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

            return View(courseViewModels);
        }

        public async Task<IActionResult> CourseStudents(int courseId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || !string.Equals(user.Role, "Lecturer", StringComparison.OrdinalIgnoreCase))
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
                    .Include(c => c.Major)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId && c.LecturerId == lecturer.LecturerId);

                if (course == null)
                {
                    TempData["Error"] = "Course not found or you don't have permission to view it.";
                    return RedirectToAction("MyCourses");
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
            catch (Exception ex)
            {
                // Log the exception (you might want to use a proper logging framework)
                TempData["Error"] = $"An error occurred while loading course students: {ex.Message}";
                return RedirectToAction("MyCourses");
            }
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