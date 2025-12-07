using SIMS.Models;

namespace SIMS.Services.Interfaces
{
    public interface IHomeService
    {
        Task<DashboardViewModel> GetDashboardDataAsync(string userId, string role);
    }

    public class DashboardViewModel
    {
        // Admin statistics
        public int? TotalStudents { get; set; }
        public int? TotalLecturers { get; set; }
        public int? TotalAdmins { get; set; }
        public int? TotalUsers { get; set; }
        public int? TotalCourses { get; set; }
        public List<RecentEnrollmentViewModel>? RecentEnrollments { get; set; }

        // Lecturer statistics
        public int? MyCourses { get; set; }
        public int? MyStudents { get; set; }

        // Student statistics
        public int? EnrolledCourses { get; set; }
    }

    public class RecentEnrollmentViewModel
    {
        public string StudentName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public DateTime? EnrollmentDate { get; set; }
    }
}