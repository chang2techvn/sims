using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;

namespace SIMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, UserManager<User> userManager, ApplicationDbContext context)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;
        }

        public IActionResult Index()
        {
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Dashboard");
            }
            return RedirectToAction("Login", "Account");
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.UserName = user.Name;
            ViewBag.UserRole = user.Role;

            // Get dashboard statistics based on role
            switch (user.Role.ToLower())
            {
                case "admin":
                    ViewBag.TotalStudents = await _context.Students.CountAsync();
                    ViewBag.TotalLecturers = await _context.Lecturers.CountAsync();
                    ViewBag.TotalCourses = await _context.Courses.CountAsync();
                    ViewBag.TotalDepartments = await _context.Departments.CountAsync();
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
