using SIMS.Data;
using SIMS.Models;
using SIMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace SIMS.Services.Implementations
{
    public class SemesterService : ISemesterService
    {
        private readonly ApplicationDbContext _context;

        public SemesterService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Semester>> GetAllSemestersAsync()
        {
            return await _context.Semesters.ToListAsync();
        }

        public async Task CreateSemesterAsync(string name, DateTime startDate, DateTime endDate)
        {
            var semester = new Semester { Name = name, StartDate = startDate, EndDate = endDate };
            _context.Semesters.Add(semester);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSemesterAsync(int id, string name, DateTime startDate, DateTime endDate)
        {
            var semester = await _context.Semesters.FindAsync(id);
            if (semester != null)
            {
                semester.Name = name;
                semester.StartDate = startDate;
                semester.EndDate = endDate;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteSemesterAsync(int id)
        {
            var semester = await _context.Semesters.FindAsync(id);
            if (semester != null)
            {
                _context.Semesters.Remove(semester);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Semester?> GetSemesterAsync(int id)
        {
            return await _context.Semesters.FindAsync(id);
        }
    }
}