using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;
using SIMS.Services.Interfaces;

namespace SIMS.Services.Implementations
{
    public class HomeService : IHomeService
    {
        private readonly ApplicationDbContext _context;

        public HomeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(string userId, string role)
        {
            var viewModel = new DashboardViewModel();

            switch (role.ToLower())
            {
                case "admin":
                    viewModel.TotalStudents = await _context.Students.CountAsync();
                    viewModel.TotalLecturers = await _context.Lecturers.CountAsync();
                    viewModel.TotalAdmins = await _context.Admins.CountAsync();
                    viewModel.TotalUsers = await _context.Users.CountAsync();
                    viewModel.TotalCourses = await _context.Courses.CountAsync();

                    // Get recent enrollment activities
                    var recentEnrollments = await _context.StudentCourses
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
                    break;

                case "lecturer":
                    var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == userId);
                    if (lecturer != null)
                    {
                        viewModel.MyCourses = await _context.Courses.Where(c => c.LecturerId == lecturer.LecturerId).CountAsync();
                        viewModel.MyStudents = await _context.StudentCourses
                            .Where(sc => sc.Course.LecturerId == lecturer.LecturerId)
                            .Select(sc => sc.StudentId)
                            .Distinct()
                            .CountAsync();
                    }
                    break;

                case "student":
                    var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                    if (student != null)
                    {
                        viewModel.EnrolledCourses = await _context.StudentCourses.Where(sc => sc.StudentId == student.StudentId).CountAsync();
                    }
                    break;
            }

            return viewModel;
        }
    }
}