using Microsoft.AspNetCore.Identity;
using SIMS.Models;

namespace SIMS.Services.Interfaces
{
    public interface IAdminDataService
    {
        Task<object?> GetUserDataAsync(string id);
        Task<object?> GetCourseDataAsync(int id);
        Task<object?> GetSubjectDataAsync(int id);
        Task<object?> GetDepartmentDataAsync(int id);
        Task<object?> GetMajorDataAsync(int id);
        Task<object?> GetSemesterDataAsync(int id);
        Task<object?> GetStudentCourseDataAsync(int studentId, int courseId);
        Task<object?> GetCourseStudentsDataAsync(int courseId);
    }
}