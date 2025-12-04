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
        public async Task<IActionResult> CreateCourse(Course course)
        {
            if (ModelState.IsValid)
            {
                _context.Courses.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction("ManageCourses");
            }
            return RedirectToAction("ManageCourses");
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
        
        private void InvalidateUserStatsCache()
        {
            _cache.Remove(USER_STATS_CACHE_KEY);
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
                    InvalidateUserStatsCache();
                    
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
                    InvalidateUserStatsCache();
                    
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
                    InvalidateUserStatsCache();
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