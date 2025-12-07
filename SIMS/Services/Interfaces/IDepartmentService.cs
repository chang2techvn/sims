using SIMS.Models;

namespace SIMS.Services.Interfaces
{
    public interface IDepartmentService
    {
        Task<List<Department>> GetAllDepartmentsAsync();
        Task<Department?> GetDepartmentByIdAsync(int id);
        Task CreateDepartmentAsync(string name);
        Task UpdateDepartmentAsync(int id, string name);
        Task DeleteDepartmentAsync(int id);
    }
}