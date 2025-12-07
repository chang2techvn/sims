using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SIMS.Models;
using SIMS.Services.Interfaces;

namespace SIMS.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly IStudentService _studentService;
        private readonly UserManager<User> _userManager;

        public StudentController(IStudentService studentService, UserManager<User> userManager)
        {
            _studentService = studentService;
            _userManager = userManager;
        }

        public async Task<IActionResult> MyCourses()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !string.Equals(user.Role, "student", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            var courses = await _studentService.GetStudentCoursesAsync(user.Id);
            return View(courses);
        }

        public async Task<IActionResult> CourseDetails(int courseId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !string.Equals(user.Role, "student", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            var (course, enrolledStudents) = await _studentService.GetCourseDetailsAsync(user.Id, courseId);

            if (course == null || enrolledStudents == null)
            {
                TempData["Error"] = "You are not enrolled in this course.";
                return RedirectToAction("MyCourses");
            }

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

            var (userProfile, student) = await _studentService.GetStudentProfileAsync(user.Id);

            if (userProfile == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Student = student;
            return View(userProfile);
        }




    }
}