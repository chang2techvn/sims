using SIMS.Models;

namespace SIMS.Services.Interfaces
{
    public interface ISemesterService
    {
        Task<List<Semester>> GetAllSemestersAsync();
        Task CreateSemesterAsync(string name, DateTime startDate, DateTime endDate);
        Task UpdateSemesterAsync(int id, string name, DateTime startDate, DateTime endDate);
        Task DeleteSemesterAsync(int id);
    }
}