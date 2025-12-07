using SIMS.Models;

namespace SIMS.Services.Interfaces
{
    public interface ICourseService
    {
        Task<List<Course>> GetAllCoursesAsync();
        Task CreateCourseAsync(Course course);
        Task UpdateCourseAsync(int id, Course course);
        Task DeleteCourseAsync(int id);
        Task<Course?> GetCourseAsync(int id);
    }
}