using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;
using SIMS.Models.ViewModels;
using SIMS.Services.Interfaces;

namespace SIMS.Services.Implementations
{
    public class StudentDashboardStrategy : IDashboardStrategy
    {
        public async Task<DashboardViewModel> GetDashboardDataAsync(string userId, ApplicationDbContext context)
        {
            var viewModel = new DashboardViewModel();

            var student = await context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student != null)
            {
                viewModel.EnrolledCourses = await context.StudentCourses.Where(sc => sc.StudentId == student.StudentId).CountAsync();
            }

            return viewModel;
        }
    }
}