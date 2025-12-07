using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http;
using SIMS.Data;
using SIMS.Models;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using OfficeOpenXml;
using System.Globalization;
using SIMS.Services.Interfaces;

namespace SIMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AdminController> _logger;
        private const string USER_STATS_CACHE_KEY = "UserStatistics";
        private readonly TimeSpan CACHE_DURATION = TimeSpan.FromHours(1);
        private readonly IDepartmentService _departmentService;
        private readonly IMajorService _majorService;
        private readonly ISemesterService _semesterService;
        private readonly ISubjectService _subjectService;
        private readonly ICourseService _courseService;
        private readonly IUserManagementService _userManagementService;
        private readonly IImportExportService _importExportService;
        private readonly IAdminViewService _adminViewService;
        private readonly IAdminDataService _adminDataService;

        private record UserStatistics(int StudentCount, int LecturerCount, int AdminCount);

        public AdminController(ApplicationDbContext context, UserManager<User> userManager, IMemoryCache cache, ILogger<AdminController> logger, IDepartmentService departmentService, IMajorService majorService, ISemesterService semesterService, ISubjectService subjectService, ICourseService courseService, IUserManagementService userManagementService, IImportExportService importExportService, IAdminViewService adminViewService, IAdminDataService adminDataService) : base(userManager)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _departmentService = departmentService;
            _majorService = majorService;
            _semesterService = semesterService;
            _subjectService = subjectService;
            _courseService = courseService;
            _userManagementService = userManagementService;
            _importExportService = importExportService;
            _adminViewService = adminViewService;
            _adminDataService = adminDataService;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Manage Departments
        public async Task<IActionResult> ManageDepartments(int page = 1, int pageSize = 10)
        {
            var viewModel = await _adminViewService.GetManageDepartmentsDataAsync(page, pageSize);

            ViewBag.CurrentPage = viewModel.CurrentPage;
            ViewBag.TotalPages = viewModel.TotalPages;
            ViewBag.PageSize = viewModel.PageSize;
            ViewBag.Action = "ManageDepartments";
            ViewBag.Controller = "Admin";

            return View(viewModel.Departments);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CreateDepartmentAjax(string name)
        {
            try
            {
                await _departmentService.CreateDepartmentAsync(name);
                return Json(new { success = true, message = "Department created successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditDepartment(int id, string name)
        {
            var department = await _departmentService.GetDepartmentByIdAsync(id);
            if (department == null)
            {
                return NotFound();
            }

            await _departmentService.UpdateDepartmentAsync(id, name);
            return RedirectToAction("ManageDepartments");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            try
            {
                await _departmentService.DeleteDepartmentAsync(id);
                return Json(new { success = true, message = "Department deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Manage Majors
        public async Task<IActionResult> ManageMajors(int page = 1, int pageSize = 10)
        {
            var viewModel = await _adminViewService.GetManageMajorsDataAsync(page, pageSize);

            ViewBag.Departments = viewModel.Departments;
            ViewBag.CurrentPage = viewModel.CurrentPage;
            ViewBag.TotalPages = viewModel.TotalPages;
            ViewBag.PageSize = viewModel.PageSize;
            ViewBag.Action = "ManageMajors";
            ViewBag.Controller = "Admin";

            return View(viewModel.Majors);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMajor(Major major)
        {
            if (ModelState.IsValid)
            {
                await _majorService.CreateMajorAsync(major.Name, major.DepartmentId);
                return RedirectToAction("ManageMajors");
            }
            return RedirectToAction("ManageMajors");
        }

        [HttpPost]
        public async Task<IActionResult> CreateMajorAjax([FromForm] string Name, [FromForm] int DepartmentId)
        {
            try
            {
                Console.WriteLine($"CreateMajorAjax called with: Name={Name}, DepartmentId={DepartmentId}");

                if (string.IsNullOrWhiteSpace(Name) || DepartmentId <= 0)
                {
                    return Json(new { success = false, message = "Invalid data provided." });
                }

                await _majorService.CreateMajorAsync(Name, DepartmentId);

                var major = await _context.Majors.FirstOrDefaultAsync(m => m.Name == Name.Trim() && m.DepartmentId == DepartmentId);

                return Json(new { success = true, message = "Major created successfully!", majorId = major?.MajorId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating major: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while creating the major." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateMajorAjax([FromForm] int MajorId, [FromForm] string Name, [FromForm] int DepartmentId)
        {
            try
            {
                Console.WriteLine($"UpdateMajorAjax called with: MajorId={MajorId}, Name={Name}, DepartmentId={DepartmentId}");

                if (MajorId <= 0 || string.IsNullOrWhiteSpace(Name) || DepartmentId <= 0)
                {
                    return Json(new { success = false, message = "Invalid data provided." });
                }

                await _majorService.UpdateMajorAsync(MajorId, Name, DepartmentId);

                var major = await _context.Majors.Include(m => m.Department).FirstOrDefaultAsync(m => m.MajorId == MajorId);

                return Json(new
                {
                    success = true,
                    message = "Major updated successfully!",
                    major = new
                    {
                        majorId = major?.MajorId,
                        name = major?.Name,
                        departmentName = major?.Department.Name
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating major: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while updating the major." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMajorAjax([FromForm] int MajorId)
        {
            try
            {
                Console.WriteLine($"DeleteMajorAjax called with: MajorId={MajorId}");

                if (MajorId <= 0)
                {
                    return Json(new { success = false, message = "Invalid major ID." });
                }

                await _majorService.DeleteMajorAsync(MajorId);

                return Json(new { success = true, message = "Major deleted successfully!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting major: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while deleting the major." });
            }
        }

        // Manage Semesters
        public async Task<IActionResult> ManageSemesters(int page = 1, int pageSize = 10)
        {
            var viewModel = await _adminViewService.GetManageSemestersDataAsync(page, pageSize);

            ViewBag.CurrentPage = viewModel.CurrentPage;
            ViewBag.TotalPages = viewModel.TotalPages;
            ViewBag.PageSize = viewModel.PageSize;
            ViewBag.Action = "ManageSemesters";
            ViewBag.Controller = "Admin";

            return View(viewModel.Semesters);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CreateSemesterAjax(string name, DateTime startDate, DateTime endDate)
        {
            try
            {
                Console.WriteLine($"CreateSemesterAjax called with name: {name}, startDate: {startDate}, endDate: {endDate}");
                await _semesterService.CreateSemesterAsync(name, startDate, endDate);
                Console.WriteLine("Semester created successfully");
                return Json(new { success = true, message = "Semester created successfully!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating semester: {ex.Message}");
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditSemester(int id, string name, DateTime startDate, DateTime endDate)
        {
            var semester = await _context.Semesters.FindAsync(id);
            if (semester == null)
            {
                return NotFound();
            }

            await _semesterService.UpdateSemesterAsync(id, name, startDate, endDate);
            return RedirectToAction("ManageSemesters");
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> EditSemesterAjax(int id, string name, DateTime startDate, DateTime endDate)
        {
            try
            {
                await _semesterService.UpdateSemesterAsync(id, name, startDate, endDate);
                return Json(new { success = true, message = "Semester updated successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSemester(int id)
        {
            try
            {
                await _semesterService.DeleteSemesterAsync(id);
                return Json(new { success = true, message = "Semester deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Manage Subjects
        public async Task<IActionResult> ManageSubjects(int page = 1, int pageSize = 10)
        {
            var viewModel = await _adminViewService.GetManageSubjectsDataAsync(page, pageSize);

            ViewBag.CurrentPage = viewModel.CurrentPage;
            ViewBag.TotalPages = viewModel.TotalPages;
            ViewBag.PageSize = viewModel.PageSize;
            ViewBag.Action = "ManageSubjects";
            ViewBag.Controller = "Admin";

            return View(viewModel.Subjects);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubject(Subject subject)
        {
            if (ModelState.IsValid)
            {
                await _subjectService.CreateSubjectAsync(subject.Name, subject.Code);
                return RedirectToAction("ManageSubjects");
            }
            return RedirectToAction("ManageSubjects");
        }

        [HttpPost]
        public async Task<IActionResult> EditSubject(int id, string code, string name)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
            {
                return NotFound();
            }

            await _subjectService.UpdateSubjectAsync(id, name, code);
            return RedirectToAction("ManageSubjects");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
            {
                return NotFound();
            }

            await _subjectService.DeleteSubjectAsync(id);
            return RedirectToAction("ManageSubjects");
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteSubjectAjax(int id)
        {
            try
            {
                await _subjectService.DeleteSubjectAsync(id);
                return Json(new { success = true, message = "Subject deleted successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting subject: " + ex.Message });
            }
        }

        // Manage Courses
        public async Task<IActionResult> ManageCourses(int page = 1, int pageSize = 10)
        {
            var viewModel = await _adminViewService.GetManageCoursesDataAsync(page, pageSize);

            ViewBag.Subjects = viewModel.Subjects;
            ViewBag.Semesters = viewModel.Semesters;
            ViewBag.Majors = viewModel.Majors;
            ViewBag.Lecturers = viewModel.Lecturers;

            ViewBag.CurrentPage = viewModel.CurrentPage;
            ViewBag.TotalPages = viewModel.TotalPages;
            ViewBag.PageSize = viewModel.PageSize;
            ViewBag.Action = "ManageCourses";
            ViewBag.Controller = "Admin";

            return View(viewModel.Courses);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCourse([FromForm] int SubjectId, [FromForm] int SemesterId, [FromForm] int MajorId, [FromForm] int LecturerId, [FromForm] string CourseName)
        {
            try
            {
                // Debug logging
                Console.WriteLine($"CreateCourse called with: CourseName={CourseName}, SubjectId={SubjectId}, SemesterId={SemesterId}, MajorId={MajorId}, LecturerId={LecturerId}");

                // Validate required fields
                if (string.IsNullOrWhiteSpace(CourseName))
                {
                    return Json(new { success = false, message = "Course name is required" });
                }

                if (SubjectId <= 0 || SemesterId <= 0 || MajorId <= 0 || LecturerId <= 0)
                {
                    return Json(new { success = false, message = "All dropdown selections are required" });
                }

                // Check if entities exist
                var subject = await _context.Subjects.FindAsync(SubjectId);
                var semester = await _context.Semesters.FindAsync(SemesterId);
                var major = await _context.Majors.FindAsync(MajorId);
                var lecturer = await _context.Lecturers.FindAsync(LecturerId);

                if (subject == null || semester == null || major == null || lecturer == null)
                {
                    return Json(new { success = false, message = "Invalid selection. Please refresh the page and try again." });
                }

                var course = new Course
                {
                    CourseName = CourseName.Trim(),
                    SubjectId = SubjectId,
                    SemesterId = SemesterId,
                    MajorId = MajorId,
                    LecturerId = LecturerId
                };

                await _courseService.CreateCourseAsync(course);

                // Reload the course with navigation properties
                var newCourse = await _context.Courses
                    .Include(c => c.Subject)
                    .Include(c => c.Semester)
                    .Include(c => c.Major)
                    .Include(c => c.Lecturer)
                    .ThenInclude(l => l.User)
                    .FirstOrDefaultAsync(c => c.CourseId == course.CourseId);

                return Json(new {
                    success = true,
                    message = "Course created successfully",
                    course = new {
                        courseId = newCourse?.CourseId,
                        courseName = newCourse?.CourseName,
                        subjectName = newCourse?.Subject?.Name,
                        subjectCode = newCourse?.Subject?.Code,
                        semesterName = newCourse?.Semester?.Name,
                        semesterPeriod = $"{newCourse?.Semester?.StartDate:MMM dd, yyyy} - {newCourse?.Semester?.EndDate:MMM dd, yyyy}",
                        majorName = newCourse?.Major?.Name,
                        lecturerName = newCourse?.Lecturer?.User?.Name
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in CreateCourse: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }        [HttpPost]
        public async Task<IActionResult> UpdateCourse([FromForm] int CourseId, [FromForm] string CourseName, [FromForm] int SubjectId, [FromForm] int SemesterId, [FromForm] int MajorId, [FromForm] int LecturerId)
        {
            try
            {
                var course = await _context.Courses.FindAsync(CourseId);
                if (course == null)
                {
                    return Json(new { success = false, message = "Course not found" });
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(CourseName))
                {
                    return Json(new { success = false, message = "Course name is required" });
                }

                if (SubjectId <= 0 || SemesterId <= 0 || MajorId <= 0 || LecturerId <= 0)
                {
                    return Json(new { success = false, message = "All dropdown selections are required" });
                }

                // Check if entities exist
                var subject = await _context.Subjects.FindAsync(SubjectId);
                var semester = await _context.Semesters.FindAsync(SemesterId);
                var major = await _context.Majors.FindAsync(MajorId);
                var lecturer = await _context.Lecturers.FindAsync(LecturerId);

                if (subject == null || semester == null || major == null || lecturer == null)
                {
                    return Json(new { success = false, message = "Invalid selection. Please refresh the page and try again." });
                }

                course.CourseName = CourseName.Trim();
                course.SubjectId = SubjectId;
                course.SemesterId = SemesterId;
                course.MajorId = MajorId;
                course.LecturerId = LecturerId;

                await _courseService.UpdateCourseAsync(CourseId, course);

                // Reload with navigation properties
                var updatedCourse = await _context.Courses
                    .Include(c => c.Subject)
                    .Include(c => c.Semester)
                    .Include(c => c.Major)
                    .Include(c => c.Lecturer)
                    .ThenInclude(l => l.User)
                    .FirstOrDefaultAsync(c => c.CourseId == CourseId);

                return Json(new {
                    success = true,
                    message = "Course updated successfully",
                    course = new {
                        courseId = updatedCourse?.CourseId,
                        courseName = updatedCourse?.CourseName,
                        subjectName = updatedCourse?.Subject?.Name,
                        subjectCode = updatedCourse?.Subject?.Code,
                        semesterName = updatedCourse?.Semester?.Name,
                        semesterPeriod = $"{updatedCourse?.Semester?.StartDate:MMM dd, yyyy} - {updatedCourse?.Semester?.EndDate:MMM dd, yyyy}",
                        majorName = updatedCourse?.Major?.Name,
                        lecturerName = updatedCourse?.Lecturer?.User?.Name
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                await _courseService.DeleteCourseAsync(id);
                return Json(new { success = true, message = "Course deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Manage Users
        public async Task<IActionResult> ManageUsers(int page = 1, int pageSize = 10)
        {
            var viewModel = await _adminViewService.GetManageUsersDataAsync(page, pageSize);

            ViewBag.Roles = viewModel.Roles;
            ViewBag.Departments = viewModel.Departments;
            ViewBag.Majors = viewModel.Majors;

            ViewBag.CurrentPage = viewModel.CurrentPage;
            ViewBag.TotalPages = viewModel.TotalPages;
            ViewBag.PageSize = viewModel.PageSize;
            ViewBag.Action = "ManageUsers";
            ViewBag.Controller = "Admin";

            return View(viewModel.Users);
        }

        [HttpPost]
        public async Task<IActionResult> ImportUsers(IFormFile file, bool skipDuplicates = true)
        {
            var result = await _importExportService.ImportUsersAsync(file, skipDuplicates);
            
            return Json(new { 
                success = result.Success, 
                message = result.Message,
                importedCount = result.ImportedCount,
                skippedCount = result.SkippedCount,
                errors = result.Errors
            });
        }



        [HttpPost]
        public async Task<IActionResult> AddUser(string Name, string Email, string Password, string Role, 
            string? StudentCode, DateTime? DateOfBirth, string? Phone, string? Gender, string? Address)
        {
            try
            {
                var result = await _userManagementService.AddUserAsync(Name, Email, Password, Role, StudentCode, DateOfBirth, Phone, Gender, Address);
                
                if (result.success)
                {
                    return Json(new { success = true });
                }
                
                return Json(new { success = false, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUser(string id)
        {
            var data = await _adminDataService.GetUserDataAsync(id);
            if (data == null)
                return Json(new { success = false });

            return Json(data);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUser(string Id, string Name, string Role, 
            string? StudentCode, DateTime? DateOfBirth, string? Phone, string? Gender, string? Address, string? Password, string? Avatar)
        {
            try
            {
                var result = await _userManagementService.UpdateUserAsync(Id, Name, Role, StudentCode, DateOfBirth, Phone, Gender, Address, Password, Avatar);
                
                if (result.success)
                {
                    return Json(new { success = true });
                }
                
                return Json(new { success = false, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Get actions for edit modals
        public async Task<IActionResult> GetCourse(int id)
        {
            var data = await _adminDataService.GetCourseDataAsync(id);
            if (data == null)
                return Json(new { success = false });

            return Json(data);
        }

        public async Task<IActionResult> GetSubject(int id)
        {
            var data = await _adminDataService.GetSubjectDataAsync(id);
            if (data == null)
                return Json(new { success = false });

            return Json(data);
        }

        public async Task<IActionResult> GetDepartment(int id)
        {
            var data = await _adminDataService.GetDepartmentDataAsync(id);
            if (data == null)
                return Json(new { success = false });

            return Json(data);
        }

        public async Task<IActionResult> GetMajor(int id)
        {
            var data = await _adminDataService.GetMajorDataAsync(id);
            if (data == null)
                return Json(new { success = false });

            return Json(data);
        }

        public async Task<IActionResult> GetSemester(int id)
        {
            var data = await _adminDataService.GetSemesterDataAsync(id);
            if (data == null)
                return Json(new { success = false });

            return Json(data);
        }

        public async Task<IActionResult> GetStudentCourse(int studentId, int courseId)
        {
            var data = await _adminDataService.GetStudentCourseDataAsync(studentId, courseId);
            if (data == null)
            {
                _logger.LogWarning($"StudentCourse not found for studentId: {studentId}, courseId: {courseId}");
                return Json(new { success = false });
            }

            return Json(data);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var result = await _userManagementService.DeleteUserAsync(id);
                
                if (result.success)
                {
                    return Json(new { success = true });
                }
                    
                return Json(new { success = false, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Assign Students to Courses
        public async Task<IActionResult> AssignStudentToCourse(int page = 1, int pageSize = 10)
        {
            var viewModel = await _adminViewService.GetAssignStudentToCourseDataAsync(page, pageSize);

            ViewBag.Students = viewModel.Students;
            ViewBag.Courses = viewModel.Courses;

            ViewBag.CurrentPage = viewModel.CurrentPage;
            ViewBag.TotalPages = viewModel.TotalPages;
            ViewBag.PageSize = viewModel.PageSize;
            ViewBag.Action = "AssignStudentToCourse";
            ViewBag.Controller = "Admin";

            return View(viewModel.Assignments);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AssignStudentToCoursePost(int studentId, int courseId)
        {
            try
            {
                var (success, message) = await _userManagementService.AssignStudentToCourseAsync(studentId, courseId);
                return Json(new { success, message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RemoveStudentFromCourse(int studentId, int courseId)
        {
            try
            {
                var (success, message) = await _userManagementService.RemoveStudentFromCourseAsync(studentId, courseId);
                return Json(new { success, message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        public async Task<IActionResult> UpdateStudentCourseAssignment(int currentStudentId, int currentCourseId, int newStudentId, int newCourseId)
        {
            try
            {
                var (success, message) = await _userManagementService.UpdateStudentCourseAssignmentAsync(currentStudentId, currentCourseId, newStudentId, newCourseId);
                return Json(new { success, message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> GetCourseStudents(int courseId)
        {
            try
            {
                var data = await _adminDataService.GetCourseStudentsDataAsync(courseId);
                if (data != null)
                {
                    return Json(data);
                }
                else
                {
                    return Json(new { success = false, message = "Course not found" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}