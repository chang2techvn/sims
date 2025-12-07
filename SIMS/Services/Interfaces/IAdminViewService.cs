using Microsoft.AspNetCore.Identity;
using SIMS.Models;

namespace SIMS.Services.Interfaces
{
    public interface IAdminViewService
    {
        Task<ManageUsersViewModel> GetManageUsersDataAsync(int page, int pageSize);
        Task<AssignStudentToCourseViewModel> GetAssignStudentToCourseDataAsync(int page, int pageSize);
        Task<ManageDepartmentsViewModel> GetManageDepartmentsDataAsync(int page, int pageSize);
        Task<ManageMajorsViewModel> GetManageMajorsDataAsync(int page, int pageSize);
        Task<ManageSemestersViewModel> GetManageSemestersDataAsync(int page, int pageSize);
        Task<ManageSubjectsViewModel> GetManageSubjectsDataAsync(int page, int pageSize);
        Task<ManageCoursesViewModel> GetManageCoursesDataAsync(int page, int pageSize);
    }

    public class ManageUsersViewModel
    {
        public List<User> Users { get; set; } = new List<User>();
        public List<IdentityRole> Roles { get; set; } = new List<IdentityRole>();
        public List<Department> Departments { get; set; } = new List<Department>();
        public List<Major> Majors { get; set; } = new List<Major>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }

    public class AssignStudentToCourseViewModel
    {
        public List<Student> Students { get; set; } = new List<Student>();
        public List<Course> Courses { get; set; } = new List<Course>();
        public List<StudentCourse> Assignments { get; set; } = new List<StudentCourse>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }

    public class ManageDepartmentsViewModel
    {
        public List<Department> Departments { get; set; } = new List<Department>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }

    public class ManageMajorsViewModel
    {
        public List<Major> Majors { get; set; } = new List<Major>();
        public List<Department> Departments { get; set; } = new List<Department>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }

    public class ManageSemestersViewModel
    {
        public List<Semester> Semesters { get; set; } = new List<Semester>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }

    public class ManageSubjectsViewModel
    {
        public List<Subject> Subjects { get; set; } = new List<Subject>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }

    public class ManageCoursesViewModel
    {
        public List<Course> Courses { get; set; } = new List<Course>();
        public List<Subject> Subjects { get; set; } = new List<Subject>();
        public List<Semester> Semesters { get; set; } = new List<Semester>();
        public List<Major> Majors { get; set; } = new List<Major>();
        public List<Lecturer> Lecturers { get; set; } = new List<Lecturer>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }
}