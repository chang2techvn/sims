using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SIMS.Data;
using SIMS.Models;

namespace SIMS.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public HomeController(ILogger<HomeController> logger, UserManager<User> userManager, ApplicationDbContext context, IMemoryCache cache) : base(userManager)
        {
            _logger = logger;
            _context = context;
            _cache = cache;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity!.IsAuthenticated)
            {
                var user = await base._userManager.GetUserAsync(User);
                if (user != null)
                {
                    switch (user.Role.ToLower())
                    {
                        case "admin":
                            return RedirectToAction("Dashboard");
                        case "lecturer":
                            return RedirectToAction("MyCourses", "Lecturer");
                        case "student":
                            return RedirectToAction("MyCourses", "Student");
                        default:
                            return RedirectToAction("Dashboard");
                    }
                }
            }
            return RedirectToAction("Login", "Account");
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var user = await base._userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Redirect non-admin users to their respective MyCourses pages
            if (user.Role.ToLower() != "admin")
            {
                switch (user.Role.ToLower())
                {
                    case "lecturer":
                        return RedirectToAction("MyCourses", "Lecturer");
                    case "student":
                        return RedirectToAction("MyCourses", "Student");
                    default:
                        return RedirectToAction("MyCourses", "Student"); // fallback
                }
            }

            ViewBag.UserName = user.Name;
            ViewBag.UserRole = user.Role.ToLower();

            // Get dashboard statistics based on role
            switch (user.Role.ToLower())
            {
                case "admin":
                    ViewBag.TotalStudents = await _context.Students.CountAsync();
                    ViewBag.TotalLecturers = await _context.Lecturers.CountAsync();
                    ViewBag.TotalAdmins = await _context.Admins.CountAsync();
                    ViewBag.TotalUsers = await _context.Users.CountAsync();
                    ViewBag.TotalCourses = await _context.Courses.CountAsync();

                    // Get recent enrollment activities
                    var recentEnrollments = await _context.StudentCourses
                        .Include(sc => sc.Student)
                        .ThenInclude(s => s.User)
                        .Include(sc => sc.Course)
                        .OrderByDescending(sc => sc.EnrollmentDate)
                        .Take(10)
                        .Select(sc => new
                        {
                            StudentName = sc.Student.User.Name,
                            CourseName = sc.Course.CourseName,
                            EnrollmentDate = sc.EnrollmentDate
                        })
                        .ToListAsync();
                    ViewBag.RecentEnrollments = recentEnrollments;
                    break;
                case "lecturer":
                    var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
                    if (lecturer != null)
                    {
                        ViewBag.MyCourses = await _context.Courses.Where(c => c.LecturerId == lecturer.LecturerId).CountAsync();
                        ViewBag.MyStudents = await _context.StudentCourses
                            .Where(sc => sc.Course.LecturerId == lecturer.LecturerId)
                            .Select(sc => sc.StudentId)
                            .Distinct()
                            .CountAsync();
                    }
                    break;
                case "student":
                    var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
                    if (student != null)
                    {
                        ViewBag.MyCourses = await _context.StudentCourses.Where(sc => sc.StudentId == student.StudentId).CountAsync();
                    }
                    break;
            }

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
