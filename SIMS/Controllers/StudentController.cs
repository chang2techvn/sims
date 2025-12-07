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
            if (user == null || !string.Equals(user.Role, "student", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (student == null)
            {
                return NotFound();
            }

            // Load course IDs first, then query Courses with explicit includes to ensure nested navigation properties are populated
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

            return View(courses);
        }

        public async Task<IActionResult> CourseDetails(int courseId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !string.Equals(user.Role, "student", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (student == null)
            {
                return NotFound();
            }

            // Check if student is enrolled in this course
            var isEnrolled = await _context.StudentCourses
                .AnyAsync(sc => sc.StudentId == student.StudentId && sc.CourseId == courseId);

            if (!isEnrolled)
            {
                TempData["Error"] = "You are not enrolled in this course.";
                return RedirectToAction("MyCourses");
            }

            // Get course details
            var course = await _context.Courses
                .Include(c => c.Subject)
                .Include(c => c.Lecturer)
                    .ThenInclude(l => l.User)
                .Include(c => c.Semester)
                .Include(c => c.Major)
                .FirstOrDefaultAsync(c => c.CourseId == courseId);

            if (course == null)
            {
                return NotFound();
            }

            // Get enrolled students
            var enrolledStudents = await _context.StudentCourses
                .Where(sc => sc.CourseId == courseId)
                .Include(sc => sc.Student)
                    .ThenInclude(s => s.User)
                .Include(sc => sc.Student)
                    .ThenInclude(s => s.Major)
                .Select(sc => sc.Student)
                .ToListAsync();

            // Pagination
            int page = 1;
            int pageSize = 10;
            var queryString = HttpContext.Request.QueryString.ToString();
            if (!string.IsNullOrEmpty(queryString))
            {
                var pageParam = HttpContext.Request.Query["page"].ToString();
                if (!string.IsNullOrEmpty(pageParam) && int.TryParse(pageParam, out int p))
                {
                    page = p;
                }
            }

            var totalStudents = enrolledStudents.Count;
            var pagedStudents = enrolledStudents.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Course = course;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalStudents / pageSize);
            ViewBag.PageSize = pageSize;
            ViewBag.Action = "CourseDetails";
            ViewBag.Controller = "Student";
            ViewBag.RouteValues = new Dictionary<string, object> { { "courseId", courseId } };

            return View(pagedStudents);
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




    }
}