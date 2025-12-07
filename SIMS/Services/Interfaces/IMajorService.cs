using SIMS.Models;

namespace SIMS.Services.Interfaces
{
    public interface IMajorService
    {
        Task<List<Major>> GetAllMajorsAsync();
        Task CreateMajorAsync(string name, int departmentId);
        Task UpdateMajorAsync(int majorId, string name, int departmentId);
        Task DeleteMajorAsync(int majorId);
        Task<Major?> GetMajorAsync(int id);
    }
}