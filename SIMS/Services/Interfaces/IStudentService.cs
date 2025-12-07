using SIMS.Models;

namespace SIMS.Services.Interfaces
{
    public interface IStudentService
    {
        Task<List<Course>?> GetStudentCoursesAsync(string userId);
        Task<(Course? Course, List<Student>? EnrolledStudents)> GetCourseDetailsAsync(string userId, int courseId);
        Task<(User? User, Student? Student)> GetStudentProfileAsync(string userId);
    }
}