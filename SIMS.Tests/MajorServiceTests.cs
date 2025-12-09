using Xunit;
using Moq;
using SIMS.Data;
using SIMS.Models;
using SIMS.Services.Implementations;
using Microsoft.EntityFrameworkCore;

namespace SIMS.Tests
{
    public class MajorServiceTests
    {
        private readonly ApplicationDbContext _context;
        private readonly MajorService _majorService;

        public MajorServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _majorService = new MajorService(_context);
        }

        [Fact]
        public async Task CreateMajorAsync_AddsMajorToDatabase()
        {
            Console.WriteLine("=== Starting Test: CreateMajorAsync_AddsMajorToDatabase ===");

            // Arrange
            Console.WriteLine("Arrange: Setting up test data");
            var name = "Software Engineering";
            var departmentId = 1;
            var department = new Department { DepartmentId = departmentId, Name = "CS" };
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            Console.WriteLine($"Arrange: Major name: {name}, DepartmentId: {departmentId}");
            Console.WriteLine($"Arrange: Created department: {department.Name} (ID: {department.DepartmentId})");

            // Check initial state
            var initialCount = await _context.Majors.CountAsync();
            Console.WriteLine($"Initial database state: {initialCount} majors");

            // Act
            Console.WriteLine("Act: Calling CreateMajorAsync");
            await _majorService.CreateMajorAsync(name, departmentId);
            Console.WriteLine("Act: CreateMajorAsync completed");

            // Check after creation
            var afterCount = await _context.Majors.CountAsync();
            Console.WriteLine($"After creation: {afterCount} majors in database");

            // Assert
            Console.WriteLine("Assert: Verifying major was added");
            var addedMajor = await _context.Majors.FirstOrDefaultAsync(m => m.Name == name);
            Console.WriteLine($"Assert: Found major - Name: {addedMajor?.Name}, ID: {addedMajor?.MajorId}, DepartmentId: {addedMajor?.DepartmentId}");

            Assert.NotNull(addedMajor);
            Assert.Equal(name, addedMajor.Name);
            Assert.Equal(departmentId, addedMajor.DepartmentId);
            Console.WriteLine("Assert: Test passed successfully");
        }
    }
}