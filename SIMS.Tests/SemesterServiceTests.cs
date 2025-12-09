using Xunit;
using Moq;
using SIMS.Data;
using SIMS.Models;
using SIMS.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SIMS.Tests
{
    public class SemesterServiceTests
    {
        private readonly ApplicationDbContext _context;
        private readonly SemesterService _semesterService;

        public SemesterServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _semesterService = new SemesterService(_context);
        }

        [Fact]
        public async Task CreateSemesterAsync_AddsSemesterToDatabase()
        {
            Console.WriteLine("=== Starting Test: CreateSemesterAsync_AddsSemesterToDatabase ===");

            // Arrange
            Console.WriteLine("Arrange: Setting up test data");
            var name = "Spring 2025";
            var startDate = new DateTime(2025, 1, 1);
            var endDate = new DateTime(2025, 5, 31);

            Console.WriteLine($"Arrange: Semester name: {name}");
            Console.WriteLine($"Arrange: Start date: {startDate:yyyy-MM-dd}, End date: {endDate:yyyy-MM-dd}");

            // Check initial state
            var initialCount = await _context.Semesters.CountAsync();
            Console.WriteLine($"Initial database state: {initialCount} semesters");

            // Act
            Console.WriteLine("Act: Calling CreateSemesterAsync");
            await _semesterService.CreateSemesterAsync(name, startDate, endDate);
            Console.WriteLine("Act: CreateSemesterAsync completed");

            // Check after creation
            var afterCount = await _context.Semesters.CountAsync();
            Console.WriteLine($"After creation: {afterCount} semesters in database");

            // Assert
            Console.WriteLine("Assert: Verifying semester was added");
            var addedSemester = await _context.Semesters.FirstOrDefaultAsync(s => s.Name == name);
            Console.WriteLine($"Assert: Found semester - Name: {addedSemester?.Name}, ID: {addedSemester?.SemesterId}");
            Console.WriteLine($"Assert: Start date: {addedSemester?.StartDate:yyyy-MM-dd}, End date: {addedSemester?.EndDate:yyyy-MM-dd}");

            Assert.NotNull(addedSemester);
            Assert.Equal(name, addedSemester.Name);
            Assert.Equal(startDate, addedSemester.StartDate);
            Assert.Equal(endDate, addedSemester.EndDate);
            Console.WriteLine("Assert: Test passed successfully");
        }
    }
}