using Xunit;
using Moq;
using SIMS.Data;
using SIMS.Models;
using SIMS.Services.Implementations;
using Microsoft.EntityFrameworkCore;

namespace SIMS.Tests
{
    public class DepartmentServiceTests
    {
        private readonly ApplicationDbContext _context;
        private readonly DepartmentService _departmentService;

        public DepartmentServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _departmentService = new DepartmentService(_context);
        }

        [Fact]
        public async Task CreateDepartmentAsync_AddsDepartmentToDatabase()
        {
            Console.WriteLine("=== Starting Test: CreateDepartmentAsync_AddsDepartmentToDatabase ===");

            // Arrange
            Console.WriteLine("Arrange: Setting up test data");
            var name = "Computer Science";
            Console.WriteLine($"Arrange: Department name to create: {name}");

            // Check initial state
            var initialCount = await _context.Departments.CountAsync();
            Console.WriteLine($"Initial database state: {initialCount} departments");

            // Act
            Console.WriteLine("Act: Calling CreateDepartmentAsync");
            await _departmentService.CreateDepartmentAsync(name);
            Console.WriteLine("Act: CreateDepartmentAsync completed");

            // Check after creation
            var afterCount = await _context.Departments.CountAsync();
            Console.WriteLine($"After creation: {afterCount} departments in database");

            // Assert
            Console.WriteLine("Assert: Verifying department was added");
            var addedDepartment = await _context.Departments.FirstOrDefaultAsync(d => d.Name == name);
            Console.WriteLine($"Assert: Found department - Name: {addedDepartment?.Name}, ID: {addedDepartment?.DepartmentId}");

            Assert.NotNull(addedDepartment);
            Assert.Equal(name, addedDepartment.Name);
            Console.WriteLine("Assert: Test passed successfully");
        }
    }
}