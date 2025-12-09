using Xunit;
using Moq;
using Microsoft.AspNetCore.Identity;
using SIMS.Data;
using SIMS.Models;
using SIMS.Models.ViewModels;
using SIMS.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SIMS.Tests
{
    public class AccountServiceTests
    {
        private readonly Mock<SignInManager<User>> _mockSignInManager;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly ApplicationDbContext _context;
        private readonly AccountService _accountService;

        public AccountServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
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

            // Mock SignInManager dependencies
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
            var optionsAccessor = new Mock<IOptions<IdentityOptions>>();
            var loggerSignIn = new Mock<ILogger<SignInManager<User>>>();

            _mockSignInManager = new Mock<SignInManager<User>>(
                _mockUserManager.Object, contextAccessor.Object, claimsFactory.Object, optionsAccessor.Object, loggerSignIn.Object, null, null);

            _accountService = new AccountService(_mockSignInManager.Object, _mockUserManager.Object, _context);
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsSuccess()
        {
            Console.WriteLine("=== Starting Test: LoginAsync_WithValidCredentials_ReturnsSuccess ===");

            // Arrange
            Console.WriteLine("Arrange: Setting up test data");
            var email = "test@example.com";
            var password = "validpassword";
            var rememberMe = false;
            var expectedResult = SignInResult.Success;

            Console.WriteLine($"Arrange: Email={email}, Password={password}, RememberMe={rememberMe}");
            _mockSignInManager.Setup(sm => sm.PasswordSignInAsync(email, password, rememberMe, false))
                .ReturnsAsync(expectedResult);
            Console.WriteLine("Arrange: Mock setup completed");

            // Act
            Console.WriteLine("Act: Calling LoginAsync");
            var result = await _accountService.LoginAsync(email, password, rememberMe);
            Console.WriteLine($"Act: Result received - {result}");

            // Assert
            Console.WriteLine("Assert: Verifying result");
            Assert.Equal(expectedResult, result);
            Console.WriteLine("Assert: Test passed successfully");
        }

        [Fact]
        public async Task LoginAsync_WithInvalidPassword_ReturnsFailure()
        {
            Console.WriteLine("=== Starting Test: LoginAsync_WithInvalidPassword_ReturnsFailure ===");

            // Arrange
            Console.WriteLine("Arrange: Setting up test data");
            var email = "test@example.com";
            var password = "invalidpassword";
            var rememberMe = false;
            var expectedResult = SignInResult.Failed;

            Console.WriteLine($"Arrange: Email={email}, Password={password}, RememberMe={rememberMe}");
            _mockSignInManager.Setup(sm => sm.PasswordSignInAsync(email, password, rememberMe, false))
                .ReturnsAsync(expectedResult);
            Console.WriteLine("Arrange: Mock setup completed");

            // Act
            Console.WriteLine("Act: Calling LoginAsync");
            var result = await _accountService.LoginAsync(email, password, rememberMe);
            Console.WriteLine($"Act: Result received - {result}");

            // Assert
            Console.WriteLine("Assert: Verifying result");
            Assert.Equal(expectedResult, result);
            Console.WriteLine("Assert: Test passed successfully");
        }

        [Fact]
        public async Task GetProfileAsync_WithValidUser_ReturnsCorrectRole()
        {
            Console.WriteLine("=== Starting Test: GetProfileAsync_WithValidUser_ReturnsCorrectRole ===");

            // Arrange
            Console.WriteLine("Arrange: Setting up test data");
            var userId = "user123";
            var user = new User { Id = userId, Name = "Test User", Email = "test@example.com", Role = "student" };
            var student = new Student { StudentId = 1, UserId = userId, MajorId = 1 };
            var major = new Major { MajorId = 1, Name = "Computer Science", DepartmentId = 1 };
            var department = new Department { DepartmentId = 1, Name = "Engineering" };

            Console.WriteLine($"Arrange: UserId={userId}, User={user.Name}, Role={user.Role}");
            Console.WriteLine($"Arrange: StudentId={student.StudentId}, MajorId={student.MajorId}");
            Console.WriteLine($"Arrange: Major={major.Name}, Department={department.Name}");

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);
            _context.Students.Add(student);
            _context.Majors.Add(major);
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            Console.WriteLine("Arrange: Database setup completed");

            // Act
            Console.WriteLine("Act: Calling GetProfileAsync");
            var result = await _accountService.GetProfileAsync(userId);
            Console.WriteLine($"Act: Result received - Role={result?.Role}, Major={result?.MajorName}, Department={result?.DepartmentName}");

            // Assert
            Console.WriteLine("Assert: Verifying result");
            Assert.NotNull(result);
            Assert.Equal("student", result.Role);
            Assert.Equal("Computer Science", result.MajorName);
            Assert.Equal("Engineering", result.DepartmentName);
            Console.WriteLine("Assert: Test passed successfully");
        }

        [Fact]
        public async Task ChangePasswordAsync_WithValidData_ReturnsSuccess()
        {
            Console.WriteLine("=== Starting Test: ChangePasswordAsync_WithValidData_ReturnsSuccess ===");

            // Arrange
            Console.WriteLine("Arrange: Setting up test data");
            var userId = "user123";
            var user = new User { Id = userId, Name = "Test User", Email = "test@example.com" };
            var currentPassword = "oldpassword";
            var newPassword = "newpassword123";
            var confirmPassword = "newpassword123";

            Console.WriteLine($"Arrange: UserId={userId}, CurrentPassword={currentPassword}, NewPassword={newPassword}");

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.ChangePasswordAsync(user, currentPassword, newPassword))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            Console.WriteLine("Act: Calling ChangePasswordAsync");
            var result = await _accountService.ChangePasswordAsync(userId, currentPassword, newPassword, confirmPassword);
            Console.WriteLine($"Act: Result - Success={result.Success}, Message={result.Message}");

            // Assert
            Console.WriteLine("Assert: Verifying password change");
            Assert.True(result.Success);
            Assert.Equal("Password changed successfully", result.Message);
            Console.WriteLine("Assert: Test passed successfully");
        }

        [Fact]
        public async Task UpdateProfileAsync_WithValidData_ReturnsSuccess()
        {
            Console.WriteLine("=== Starting Test: UpdateProfileAsync_WithValidData_ReturnsSuccess ===");

            // Arrange
            Console.WriteLine("Arrange: Setting up test data");
            var userId = "user123";
            var user = new User { Id = userId, Name = "Old Name", Email = "old@example.com" };
            var model = new ProfileUpdateModel
            {
                Name = "New Name",
                Email = "new@example.com",
                Phone = "1234567890"
            };

            Console.WriteLine($"Arrange: UserId={userId}, OldName={user.Name}, NewName={model.Name}");

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);

            // Act
            Console.WriteLine("Act: Calling UpdateProfileAsync");
            var result = await _accountService.UpdateProfileAsync(userId, model);
            Console.WriteLine($"Act: Result - Success={result.Success}, Message={result.Message}, EmailChanged={result.EmailChanged}");

            // Assert
            Console.WriteLine("Assert: Verifying profile update");
            Assert.True(result.Success);
            Assert.Equal("Profile updated successfully! Your new email will be used for future logins.", result.Message);
            Assert.True(result.EmailChanged); // Email was changed
            Assert.Equal("new@example.com", result.NewEmail);
            Console.WriteLine("Assert: Test passed successfully");
        }
    }
}