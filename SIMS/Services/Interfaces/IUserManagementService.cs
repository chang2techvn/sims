using SIMS.Models;

namespace SIMS.Services.Interfaces
{
    public interface IUserManagementService
    {
        Task<List<User>> GetAllUsersAsync();
        Task<(bool success, string message)> AssignStudentToCourseAsync(int studentId, int courseId);
        Task<(bool success, string message)> RemoveStudentFromCourseAsync(int studentId, int courseId);
        Task<(bool success, string message)> UpdateStudentCourseAssignmentAsync(int currentStudentId, int currentCourseId, int newStudentId, int newCourseId);
        Task<(bool success, string message, object? data)> GetCourseStudentsAsync(int courseId);
    }
}