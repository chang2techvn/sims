using SIMS.Models;

namespace SIMS.Services.Interfaces
{
    public interface ISubjectService
    {
        Task<List<Subject>> GetAllSubjectsAsync();
        Task CreateSubjectAsync(string name, string code);
        Task UpdateSubjectAsync(int id, string name, string code);
        Task DeleteSubjectAsync(int id);
    }
}