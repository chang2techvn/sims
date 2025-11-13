using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;

namespace SIMS.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public StudentController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> MyCourses()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != "student")
            {
                return Forbid();
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (student == null)
            {
                return NotFound();
            }

            var courses = await _context.StudentCourses
                .Where(sc => sc.StudentId == student.StudentId)
                .Include(sc => sc.Course)
                .ThenInclude(c => c.Subject)
                .Include(sc => sc.Course)
                .ThenInclude(c => c.Lecturer)
                .ThenInclude(l => l.User)
                .Include(sc => sc.Course)
                .ThenInclude(c => c.Semester)
                .Select(sc => sc.Course)
                .ToListAsync();

            return View(courses);
        }

        public async Task<IActionResult> ViewProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var student = await _context.Students
                .Include(s => s.Major)
                .ThenInclude(m => m.Department)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            ViewBag.Student = student;
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Dashboard", "Home");
            }

            ViewBag.Majors = await _context.Majors.Include(m => m.Department).ToListAsync();
            return View();
        }

        public async Task<IActionResult> AvailableCourses()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != "student")
            {
                return Forbid();
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (student == null)
            {
                return NotFound();
            }

            // Get enrolled course IDs
            var enrolledCourseIds = await _context.StudentCourses
                .Where(sc => sc.StudentId == student.StudentId)
                .Select(sc => sc.CourseId)
                .ToListAsync();

            // Get available courses for the student's major
            var availableCourses = await _context.Courses
                .Where(c => c.MajorId == student.MajorId && !enrolledCourseIds.Contains(c.CourseId))
                .Include(c => c.Subject)
                .Include(c => c.Lecturer)
                .ThenInclude(l => l.User)
                .Include(c => c.Semester)
                .ToListAsync();

            return View(availableCourses);
        }

        [HttpPost]
        public async Task<IActionResult> EnrollInCourse(int courseId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != "student")
            {
                return Forbid();
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (student == null)
            {
                return NotFound();
            }

            // Check if already enrolled
            var existing = await _context.StudentCourses
                .FirstOrDefaultAsync(sc => sc.StudentId == student.StudentId && sc.CourseId == courseId);

            if (existing == null)
            {
                var studentCourse = new StudentCourse
                {
                    StudentId = student.StudentId,
                    CourseId = courseId
                };

                _context.StudentCourses.Add(studentCourse);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Successfully enrolled in the course!";
            }
            else
            {
                TempData["Error"] = "You are already enrolled in this course.";
            }

            return RedirectToAction("AvailableCourses");
        }
    }
}