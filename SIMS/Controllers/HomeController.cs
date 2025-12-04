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
        private const string DASHBOARD_STATS_CACHE_KEY = "DashboardStatistics";
        private readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(10);

        public HomeController(ILogger<HomeController> logger, UserManager<User> userManager, ApplicationDbContext context, IMemoryCache cache) : base(userManager)
        {
            _logger = logger;
            _context = context;
            _cache = cache;
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
            var user = await base._userManager.GetUserAsync(User);
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
                    var adminStats = await _cache.GetOrCreateAsync($"{DASHBOARD_STATS_CACHE_KEY}_Admin", async entry =>
                    {
                        entry.AbsoluteExpirationRelativeToNow = CACHE_DURATION;
                        return new
                        {
                            TotalStudents = await _context.Students.CountAsync(),
                            TotalLecturers = await _context.Lecturers.CountAsync(),
                            TotalAdmins = await _context.Admins.CountAsync(),
                            TotalUsers = await _context.Users.CountAsync()
                        };
                    });
                    ViewBag.TotalStudents = adminStats!.TotalStudents;
                    ViewBag.TotalLecturers = adminStats.TotalLecturers;
                    ViewBag.TotalAdmins = adminStats.TotalAdmins;
                    ViewBag.TotalUsers = adminStats.TotalUsers;
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
