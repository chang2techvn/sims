using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;

namespace SIMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context, UserManager<User> userManager) : base(userManager)
        {
            _context = context;
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
            return View(users);
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