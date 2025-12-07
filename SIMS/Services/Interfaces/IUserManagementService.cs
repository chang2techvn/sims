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
        Task<StudentCourse?> GetStudentCourseAsync(int studentId, int courseId);
        Task<(bool success, string message)> AddUserAsync(string name, string email, string password, string role, string? studentCode, DateTime? dateOfBirth, string? phone, string? gender, string? address);
        Task<(bool success, string message)> UpdateUserAsync(string id, string name, string role, string? studentCode, DateTime? dateOfBirth, string? phone, string? gender, string? address, string? password, string? avatar);
        Task<(bool success, string message)> DeleteUserAsync(string id);
    }
}