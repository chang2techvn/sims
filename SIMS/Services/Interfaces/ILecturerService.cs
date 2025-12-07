using SIMS.Models;
using SIMS.Models.ViewModels;

namespace SIMS.Services.Interfaces
{
    public interface ILecturerService
    {
        Task<List<LecturerCourseViewModel>?> GetLecturerCoursesAsync(string userId);
        Task<(Course? Course, List<Student>? Students)> GetCourseStudentsAsync(string userId, int courseId);
        Task<(User? User, Lecturer? Lecturer)> GetLecturerProfileAsync(string userId);
    }
}