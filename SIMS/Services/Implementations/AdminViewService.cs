using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;
using SIMS.Services.Interfaces;

namespace SIMS.Services.Implementations
{
    public class AdminViewService : IAdminViewService
    {
        private readonly ApplicationDbContext _context;

        public AdminViewService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ManageUsersViewModel> GetManageUsersDataAsync(int page, int pageSize)
        {
            var users = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Lecturer)
                .ToListAsync();
            var totalUsers = users.Count;
            var pagedUsers = users.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var roles = await _context.Roles.ToListAsync();
            var departments = await _context.Departments.ToListAsync();
            var majors = await _context.Majors.ToListAsync();

            return new ManageUsersViewModel
            {
                Users = pagedUsers,
                Roles = roles,
                Departments = departments,
                Majors = majors,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize),
                PageSize = pageSize
            };
        }

        public async Task<AssignStudentToCourseViewModel> GetAssignStudentToCourseDataAsync(int page, int pageSize)
        {
            var students = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Major)
                .ToListAsync();

            var courses = await _context.Courses
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

            return new AssignStudentToCourseViewModel
            {
                Students = students,
                Courses = courses,
                Assignments = pagedAssignments,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalAssignments / pageSize),
                PageSize = pageSize
            };
        }

        public async Task<ManageDepartmentsViewModel> GetManageDepartmentsDataAsync(int page, int pageSize)
        {
            var departments = await _context.Departments.ToListAsync();
            var totalDepartments = departments.Count;
            var pagedDepartments = departments.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new ManageDepartmentsViewModel
            {
                Departments = pagedDepartments,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalDepartments / pageSize),
                PageSize = pageSize
            };
        }

        public async Task<ManageMajorsViewModel> GetManageMajorsDataAsync(int page, int pageSize)
        {
            var majors = await _context.Majors.Include(m => m.Department).ToListAsync();
            var totalMajors = majors.Count;
            var pagedMajors = majors.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var departments = await _context.Departments.ToListAsync();

            return new ManageMajorsViewModel
            {
                Majors = pagedMajors,
                Departments = departments,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalMajors / pageSize),
                PageSize = pageSize
            };
        }

        public async Task<ManageSemestersViewModel> GetManageSemestersDataAsync(int page, int pageSize)
        {
            var semesters = await _context.Semesters.ToListAsync();
            var totalSemesters = semesters.Count;
            var pagedSemesters = semesters.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new ManageSemestersViewModel
            {
                Semesters = pagedSemesters,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalSemesters / pageSize),
                PageSize = pageSize
            };
        }

        public async Task<ManageSubjectsViewModel> GetManageSubjectsDataAsync(int page, int pageSize)
        {
            var subjects = await _context.Subjects.ToListAsync();
            var totalSubjects = subjects.Count;
            var pagedSubjects = subjects.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new ManageSubjectsViewModel
            {
                Subjects = pagedSubjects,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalSubjects / pageSize),
                PageSize = pageSize
            };
        }

        public async Task<ManageCoursesViewModel> GetManageCoursesDataAsync(int page, int pageSize)
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

            var subjects = await _context.Subjects.ToListAsync();
            var semesters = await _context.Semesters.ToListAsync();
            var majors = await _context.Majors.ToListAsync();
            var lecturers = await _context.Lecturers.Include(l => l.User).ToListAsync();

            return new ManageCoursesViewModel
            {
                Courses = pagedCourses,
                Subjects = subjects,
                Semesters = semesters,
                Majors = majors,
                Lecturers = lecturers,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalCourses / pageSize),
                PageSize = pageSize
            };
        }
    }
}