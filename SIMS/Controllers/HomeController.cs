using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SIMS.Models;
using SIMS.Services.Interfaces;

namespace SIMS.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHomeService _homeService;
        private readonly IMemoryCache _cache;

        public HomeController(ILogger<HomeController> logger, UserManager<User> userManager, IHomeService homeService, IMemoryCache cache) : base(userManager)
        {
            _logger = logger;
            _homeService = homeService;
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
            var dashboardData = await _homeService.GetDashboardDataAsync(user.Id, user.Role);

            ViewBag.TotalStudents = dashboardData.TotalStudents;
            ViewBag.TotalLecturers = dashboardData.TotalLecturers;
            ViewBag.TotalAdmins = dashboardData.TotalAdmins;
            ViewBag.TotalUsers = dashboardData.TotalUsers;
            ViewBag.TotalCourses = dashboardData.TotalCourses;
            ViewBag.RecentEnrollments = dashboardData.RecentEnrollments;
            ViewBag.MyCourses = dashboardData.MyCourses;
            ViewBag.MyStudents = dashboardData.MyStudents;
            ViewBag.EnrolledCourses = dashboardData.EnrolledCourses;

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
