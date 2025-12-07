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

        private record UserStatistics(int StudentCount, int LecturerCount, int AdminCount);

        public AdminController(ApplicationDbContext context, UserManager<User> userManager, IMemoryCache cache, ILogger<AdminController> logger) : base(userManager)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Manage Departments
        public async Task<IActionResult> ManageDepartments(int page = 1, int pageSize = 10)
        {
            var departments = await _context.Departments.ToListAsync();
            var totalDepartments = departments.Count;
            var pagedDepartments = departments.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalDepartments / pageSize);
            ViewBag.PageSize = pageSize;
            ViewBag.Action = "ManageDepartments";
            ViewBag.Controller = "Admin";

            return View(pagedDepartments);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CreateDepartmentAjax(string name)
        {
            try
            {
                var department = new Department { Name = name };
                _context.Departments.Add(department);
                await _context.SaveChangesAsync();
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
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound();
            }

            department.Name = name;
            await _context.SaveChangesAsync();
            return RedirectToAction("ManageDepartments");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            try
            {
                var department = await _context.Departments.FindAsync(id);
                if (department == null)
                {
                    return Json(new { success = false, message = "Department not found" });
                }

                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();

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
            var majors = await _context.Majors.Include(m => m.Department).ToListAsync();
            var totalMajors = majors.Count;
            var pagedMajors = majors.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Departments = await _context.Departments.ToListAsync();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalMajors / pageSize);
            ViewBag.PageSize = pageSize;
            ViewBag.Action = "ManageMajors";
            ViewBag.Controller = "Admin";

            return View(pagedMajors);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMajor(Major major)
        {
            if (ModelState.IsValid)
            {
                _context.Majors.Add(major);
                await _context.SaveChangesAsync();
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

                var department = await _context.Departments.FindAsync(DepartmentId);
                if (department == null)
                {
                    return Json(new { success = false, message = "Department not found." });
                }

                var major = new Major
                {
                    Name = Name.Trim(),
                    DepartmentId = DepartmentId
                };

                _context.Majors.Add(major);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Major created successfully!", majorId = major.MajorId });
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

                var major = await _context.Majors.Include(m => m.Department).FirstOrDefaultAsync(m => m.MajorId == MajorId);
                if (major == null)
                {
                    return Json(new { success = false, message = "Major not found." });
                }

                var department = await _context.Departments.FindAsync(DepartmentId);
                if (department == null)
                {
                    return Json(new { success = false, message = "Department not found." });
                }

                major.Name = Name.Trim();
                major.DepartmentId = DepartmentId;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Major updated successfully!",
                    major = new
                    {
                        majorId = major.MajorId,
                        name = major.Name,
                        departmentName = major.Department.Name
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

                var major = await _context.Majors
                    .Include(m => m.Students)
                    .Include(m => m.Courses)
                    .FirstOrDefaultAsync(m => m.MajorId == MajorId);

                if (major == null)
                {
                    return Json(new { success = false, message = "Major not found." });
                }

                // Check if major has students or courses
                if (major.Students.Any() || major.Courses.Any())
                {
                    return Json(new { success = false, message = "Cannot delete major that has students or courses assigned." });
                }

                _context.Majors.Remove(major);
                await _context.SaveChangesAsync();

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
            var semesters = await _context.Semesters.ToListAsync();
            var totalSemesters = semesters.Count;
            var pagedSemesters = semesters.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalSemesters / pageSize);
            ViewBag.PageSize = pageSize;
            ViewBag.Action = "ManageSemesters";
            ViewBag.Controller = "Admin";

            return View(pagedSemesters);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CreateSemesterAjax(string name, string year)
        {
            try
            {
                Console.WriteLine($"CreateSemesterAjax called with name: {name}, year: {year}");
                var semester = new Semester { Name = name, Year = year };
                _context.Semesters.Add(semester);
                await _context.SaveChangesAsync();
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
        public async Task<IActionResult> EditSemester(int id, string name, string year)
        {
            var semester = await _context.Semesters.FindAsync(id);
            if (semester == null)
            {
                return NotFound();
            }

            semester.Name = name;
            semester.Year = year;
            await _context.SaveChangesAsync();
            return RedirectToAction("ManageSemesters");
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> EditSemesterAjax(int id, string name, string year)
        {
            try
            {
                var semester = await _context.Semesters.FindAsync(id);
                if (semester == null)
                {
                    return Json(new { success = false, message = "Semester not found." });
                }

                semester.Name = name;
                semester.Year = year;
                await _context.SaveChangesAsync();
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
                var semester = await _context.Semesters.FindAsync(id);
                if (semester == null)
                {
                    return Json(new { success = false, message = "Semester not found" });
                }

                _context.Semesters.Remove(semester);
                await _context.SaveChangesAsync();

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
            var subjects = await _context.Subjects.ToListAsync();
            var totalSubjects = subjects.Count;
            var pagedSubjects = subjects.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalSubjects / pageSize);
            ViewBag.PageSize = pageSize;
            ViewBag.Action = "ManageSubjects";
            ViewBag.Controller = "Admin";

            return View(pagedSubjects);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubject(Subject subject)
        {
            if (ModelState.IsValid)
            {
                _context.Subjects.Add(subject);
                await _context.SaveChangesAsync();
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

            subject.Code = code;
            subject.Name = name;
            await _context.SaveChangesAsync();
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

            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();
            return RedirectToAction("ManageSubjects");
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteSubjectAjax(int id)
        {
            try
            {
                var subject = await _context.Subjects.FindAsync(id);
                if (subject == null)
                {
                    return Json(new { success = false, message = "Subject not found." });
                }

                _context.Subjects.Remove(subject);
                await _context.SaveChangesAsync();

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
            var courses = await _context.Courses
                .Include(c => c.Subject)
                .Include(c => c.Semester)
                .Include(c => c.Major)
                .Include(c => c.Lecturer)
                .ThenInclude(l => l.User)
                .ToListAsync();

            var totalCourses = courses.Count;
            var pagedCourses = courses.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            
            ViewBag.Subjects = await _context.Subjects.ToListAsync();
            ViewBag.Semesters = await _context.Semesters.ToListAsync();
            ViewBag.Majors = await _context.Majors.ToListAsync();
            ViewBag.Lecturers = await _context.Lecturers.Include(l => l.User).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCourses / pageSize);
            ViewBag.PageSize = pageSize;
            ViewBag.Action = "ManageCourses";
            ViewBag.Controller = "Admin";
            
            return View(pagedCourses);
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

                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

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
                        semesterYear = newCourse?.Semester?.Year,
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

                await _context.SaveChangesAsync();

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
                        semesterYear = updatedCourse?.Semester?.Year,
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
                var course = await _context.Courses.FindAsync(id);
                if (course == null)
                {
                    return Json(new { success = false, message = "Course not found" });
                }

                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();

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
            var users = await base._userManager.Users.ToListAsync();
            var totalUsers = users.Count;
            var pagedUsers = users.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize);
            ViewBag.PageSize = pageSize;
            ViewBag.Action = "ManageUsers";
            ViewBag.Controller = "Admin";

            return View(pagedUsers);
        }

        [HttpPost]
        public async Task<IActionResult> ImportUsers(IFormFile file, bool skipDuplicates = true)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "Please select a file to import." });
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (fileExtension != ".csv" && fileExtension != ".xlsx" && fileExtension != ".xls")
            {
                return Json(new { success = false, message = "Only CSV and Excel files are supported." });
            }

            var users = new List<UserImportModel>();
            var errors = new List<string>();

            try
            {
                if (fileExtension == ".csv")
                {
                    users = await ParseCsvFile(file);
                }
                else
                {
                    users = await ParseExcelFile(file);
                }

                if (!users.Any())
                {
                    return Json(new { success = false, message = "No valid data found in the file." });
                }

                _logger.LogInformation($"Parsed {users.Count} users from file");

                var importedCount = 0;
                var skippedCount = 0;

                // Get default major for students
                var defaultMajor = await _context.Majors.FirstOrDefaultAsync();
                if (defaultMajor == null)
                {
                    return Json(new { success = false, message = "No majors found in database. Please create at least one major before importing students." });
                }
                _logger.LogInformation($"Using default major: {defaultMajor.Name} (ID: {defaultMajor.MajorId})");

                foreach (var userData in users)
                {
                    try
                    {
                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(userData.Name) || 
                            string.IsNullOrWhiteSpace(userData.Email) || 
                            string.IsNullOrWhiteSpace(userData.Password) || 
                            string.IsNullOrWhiteSpace(userData.Role))
                        {
                            errors.Add($"Row {users.IndexOf(userData) + 2}: Missing required fields (Name, Email, Password, Role)");
                            _logger.LogWarning($"Skipping user {userData.Email}: Missing required fields");
                            continue;
                        }

                        // Check for duplicate email if skipDuplicates is true
                        if (skipDuplicates)
                        {
                            var existingUser = await _userManager.FindByEmailAsync(userData.Email);
                            if (existingUser != null)
                            {
                                skippedCount++;
                                continue;
                            }
                        }

                        // Validate role
                        var validRoles = new[] { "admin", "lecturer", "student" };
                        if (!validRoles.Contains(userData.Role.ToLower()))
                        {
                            errors.Add($"Row {users.IndexOf(userData) + 2}: Invalid role '{userData.Role}'. Must be Admin, Lecturer, or Student");
                            _logger.LogWarning($"Skipping user {userData.Email}: Invalid role '{userData.Role}'");
                            continue;
                        }

                        _logger.LogInformation($"Creating user: {userData.Email} with role {userData.Role}");

                        // Create user
                        var user = new User
                        {
                            UserName = userData.Email,
                            Email = userData.Email,
                            Name = userData.Name,
                            Role = userData.Role,
                            StudentCode = userData.StudentCode,
                            DateOfBirth = userData.DateOfBirth,
                            Phone = userData.Phone,
                            Gender = userData.Gender,
                            Address = userData.Address,
                            EmailConfirmed = true
                        };

                        var result = await _userManager.CreateAsync(user, userData.Password);
                        
                        if (result.Succeeded)
                        {
                            await _userManager.AddToRoleAsync(user, userData.Role);
                            
                            // Create role-specific records
                            switch (userData.Role.ToLower())
                            {
                                case "student":
                                    var student = new Student { UserId = user.Id, MajorId = defaultMajor.MajorId };
                                    _context.Students.Add(student);
                                    _logger.LogInformation($"Created student record for {userData.Email}");
                                    break;
                                case "lecturer":
                                    var lecturer = new Lecturer { UserId = user.Id };
                                    _context.Lecturers.Add(lecturer);
                                    _logger.LogInformation($"Created lecturer record for {userData.Email}");
                                    break;
                                case "admin":
                                    var admin = new Admin { UserId = user.Id };
                                    _context.Admins.Add(admin);
                                    _logger.LogInformation($"Created admin record for {userData.Email}");
                                    break;
                            }
                            
                            await _context.SaveChangesAsync();
                            importedCount++;
                            _logger.LogInformation($"Successfully imported user: {userData.Email}");
                        }
                        else
                        {
                            var errorMsg = string.Join(", ", result.Errors.Select(e => e.Description));
                            errors.Add($"Row {users.IndexOf(userData) + 2}: {errorMsg}");
                            _logger.LogWarning($"Failed to create user {userData.Email}: {errorMsg}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {users.IndexOf(userData) + 2}: {ex.Message}");
                    }
                }

                await InvalidateUserStatsCache();

                var message = $"Imported {importedCount} users successfully.";
                if (skippedCount > 0)
                {
                    message += $" Skipped {skippedCount} duplicate emails.";
                }

                return Json(new { 
                    success = true, 
                    message = message,
                    importedCount = importedCount,
                    skippedCount = skippedCount,
                    errors = errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing users");
                return Json(new { success = false, message = "Error processing file: " + ex.Message });
            }
        }

        private async Task<List<UserImportModel>> ParseCsvFile(IFormFile file)
        {
            var users = new List<UserImportModel>();
            
            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();
            _logger.LogInformation($"CSV Content length: {content.Length}");
            
            // Reset stream
            file.OpenReadStream().Position = 0;
            
            using var reader2 = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader2, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                HasHeaderRecord = false // CSV file doesn't have header row
            });
            
            await foreach (var record in csv.GetRecordsAsync<UserImportModel>())
            {
                users.Add(record);
            }
            
            _logger.LogInformation($"Parsed {users.Count} records from CSV file");
            return users;
        }

        private async Task<List<UserImportModel>> ParseExcelFile(IFormFile file)
        {
            var users = new List<UserImportModel>();
            
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage(file.OpenReadStream());
            var worksheet = package.Workbook.Worksheets[0];
            
            // Skip header row
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var user = new UserImportModel
                {
                    Name = worksheet.Cells[row, 1].Text?.Trim(),
                    Email = worksheet.Cells[row, 2].Text?.Trim(),
                    Password = worksheet.Cells[row, 3].Text?.Trim(),
                    Role = worksheet.Cells[row, 4].Text?.Trim(),
                    StudentCode = worksheet.Cells[row, 5].Text?.Trim(),
                    DateOfBirth = ParseDate(worksheet.Cells[row, 6].Text),
                    Phone = worksheet.Cells[row, 7].Text?.Trim(),
                    Gender = worksheet.Cells[row, 8].Text?.Trim(),
                    Address = worksheet.Cells[row, 9].Text?.Trim()
                };
                
                users.Add(user);
            }
            
            return users;
        }

        private DateTime? ParseDate(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return null;
                
            if (DateTime.TryParse(dateString, out var date))
                return date;
                
            return null;
        }
        
        private async Task InvalidateUserStatsCache()
        {
            _cache.Remove(USER_STATS_CACHE_KEY);
            await Task.CompletedTask;
        }

        [HttpPost]
        public async Task<IActionResult> AddUser(string Name, string Email, string Password, string Role, 
            string? StudentCode, DateTime? DateOfBirth, string? Phone, string? Gender, string? Address)
        {
            try
            {
                var user = new User
                {
                    UserName = Email,
                    Email = Email,
                    Name = Name,
                    Role = Role,
                    StudentCode = StudentCode,
                    DateOfBirth = DateOfBirth,
                    Phone = Phone,
                    Gender = Gender,
                    Address = Address,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, Password);
                
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, Role);
                    
                    // Create role-specific records
                    switch (Role.ToLower())
                    {
                        case "student":
                            var student = new Student
                            {
                                UserId = user.Id,
                                MajorId = 1 // Default major
                            };
                            _context.Students.Add(student);
                            break;
                        case "lecturer":
                            var lecturer = new Lecturer
                            {
                                UserId = user.Id,
                                DepartmentId = 1 // Default department
                            };
                            _context.Lecturers.Add(lecturer);
                            break;
                        case "admin":
                            var admin = new Admin
                            {
                                UserId = user.Id
                            };
                            _context.Admins.Add(admin);
                            break;
                    }
                    
                    await _context.SaveChangesAsync();
                    
                    // Invalidate cache after adding user
                    await InvalidateUserStatsCache();
                    
                    return Json(new { success = true });
                }
                
                return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return Json(new { success = false });

            return Json(new
            {
                id = user.Id,
                name = user.Name,
                email = user.Email,
                role = user.Role,
                studentCode = user.StudentCode,
                dateOfBirth = user.DateOfBirth,
                phone = user.Phone,
                gender = user.Gender,
                address = user.Address
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUser(string Id, string Name, string Role, 
            string? StudentCode, DateTime? DateOfBirth, string? Phone, string? Gender, string? Address)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(Id);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                user.Name = Name;
                user.Role = Role;
                user.StudentCode = StudentCode;
                user.DateOfBirth = DateOfBirth;
                user.Phone = Phone;
                user.Gender = Gender;
                user.Address = Address;

                var result = await _userManager.UpdateAsync(user);
                
                if (result.Succeeded)
                {
                    // Update role
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    await _userManager.AddToRoleAsync(user, Role);
                    
                    // Invalidate cache after updating user
                    await InvalidateUserStatsCache();
                    
                    return Json(new { success = true });
                }
                
                return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                // Delete role-specific records first
                switch (user.Role.ToLower())
                {
                    case "student":
                        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == id);
                        if (student != null)
                        {
                            // Remove student course assignments
                            var studentCourses = await _context.StudentCourses
                                .Where(sc => sc.StudentId == student.StudentId)
                                .ToListAsync();
                            _context.StudentCourses.RemoveRange(studentCourses);
                            
                            _context.Students.Remove(student);
                        }
                        break;
                    case "lecturer":
                        var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == id);
                        if (lecturer != null)
                            _context.Lecturers.Remove(lecturer);
                        break;
                    case "admin":
                        var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == id);
                        if (admin != null)
                            _context.Admins.Remove(admin);
                        break;
                }
                
                await _context.SaveChangesAsync();
                
                var result = await _userManager.DeleteAsync(user);
                
                if (result.Succeeded)
                {
                    // Invalidate cache after deleting user
                    await InvalidateUserStatsCache();
                    return Json(new { success = true });
                }
                    
                return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Assign Students to Courses
        public async Task<IActionResult> AssignStudentToCourse(int page = 1, int pageSize = 10)
        {
            ViewBag.Students = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Major)
                .ToListAsync();
            ViewBag.Courses = await _context.Courses
                .Include(c => c.Subject)
                .Include(c => c.Major)
                .ToListAsync();

            var assignments = await _context.StudentCourses
                .Include(sc => sc.Student)
                .ThenInclude(s => s.User)
                .Include(sc => sc.Course)
                .ThenInclude(c => c.Subject)
                .ToListAsync();

            var totalAssignments = assignments.Count;
            var pagedAssignments = assignments.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalAssignments / pageSize);
            ViewBag.PageSize = pageSize;
            ViewBag.Action = "AssignStudentToCourse";
            ViewBag.Controller = "Admin";

            return View(pagedAssignments);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AssignStudentToCoursePost(int studentId, int courseId)
        {
            try
            {
                var existing = await _context.StudentCourses
                    .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);
                
                if (existing == null)
                {
                    var studentCourse = new StudentCourse
                    {
                        StudentId = studentId,
                        CourseId = courseId
                    };
                    
                    _context.StudentCourses.Add(studentCourse);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Student assigned to course successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Student is already assigned to this course." });
                }
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
                var studentCourse = await _context.StudentCourses
                    .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);
                
                if (studentCourse != null)
                {
                    _context.StudentCourses.Remove(studentCourse);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Student removed from course successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Assignment not found." });
                }
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
                var course = await _context.Courses
                    .Include(c => c.Subject)
                    .Include(c => c.Semester)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId);

                if (course == null)
                {
                    return Json(new { success = false, message = "Course not found." });
                }

                var students = await _context.StudentCourses
                    .Where(sc => sc.CourseId == courseId)
                    .Include(sc => sc.Student)
                    .ThenInclude(s => s.User)
                    .Select(sc => new
                    {
                        studentId = sc.Student.StudentId,
                        name = sc.Student.User.Name,
                        email = sc.Student.User.Email,
                        avatar = sc.Student.User.Avatar
                    })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    courseName = course.CourseName,
                    subjectName = course.Subject.Name,
                    semesterName = $"{course.Semester.Name} {course.Semester.Year}",
                    students = students
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }

    public class UserImportModel
    {
        [CsvHelper.Configuration.Attributes.Index(0)]
        public string? Name { get; set; }
        [CsvHelper.Configuration.Attributes.Index(1)]
        public string? Email { get; set; }
        [CsvHelper.Configuration.Attributes.Index(2)]
        public string? Password { get; set; }
        [CsvHelper.Configuration.Attributes.Index(3)]
        public string? Role { get; set; }
        [CsvHelper.Configuration.Attributes.Index(4)]
        public string? StudentCode { get; set; }
        [CsvHelper.Configuration.Attributes.Index(5)]
        public DateTime? DateOfBirth { get; set; }
        [CsvHelper.Configuration.Attributes.Index(6)]
        public string? Phone { get; set; }
        [CsvHelper.Configuration.Attributes.Index(7)]
        public string? Gender { get; set; }
        [CsvHelper.Configuration.Attributes.Index(8)]
        public string? Address { get; set; }
    }
}