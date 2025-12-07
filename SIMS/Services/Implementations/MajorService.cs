using SIMS.Data;
using SIMS.Models;
using SIMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace SIMS.Services.Implementations
{
    public class MajorService : IMajorService
    {
        private readonly ApplicationDbContext _context;

        public MajorService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Major>> GetAllMajorsAsync()
        {
            return await _context.Majors.Include(m => m.Department).ToListAsync();
        }

        public async Task CreateMajorAsync(string name, int departmentId)
        {
            var department = await _context.Departments.FindAsync(departmentId);
            if (department == null) throw new Exception("Department not found.");

            var major = new Major { Name = name.Trim(), DepartmentId = departmentId };
            _context.Majors.Add(major);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateMajorAsync(int majorId, string name, int departmentId)
        {
            var major = await _context.Majors.Include(m => m.Department).FirstOrDefaultAsync(m => m.MajorId == majorId);
            if (major == null) throw new Exception("Major not found.");

            var department = await _context.Departments.FindAsync(departmentId);
            if (department == null) throw new Exception("Department not found.");

            major.Name = name.Trim();
            major.DepartmentId = departmentId;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteMajorAsync(int majorId)
        {
            var major = await _context.Majors
                .Include(m => m.Students)
                .Include(m => m.Courses)
                .FirstOrDefaultAsync(m => m.MajorId == majorId);

            if (major == null) throw new Exception("Major not found.");

            if (major.Students.Any() || major.Courses.Any())
            {
                throw new Exception("Cannot delete major with associated students or courses.");
            }

            _context.Majors.Remove(major);
            await _context.SaveChangesAsync();
        }

        public async Task<Major?> GetMajorAsync(int id)
        {
            return await _context.Majors
                .Include(m => m.Department)
                .FirstOrDefaultAsync(m => m.MajorId == id);
        }
    }
}