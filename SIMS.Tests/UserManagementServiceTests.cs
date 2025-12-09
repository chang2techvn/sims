using Xunit;
using Moq;
using SIMS.Data;
using SIMS.Models;
using SIMS.Services.Implementations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SIMS.Tests
{
    public class UserManagementServiceTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly UserManagementService _userManagementService;

        public UserManagementServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // Mock UserManager dependencies
            var userStore = new Mock<IUserStore<User>>();
            var passwordHasher = new Mock<IPasswordHasher<User>>();
            var userValidators = new List<IUserValidator<User>>();
            var passwordValidators = new List<IPasswordValidator<User>>();
            var keyNormalizer = new Mock<ILookupNormalizer>();
            var errors = new Mock<IdentityErrorDescriber>();
            var services = new Mock<IServiceProvider>();
            var logger = new Mock<ILogger<UserManager<User>>>();

            _mockUserManager = new Mock<UserManager<User>>(
                userStore.Object, null, passwordHasher.Object, userValidators, passwordValidators, keyNormalizer.Object, errors.Object, services.Object, logger.Object);

            _mockCache = new Mock<IMemoryCache>();
            _userManagementService = new UserManagementService(_context, _mockUserManager.Object, _mockCache.Object);
        }

        [Fact]
        public async Task AddUserAsync_WithValidData_ReturnsSuccess()
        {
            Console.WriteLine("=== Starting Test: AddUserAsync_WithValidData_ReturnsSuccess ===");

            // Arrange
            Console.WriteLine("Arrange: Setting up test data");
            var name = "Test User";
            var email = "test@example.com";
            var password = "Password123!";
            var role = "student";

            Console.WriteLine($"Arrange: User data - Name: {name}, Email: {email}, Role: {role}");

            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<User>(), password))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.AddToRoleAsync(It.IsAny<User>(), role))
                .ReturnsAsync(IdentityResult.Success);
            Console.WriteLine("Arrange: Mock setup completed");

            // Act
            Console.WriteLine("Act: Calling AddUserAsync");
            var result = await _userManagementService.AddUserAsync(name, email, password, role, null, null, null, null, null);
            Console.WriteLine($"Act: Result received - Success: {result.success}, Message: {result.message}");

            // Assert
            Console.WriteLine("Assert: Verifying user was added successfully");
            Assert.True(result.success);
            Assert.Equal("User added successfully!", result.message);
            Console.WriteLine("Assert: Test passed successfully");
        }

        [Fact]
        public async Task AddUserAsync_WithDuplicateEmail_ReturnsError()
        {
            Console.WriteLine("=== Starting Test: AddUserAsync_WithDuplicateEmail_ReturnsError ===");

            // Arrange
            Console.WriteLine("Arrange: Setting up test data");
            var name = "Test User";
            var email = "test@example.com";
            var password = "Password123!";
            var role = "student";

            Console.WriteLine($"Arrange: User data - Name: {name}, Email: {email}, Role: {role}");
            Console.WriteLine("Arrange: Simulating duplicate email scenario");

            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<User>(), password))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Email already exists" }));
            Console.WriteLine("Arrange: Mock setup completed with failure scenario");

            // Act
            Console.WriteLine("Act: Calling AddUserAsync with duplicate email");
            var result = await _userManagementService.AddUserAsync(name, email, password, role, null, null, null, null, null);
            Console.WriteLine($"Act: Result received - Success: {result.success}, Message: {result.message}");

            // Assert
            Console.WriteLine("Assert: Verifying error was returned for duplicate email");
            Assert.False(result.success);
            Assert.Equal("Email already exists", result.message);
            Console.WriteLine("Assert: Test passed successfully");
        }

        [Fact]
        public async Task RemoveStudentFromCourseAsync_WithExistingAssignment_ReturnsSuccess()
        {
            Console.WriteLine("=== Starting Test: RemoveStudentFromCourseAsync_WithExistingAssignment_ReturnsSuccess ===");

            // Arrange
            Console.WriteLine("Arrange: Setting up test data");
            var studentId = 1;
            var courseId = 1;
            var studentCourse = new StudentCourse { StudentId = studentId, CourseId = courseId, EnrollmentDate = DateTime.Now };
            _context.StudentCourses.Add(studentCourse);
            await _context.SaveChangesAsync();

            Console.WriteLine($"Arrange: Created enrollment - StudentId: {studentId}, CourseId: {courseId}");

            // Check before removal
            var beforeRemoval = await _context.StudentCourses.FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);
            Console.WriteLine($"Before removal: Enrollment exists = {beforeRemoval != null}");

            // Act
            Console.WriteLine("Act: Calling RemoveStudentFromCourseAsync");
            var result = await _userManagementService.RemoveStudentFromCourseAsync(studentId, courseId);
            Console.WriteLine($"Act: Result - Success: {result.success}, Message: {result.message}");

            // Assert
            Console.WriteLine("Assert: Verifying student was removed from course");
            Assert.True(result.success);
            Assert.Equal("Student removed from course successfully!", result.message);

            var assignment = await _context.StudentCourses.FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);
            Console.WriteLine($"Assert: Enrollment still exists = {assignment != null}");

            Assert.Null(assignment);
            Console.WriteLine("Assert: Test passed successfully");
        }

        [Fact]
        public async Task AssignStudentToCourseAsync_WithValidData_ReturnsSuccess()
        {
            Console.WriteLine("=== Starting Test: AssignStudentToCourseAsync_WithValidData_ReturnsSuccess ===");

            // Arrange
            Console.WriteLine("Arrange: Setting up test data");
            var student = new Student { StudentId = 1, UserId = "student1", MajorId = 1 };
            var course = new Course { CourseId = 1, CourseName = "Test Course" };
            _context.Students.Add(student);
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var studentId = student.StudentId;
            var courseId = course.CourseId;

            Console.WriteLine($"Arrange: StudentId={studentId}, CourseId={courseId}");

            // Check initial state
            var initialCount = await _context.StudentCourses.CountAsync();
            Console.WriteLine($"Initial enrollments: {initialCount}");

            // Act
            Console.WriteLine("Act: Calling AssignStudentToCourseAsync");
            var result = await _userManagementService.AssignStudentToCourseAsync(studentId, courseId);
            Console.WriteLine($"Act: Result - Success={result.success}, Message={result.message}");

            // Assert
            Console.WriteLine("Assert: Verifying enrollment was created");
            Assert.True(result.success);
            Assert.Equal("Student assigned to course successfully!", result.message);

            var assignment = await _context.StudentCourses.FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);
            Assert.NotNull(assignment);
            Assert.Equal(DateTime.Now.Date, assignment.EnrollmentDate.Date);
            Console.WriteLine("Assert: Test passed successfully");
        }
    }
}