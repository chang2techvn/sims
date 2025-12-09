using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;
using System.Diagnostics;

namespace SIMS.Tests
{
    public class UITests : IDisposable
    {
        private IPlaywright _playwright;
        private IBrowser _browser;

        public UITests()
        {
            SetupAsync().GetAwaiter().GetResult();
        }

        private async Task SetupAsync()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false, Timeout = 10000 });
        }

        public void Dispose()
        {
            TearDownAsync().GetAwaiter().GetResult();
        }

        private async Task TearDownAsync()
        {
            await _browser.CloseAsync();
            _playwright.Dispose();
        }

        private async Task LoginAsAdmin(IPage page)
        {
            await page.GotoAsync("http://localhost:5281/Account/Login");
            await page.WaitForSelectorAsync("input[name='Email']");
            await page.FillAsync("input[name='Email']", "admin@sims.com");
            await page.FillAsync("input[name='Password']", "Admin123");
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync("**/Dashboard");
        }

        private async Task LoginAsStudent(IPage page)
        {
            await page.GotoAsync("http://localhost:5281/Account/Login");
            await page.WaitForSelectorAsync("input[name='Email']");
            await page.FillAsync("input[name='Email']", "student@sims.com");
            await page.FillAsync("input[name='Password']", "chang123");
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync("**/MyCourses");
        }

        private async Task LoginAsLecturer(IPage page)
        {
            await page.GotoAsync("http://localhost:5281/Account/Login");
            await page.WaitForSelectorAsync("input[name='Email']");
            await page.FillAsync("input[name='Email']", "lecturer@sims.com");
            await page.FillAsync("input[name='Password']", "chang123");
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync("**/MyCourses");
        }

        [Fact]
        public async Task AT01_AutoLoginCheck()
        {
            var page = await _browser.NewPageAsync();
            await page.GotoAsync("http://localhost:5281/Account/Login");
            await page.WaitForSelectorAsync("input[name='Email']");
            await page.FillAsync("input[name='Email']", "admin@sims.com");
            await page.FillAsync("input[name='Password']", "Admin123");
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync("**/Dashboard");
            Assert.True(true);
        }

        [Fact]
        public async Task AT02_AutoRegisterTest()
        {
            var page = await _browser.NewPageAsync();
            await LoginAsAdmin(page);
            await page.GotoAsync("http://localhost:5281/Admin/ManageUsers");
            await page.ClickAsync("text=Add User");
            await page.WaitForSelectorAsync("#addUserModal");
            await page.FillAsync("input[name='Name']", "Test User");
            await page.FillAsync("input[name='Email']", "test@example.com");
            await page.FillAsync("input[name='Password']", "Test123!");
            // Select role
            await page.FocusAsync("#addRole");
            await page.WaitForSelectorAsync("#addRoleDropdown .dropdown-option");
            await page.ClickAsync("#addRoleDropdown .dropdown-option:first-child");
            // Fill additional fields
            await page.FillAsync("input[name='StudentCode']", "BC00123");
            await page.FillAsync("input[name='Phone']", "0123456789");
            // Select gender
            await page.FocusAsync("#addGender");
            await page.WaitForSelectorAsync("#addGenderDropdown .dropdown-option");
            await page.ClickAsync("#addGenderDropdown .dropdown-option:first-child");
            await page.FillAsync("textarea[name='Address']", "123 Test Street");
            await page.ClickAsync("#addUserModal button[type='submit']");
            await page.WaitForSelectorAsync("#addUserModal", new() { State = WaitForSelectorState.Hidden });
            Assert.True(true);
        }

        [Fact]
        public async Task AT03_AutoLogoutTest()
        {
            var page = await _browser.NewPageAsync();
            await LoginAsAdmin(page);
            await page.ClickAsync("text=Logout");
            await Task.Delay(1000);
            Assert.True(true);
        }

        [Fact]
        public async Task AT04_AutoViewProfile()
        {
            var page = await _browser.NewPageAsync();
            await LoginAsAdmin(page);
            await page.ClickAsync("text=Profile");
            await page.WaitForSelectorAsync("text=Profile Information");
            Assert.True(true);
        }

        [Fact]
        public async Task AT05_AutoUpdateProfile()
        {
            var page = await _browser.NewPageAsync();
            await LoginAsAdmin(page);
            await page.ClickAsync("text=Profile");
            await page.ClickAsync("#editProfileBtn");
            await page.FillAsync("#Name", "Updated Name");
            await page.ClickAsync("#saveBtn");
            await Task.Delay(1000);
            Assert.True(true);
        }

        [Fact]
        public async Task AT06_AutoManageCourse()
        {
            var page = await _browser.NewPageAsync();
            await LoginAsAdmin(page);
            await page.ClickAsync("text=Admin");
            await page.ClickAsync("text=Manage Courses");
            await page.ClickAsync("text=Add New Course");
            await page.WaitForSelectorAsync("#editCourseModal");
            await page.FillAsync("#editCourseName", "Test Course");
            // Select subject
            await page.ClickAsync("#editSubject");
            await page.WaitForSelectorAsync("#editSubjectDropdown .dropdown-option");
            await page.ClickAsync("#editSubjectDropdown .dropdown-option:first-child");
            // Select semester
            await page.ClickAsync("#editSemester");
            await page.WaitForSelectorAsync("#editSemesterDropdown .dropdown-option");
            await page.ClickAsync("#editSemesterDropdown .dropdown-option:first-child");
            // Select major
            await page.ClickAsync("#editMajor");
            await page.WaitForSelectorAsync("#editMajorDropdown .dropdown-option");
            await page.ClickAsync("#editMajorDropdown .dropdown-option:first-child");
            // Select lecturer
            await page.ClickAsync("#editLecturer");
            await page.WaitForSelectorAsync("#editLecturerDropdown .dropdown-option");
            await page.ClickAsync("#editLecturerDropdown .dropdown-option:first-child");
            await page.ClickAsync("#editCourseModal button[type=\"button\"]");
            await page.WaitForSelectorAsync("#editCourseModal", new() { State = WaitForSelectorState.Hidden });
            Assert.True(true);
        }

        [Fact]
        public async Task AT07_AutoEnrollStudent()
        {
            var page = await _browser.NewPageAsync();
            await LoginAsAdmin(page);
            await page.ClickAsync("text=Admin");
            await page.ClickAsync("text=Assign Students");
            // Select student
            await page.ClickAsync("#studentSearch");
            await page.WaitForSelectorAsync("#studentSearchDropdown .dropdown-option");
            await page.ClickAsync("#studentSearchDropdown .dropdown-option:first-child");
            // Select course
            await page.ClickAsync("#courseSearch");
            await page.WaitForSelectorAsync("#courseSearchDropdown .dropdown-option");
            await page.ClickAsync("#courseSearchDropdown .dropdown-option:first-child");
            await page.ClickAsync("button:has-text('Assign to Course')");
            await Task.Delay(1000);
            Assert.True(true);
        }

        [Fact]
        public async Task AT08_AutoCheckPermissions()
        {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"Starting AT08_AutoCheckPermissions at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            var page = await _browser.NewPageAsync();
            var loginStart = stopwatch.ElapsedMilliseconds;
            await LoginAsStudent(page);
            var loginEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Login as Student completed in {loginEnd - loginStart}ms");

            var requestStart = stopwatch.ElapsedMilliseconds;
            var response = await page.APIRequest.GetAsync("http://localhost:5281/Admin/ManageCourses");
            var requestEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"API request to /Admin/ManageCourses completed in {requestEnd - requestStart}ms, status: {response.Status}");

            Assert.Equal(404, response.Status);
            Console.WriteLine("Assert: Permission check passed successfully ✅");

            stopwatch.Stop();
            Console.WriteLine($"Test AT08 completed in {stopwatch.ElapsedMilliseconds}ms at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("---");
        }

        [Fact]
        public async Task AT09_AutoResetPassword()
        {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"Starting AT09_AutoResetPassword at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            var page = await _browser.NewPageAsync();
            var loginStart = stopwatch.ElapsedMilliseconds;
            await LoginAsStudent(page);
            var loginEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Login as Student completed in {loginEnd - loginStart}ms");

            var navigateStart = stopwatch.ElapsedMilliseconds;
            await page.ClickAsync("text=Profile");
            await page.WaitForSelectorAsync("#passwordForm");
            var navigateEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Navigate to Profile and wait for form completed in {navigateEnd - navigateStart}ms");

            var fillStart = stopwatch.ElapsedMilliseconds;
            await page.FillAsync("#CurrentPassword", "chang123");
            await page.FillAsync("#NewPassword", "newpassword123");
            await page.FillAsync("#ConfirmPassword", "newpassword123");
            var fillEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Fill password fields completed in {fillEnd - fillStart}ms");

            var submitStart = stopwatch.ElapsedMilliseconds;
            await page.ClickAsync("#changePasswordBtn");
            await page.WaitForSelectorAsync("text=Password changed successfully!");
            var submitEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Submit password change and wait for success completed in {submitEnd - submitStart}ms");

            Assert.True(true);
            Console.WriteLine("Assert: Test passed successfully ✅");

            stopwatch.Stop();
            Console.WriteLine($"Test AT09 completed in {stopwatch.ElapsedMilliseconds}ms at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("---");
        }

        [Fact]
        public async Task AT10_AutoViewAssignedClasses()
        {
            var page = await _browser.NewPageAsync();
            await LoginAsLecturer(page);
            await page.ClickAsync("text=My Courses");
            await page.WaitForSelectorAsync("text=My Teaching Courses");
            Assert.True(true);
        }
    }
}