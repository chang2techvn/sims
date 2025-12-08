using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;
using SIMS.Models.ViewModels;
using SIMS.Services.Interfaces;

namespace SIMS.Services.Implementations
{
    public class LecturerDashboardStrategy : IDashboardStrategy
    {
        public async Task<DashboardViewModel> GetDashboardDataAsync(string userId, ApplicationDbContext context)
        {
            var viewModel = new DashboardViewModel();

            var lecturer = await context.Lecturers.FirstOrDefaultAsync(l => l.UserId == userId);
            if (lecturer != null)
            {
                viewModel.MyCourses = await context.Courses.Where(c => c.LecturerId == lecturer.LecturerId).CountAsync();
                viewModel.MyStudents = await context.StudentCourses
                    .Where(sc => sc.Course.LecturerId == lecturer.LecturerId)
                    .Select(sc => sc.StudentId)
                    .Distinct()
                    .CountAsync();
            }

            return viewModel;
        }
    }
}