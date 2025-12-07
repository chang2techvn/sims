using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;
using SIMS.Services.Interfaces;

namespace SIMS.Services.Implementations
{
    public class AdminDataService : IAdminDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public AdminDataService(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<object?> GetUserDataAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return null;

            return new
            {
                id = user.Id,
                name = user.Name,
                email = user.Email,
                role = user.Role,
                studentCode = user.StudentCode,
                dateOfBirth = user.DateOfBirth,
                phone = user.Phone,
                gender = user.Gender,
                address = user.Address,
                avatar = user.Avatar
            };
        }

        public async Task<object?> GetCourseDataAsync(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Subject)
                .Include(c => c.Semester)
                .Include(c => c.Major)
                .Include(c => c.Lecturer)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
                return null;

            return new
            {
                courseId = course.CourseId,
                courseName = course.CourseName,
                subjectId = course.SubjectId,
                semesterId = course.SemesterId,
                majorId = course.MajorId,
                lecturerId = course.LecturerId
            };
        }

        public async Task<object?> GetSubjectDataAsync(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
                return null;

            return new
            {
                subjectId = subject.SubjectId,
                code = subject.Code,
                name = subject.Name
            };
        }

        public async Task<object?> GetDepartmentDataAsync(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return null;

            return new
            {
                departmentId = department.DepartmentId,
                name = department.Name
            };
        }

        public async Task<object?> GetMajorDataAsync(int id)
        {
            var major = await _context.Majors
                .Include(m => m.Department)
                .FirstOrDefaultAsync(m => m.MajorId == id);

            if (major == null)
                return null;

            return new
            {
                majorId = major.MajorId,
                name = major.Name,
                departmentId = major.DepartmentId
            };
        }

        public async Task<object?> GetSemesterDataAsync(int id)
        {
            var semester = await _context.Semesters.FindAsync(id);
            if (semester == null)
                return null;

            return new
            {
                semesterId = semester.SemesterId,
                name = semester.Name,
                startDate = semester.StartDate.ToString("yyyy-MM-dd"),
                endDate = semester.EndDate.ToString("yyyy-MM-dd")
            };
        }

        public async Task<object?> GetStudentCourseDataAsync(int studentId, int courseId)
        {
            var studentCourse = await _context.StudentCourses
                .Include(sc => sc.Student)
                .ThenInclude(s => s.User)
                .Include(sc => sc.Course)
                .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);

            if (studentCourse == null)
                return null;

            var studentName = studentCourse.Student?.User?.Name ?? "Unknown Student";
            var courseName = studentCourse.Course?.CourseName ?? "Unknown Course";

            return new
            {
                success = true,
                studentId = studentCourse.StudentId,
                courseId = studentCourse.CourseId,
                studentName = studentName,
                courseName = courseName
            };
        }

        public async Task<object?> GetCourseStudentsDataAsync(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
                return null;

            var studentCourses = await _context.StudentCourses
                .Include(sc => sc.Student)
                .ThenInclude(s => s.User)
                .Include(sc => sc.Course)
                .Where(sc => sc.CourseId == courseId)
                .ToListAsync();

            var students = studentCourses.Select(sc => new
            {
                studentId = sc.StudentId,
                studentName = sc.Student?.User?.Name ?? "Unknown",
                studentCode = sc.Student?.User?.StudentCode ?? "",
                enrollmentDate = sc.EnrollmentDate
            }).ToList();

            return new
            {
                success = true,
                courseName = course.CourseName,
                students = students
            };
        }
    }
}