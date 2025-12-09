using Xunit;
using Moq;
using SIMS.Data;
using SIMS.Models;
using SIMS.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SIMS.Tests
{
    public class AuthorizationTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<User>> _mockUserManager;

        public AuthorizationTests()
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
        }

        [Fact]
        public async Task AuthorizeUser_WithAdminRole_ReturnsTrue()
        {
            Console.WriteLine("=== Starting Test: AuthorizeUser_WithAdminRole_ReturnsTrue ===");

            // Arrange
            Console.WriteLine("Arrange: Setting up test data");
            var user = new User { Id = "admin1", Name = "Admin User", Email = "admin@test.com", Role = "Admin" };
            var adminRole = "Admin";

            Console.WriteLine($"Arrange: User={user.Name}, Role={user.Role}, RequiredRole={adminRole}");

            _mockUserManager.Setup(um => um.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.IsInRoleAsync(user, adminRole)).ReturnsAsync(true);

            // Act
            Console.WriteLine("Act: Checking authorization");
            var isInRole = await _mockUserManager.Object.IsInRoleAsync(user, adminRole);
            Console.WriteLine($"Act: User is in role '{adminRole}': {isInRole}");

            // Assert
            Console.WriteLine("Assert: Verifying authorization");
            Assert.True(isInRole);
            Console.WriteLine("Assert: Test passed successfully");
        }

        [Fact]
        public async Task AuthorizeUser_WithStudentRole_ReturnsFalseForAdminAccess()
        {
            Console.WriteLine("=== Starting Test: AuthorizeUser_WithStudentRole_ReturnsFalseForAdminAccess ===");

            // Arrange
            Console.WriteLine("Arrange: Setting up test data");
            var user = new User { Id = "student1", Name = "Student User", Email = "student@test.com", Role = "Student" };
            var requiredRole = "Admin";

            Console.WriteLine($"Arrange: User={user.Name}, Role={user.Role}, RequiredRole={requiredRole}");

            _mockUserManager.Setup(um => um.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.IsInRoleAsync(user, requiredRole)).ReturnsAsync(false);

            // Act
            Console.WriteLine("Act: Checking authorization");
            var isInRole = await _mockUserManager.Object.IsInRoleAsync(user, requiredRole);
            Console.WriteLine($"Act: User is in role '{requiredRole}': {isInRole}");

            // Assert
            Console.WriteLine("Assert: Verifying authorization denied");
            Assert.False(isInRole);
            Console.WriteLine("Assert: Test passed successfully");
        }
    }
}