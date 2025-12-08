using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;
using SIMS.Models.ViewModels;
using SIMS.Services.Interfaces;

namespace SIMS.Services.Implementations
{
    public class AdminDashboardStrategy : IDashboardStrategy
    {
        public async Task<DashboardViewModel> GetDashboardDataAsync(string userId, ApplicationDbContext context)
        {
            var viewModel = new DashboardViewModel
            {
                TotalStudents = await context.Students.CountAsync(),
                TotalLecturers = await context.Lecturers.CountAsync(),
                TotalAdmins = await context.Admins.CountAsync(),
                TotalUsers = await context.Users.CountAsync(),
                TotalCourses = await context.Courses.CountAsync()
            };

            // Get recent enrollment activities
            var recentEnrollments = await context.StudentCourses
                .Include(sc => sc.Student)
                .ThenInclude(s => s.User)
                .Include(sc => sc.Course)
                .OrderByDescending(sc => sc.EnrollmentDate)
                .Take(10)
                .Select(sc => new RecentEnrollmentViewModel
                {
                    StudentName = sc.Student.User.Name,
                    CourseName = sc.Course.CourseName,
                    EnrollmentDate = sc.EnrollmentDate
                })
                .ToListAsync();
            viewModel.RecentEnrollments = recentEnrollments;

            return viewModel;
        }
    }
}