using Xunit;
using Moq;
using SIMS.Data;
using SIMS.Models;
using SIMS.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SIMS.Tests
{
    public class CourseServiceTests
    {
        private readonly ApplicationDbContext _context;
        private readonly CourseService _courseService;

        public CourseServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _courseService = new CourseService(_context);
        }

        [Fact]
        public async Task CreateCourseAsync_AddsCourseToDatabase()
        {
            Console.WriteLine("=== Starting Test: CreateCourseAsync_AddsCourseToDatabase ===");

            // Arrange
            Console.WriteLine("Arrange: Setting up test data");
            var course = new Course { CourseName = "Test Course" };
            Console.WriteLine($"Arrange: Course to create - Name: {course.CourseName}");

            // Check initial state
            var initialCount = await _context.Courses.CountAsync();
            Console.WriteLine($"Initial database state: {initialCount} courses");

            // Act
            Console.WriteLine("Act: Calling CreateCourseAsync");
            await _courseService.CreateCourseAsync(course);
            Console.WriteLine("Act: CreateCourseAsync completed");

            // Check after creation
            var afterCount = await _context.Courses.CountAsync();
            Console.WriteLine($"After creation: {afterCount} courses in database");

            // Assert
            Console.WriteLine("Assert: Verifying course was added");
            var addedCourse = await _context.Courses.FirstOrDefaultAsync(c => c.CourseName == "Test Course");
            Console.WriteLine($"Assert: Found course - Name: {addedCourse?.CourseName}, ID: {addedCourse?.CourseId}");

            Assert.NotNull(addedCourse);
            Assert.Equal("Test Course", addedCourse.CourseName);
            Console.WriteLine("Assert: Test passed successfully");
        }

        [Fact]
        public async Task UpdateCourseAsync_UpdatesExistingCourse()
        {
            Console.WriteLine("=== Starting Test: UpdateCourseAsync_UpdatesExistingCourse ===");

            // Arrange
            Console.WriteLine("Arrange: Setting up test data");
            var course = new Course { CourseName = "Old Name" };
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
            Console.WriteLine($"Arrange: Added course - ID: {course.CourseId}, Name: {course.CourseName}");

            var updatedCourse = new Course { SubjectId = 1, SemesterId = 1, MajorId = 1, LecturerId = 1, CourseName = "New Name" };
            Console.WriteLine($"Arrange: Update data - New Name: {updatedCourse.CourseName}");

            // Check before update
            var beforeUpdate = await _context.Courses.FindAsync(course.CourseId);
            Console.WriteLine($"Before update: Course Name = {beforeUpdate?.CourseName}");

            // Act
            Console.WriteLine("Act: Calling UpdateCourseAsync");
            await _courseService.UpdateCourseAsync(course.CourseId, updatedCourse);
            Console.WriteLine("Act: UpdateCourseAsync completed");

            // Assert
            Console.WriteLine("Assert: Verifying course was updated");
            var updated = await _context.Courses.FindAsync(course.CourseId);
            Console.WriteLine($"Assert: Updated course - Name: {updated?.CourseName}");

            Assert.Equal("New Name", updated.CourseName);
            Console.WriteLine("Assert: Test passed successfully");
        }

        [Fact]
        public async Task DeleteCourseAsync_RemovesCourseFromDatabase()
        {
            Console.WriteLine("=== Starting Test: DeleteCourseAsync_RemovesCourseFromDatabase ===");

            // Arrange
            Console.WriteLine("Arrange: Setting up test data");
            var course = new Course { CourseName = "Test Course" };
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
            Console.WriteLine($"Arrange: Added course - ID: {course.CourseId}, Name: {course.CourseName}");

            // Check before delete
            var beforeDelete = await _context.Courses.FindAsync(course.CourseId);
            Console.WriteLine($"Before delete: Course exists = {beforeDelete != null}");

            // Act
            Console.WriteLine("Act: Calling DeleteCourseAsync");
            await _courseService.DeleteCourseAsync(course.CourseId);
            Console.WriteLine("Act: DeleteCourseAsync completed");

            // Check after delete
            var afterDeleteCount = await _context.Courses.CountAsync();
            Console.WriteLine($"After delete: Total courses in database = {afterDeleteCount}");

            // Assert
            Console.WriteLine("Assert: Verifying course was deleted");
            var deleted = await _context.Courses.FindAsync(course.CourseId);
            Console.WriteLine($"Assert: Course still exists = {deleted != null}");

            Assert.Null(deleted);
            Console.WriteLine("Assert: Test passed successfully");
        }
    }
}