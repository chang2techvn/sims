using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SIMS.Data;
using SIMS.Models;

namespace SIMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private const string USER_STATS_CACHE_KEY = "UserStatistics";
        private readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(10);

        public AdminController(ApplicationDbContext context, UserManager<User> userManager, IMemoryCache cache) : base(userManager)
        {
            _context = context;
            _cache = cache;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Manage Departments
        public async Task<IActionResult> ManageDepartments()
        {
            var departments = await _context.Departments.ToListAsync();
            return View(departments);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDepartment(Department department)
        {
            if (ModelState.IsValid)
            {
                _context.Departments.Add(department);
                await _context.SaveChangesAsync();
                return RedirectToAction("ManageDepartments");
            }
            return RedirectToAction("ManageDepartments");
        }

        // Manage Majors
        public async Task<IActionResult> ManageMajors()
        {
            var majors = await _context.Majors.Include(m => m.Department).ToListAsync();
            ViewBag.Departments = await _context.Departments.ToListAsync();
            return View(majors);
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

                // Invalidate cache
                await InvalidateUserStatsCache();

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

                // Invalidate cache
                await InvalidateUserStatsCache();

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

                // Invalidate cache
                await InvalidateUserStatsCache();

                return Json(new { success = true, message = "Major deleted successfully!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting major: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while deleting the major." });
            }
        }

        // Manage Semesters
        public async Task<IActionResult> ManageSemesters()
        {
            var semesters = await _context.Semesters.ToListAsync();
            return View(semesters);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSemester(Semester semester)
        {
            if (ModelState.IsValid)
            {
                _context.Semesters.Add(semester);
                await _context.SaveChangesAsync();
                return RedirectToAction("ManageSemesters");
            }
            return RedirectToAction("ManageSemesters");
        }

        // Manage Subjects
        public async Task<IActionResult> ManageSubjects()
        {
            var subjects = await _context.Subjects.ToListAsync();
            return View(subjects);
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

        // Manage Courses
        public async Task<IActionResult> ManageCourses()
        {
            var courses = await _context.Courses
                .Include(c => c.Subject)
                .Include(c => c.Semester)
                .Include(c => c.Major)
                .Include(c => c.Lecturer)
                .ThenInclude(l => l.User)
                .ToListAsync();
            
            ViewBag.Subjects = await _context.Subjects.ToListAsync();
            ViewBag.Semesters = await _context.Semesters.ToListAsync();
            ViewBag.Majors = await _context.Majors.ToListAsync();
            ViewBag.Lecturers = await _context.Lecturers.Include(l => l.User).ToListAsync();
            
            return View(courses);
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
        public async Task<IActionResult> ManageUsers()
        {
            var users = await base._userManager.Users.ToListAsync();
            
            // Get statistics from cache or calculate
            var stats = await _cache.GetOrCreateAsync(USER_STATS_CACHE_KEY, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CACHE_DURATION;
                
                var roles = await _userManager.Users
                    .Select(u => u.Role.ToLower())
                    .ToListAsync();
                
                return new
                {
                    StudentCount = roles.Count(r => r == "student"),
                    LecturerCount = roles.Count(r => r == "lecturer"),
                    AdminCount = roles.Count(r => r == "admin")
                };
            });
            
            ViewBag.StudentCount = stats!.StudentCount;
            ViewBag.LecturerCount = stats.LecturerCount;
            ViewBag.AdminCount = stats.AdminCount;
            
            return View(users);
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
        public async Task<IActionResult> AssignStudentToCourse()
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
            
            return View(assignments);
        }

        [HttpPost]
        public async Task<IActionResult> AssignStudentToCourse(int studentId, int courseId)
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
            }
            
            return RedirectToAction("AssignStudentToCourse");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveStudentFromCourse(int studentId, int courseId)
        {
            var studentCourse = await _context.StudentCourses
                .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);
            
            if (studentCourse != null)
            {
                _context.StudentCourses.Remove(studentCourse);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction("AssignStudentToCourse");
        }
    }
}