using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SIMS.Models;
using SIMS.Services.Interfaces;

namespace SIMS.Controllers
{
    [Authorize]
    public class LecturerController : BaseController
    {
        private readonly ILecturerService _lecturerService;

        public LecturerController(ILecturerService lecturerService, UserManager<User> userManager) : base(userManager)
        {
            _lecturerService = lecturerService;
        }

        public async Task<IActionResult> MyCourses()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !string.Equals(user.Role, "Lecturer", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            var courseViewModels = await _lecturerService.GetLecturerCoursesAsync(user.Id);
            if (courseViewModels == null)
            {
                return NotFound();
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

                var (course, students) = await _lecturerService.GetCourseStudentsAsync(user.Id, courseId);

                if (course == null || students == null)
                {
                    TempData["Error"] = "Course not found or you don't have permission to view it.";
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

                var totalStudents = students.Count;
                var pagedStudents = students.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                ViewBag.Course = course;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalStudents / pageSize);
                ViewBag.PageSize = pageSize;
                ViewBag.Action = "CourseStudents";
                ViewBag.Controller = "Lecturer";
                ViewBag.RouteValues = new Dictionary<string, object> { { "courseId", courseId } };

                return View(pagedStudents);
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

            var (userProfile, lecturer) = await _lecturerService.GetLecturerProfileAsync(user.Id);

            if (userProfile == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Lecturer = lecturer;
            return View(userProfile);
        }
    }
}