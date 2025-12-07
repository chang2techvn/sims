using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using SIMS.Data;
using SIMS.Models;
using SIMS.Services.Interfaces;

namespace SIMS.Services.Implementations
{
    public class ImportExportService : IImportExportService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ImportExportService> _logger;
        private readonly IMemoryCache _cache;
        private const string USER_STATS_CACHE_KEY = "UserStatistics";

        public ImportExportService(
            ApplicationDbContext context,
            UserManager<User> userManager,
            ILogger<ImportExportService> logger,
            IMemoryCache cache)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _cache = cache;
        }

        public async Task<ImportResult> ImportUsersAsync(IFormFile file, bool skipDuplicates = true)
        {
            var result = new ImportResult();

            if (file == null || file.Length == 0)
            {
                result.Success = false;
                result.Message = "Please select a file to import.";
                return result;
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (fileExtension != ".csv" && fileExtension != ".xlsx" && fileExtension != ".xls")
            {
                result.Success = false;
                result.Message = "Only CSV and Excel files are supported.";
                return result;
            }

            var users = new List<UserImportModel>();

            try
            {
                if (fileExtension == ".csv")
                {
                    users = await ParseCsvFile(file);
                }
                else
                {
                    users = ParseExcelFile(file);
                }

                if (!users.Any())
                {
                    result.Success = false;
                    result.Message = "No valid data found in the file.";
                    return result;
                }

                _logger.LogInformation($"Parsed {users.Count} users from file");

                var importedCount = 0;
                var skippedCount = 0;

                // Get default major for students
                var defaultMajor = await _context.Majors.FirstOrDefaultAsync();
                if (defaultMajor == null)
                {
                    result.Success = false;
                    result.Message = "No majors found in database. Please create at least one major before importing students.";
                    return result;
                }
                _logger.LogInformation($"Using default major: {defaultMajor.Name} (ID: {defaultMajor.MajorId})");

                foreach (var userData in users)
                {
                    try
                    {
                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(userData.Name) ||
                            string.IsNullOrWhiteSpace(userData.Email) ||
                            string.IsNullOrWhiteSpace(userData.Password) ||
                            string.IsNullOrWhiteSpace(userData.Role))
                        {
                            result.Errors.Add($"Row {users.IndexOf(userData) + 2}: Missing required fields (Name, Email, Password, Role)");
                            _logger.LogWarning($"Skipping user {userData.Email}: Missing required fields");
                            continue;
                        }

                        // Check for duplicate email if skipDuplicates is true
                        if (skipDuplicates)
                        {
                            var existingUser = await _userManager.FindByEmailAsync(userData.Email);
                            if (existingUser != null)
                            {
                                skippedCount++;
                                continue;
                            }
                        }

                        // Validate role
                        var validRoles = new[] { "admin", "lecturer", "student" };
                        if (!validRoles.Contains(userData.Role.ToLower()))
                        {
                            result.Errors.Add($"Row {users.IndexOf(userData) + 2}: Invalid role '{userData.Role}'. Must be Admin, Lecturer, or Student");
                            _logger.LogWarning($"Skipping user {userData.Email}: Invalid role '{userData.Role}'");
                            continue;
                        }

                        _logger.LogInformation($"Creating user: {userData.Email} with role {userData.Role}");

                        // Create user
                        var user = new User
                        {
                            UserName = userData.Email,
                            Email = userData.Email,
                            Name = userData.Name,
                            Role = userData.Role,
                            StudentCode = userData.StudentCode,
                            DateOfBirth = userData.DateOfBirth,
                            Phone = userData.Phone,
                            Gender = userData.Gender,
                            Address = userData.Address,
                            EmailConfirmed = true
                        };

                        var createResult = await _userManager.CreateAsync(user, userData.Password);

                        if (createResult.Succeeded)
                        {
                            await _userManager.AddToRoleAsync(user, userData.Role);

                            // Create role-specific records
                            switch (userData.Role.ToLower())
                            {
                                case "student":
                                    var student = new Student { UserId = user.Id, MajorId = defaultMajor.MajorId };
                                    _context.Students.Add(student);
                                    _logger.LogInformation($"Created student record for {userData.Email}");
                                    break;
                                case "lecturer":
                                    var lecturer = new Lecturer { UserId = user.Id };
                                    _context.Lecturers.Add(lecturer);
                                    _logger.LogInformation($"Created lecturer record for {userData.Email}");
                                    break;
                                case "admin":
                                    var admin = new Admin { UserId = user.Id };
                                    _context.Admins.Add(admin);
                                    _logger.LogInformation($"Created admin record for {userData.Email}");
                                    break;
                            }

                            await _context.SaveChangesAsync();
                            importedCount++;
                            _logger.LogInformation($"Successfully imported user: {userData.Email}");
                        }
                        else
                        {
                            var errorMsg = string.Join(", ", createResult.Errors.Select(e => e.Description));
                            result.Errors.Add($"Row {users.IndexOf(userData) + 2}: {errorMsg}");
                            _logger.LogWarning($"Failed to create user {userData.Email}: {errorMsg}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Row {users.IndexOf(userData) + 2}: {ex.Message}");
                    }
                }

                await InvalidateUserStatsCache();

                var message = $"Imported {importedCount} users successfully.";
                if (skippedCount > 0)
                {
                    message += $" Skipped {skippedCount} duplicate emails.";
                }

                result.Success = true;
                result.Message = message;
                result.ImportedCount = importedCount;
                result.SkippedCount = skippedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing users");
                result.Success = false;
                result.Message = "Error processing file: " + ex.Message;
            }

            return result;
        }

        private async Task<List<UserImportModel>> ParseCsvFile(IFormFile file)
        {
            var users = new List<UserImportModel>();

            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();
            _logger.LogInformation($"CSV Content length: {content.Length}");

            // Reset stream
            file.OpenReadStream().Position = 0;

            using var reader2 = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader2, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                HasHeaderRecord = false // CSV file doesn't have header row
            });

            await foreach (var record in csv.GetRecordsAsync<UserImportModel>())
            {
                users.Add(record);
            }

            _logger.LogInformation($"Parsed {users.Count} records from CSV file");
            return users;
        }

        private List<UserImportModel> ParseExcelFile(IFormFile file)
        {
            var users = new List<UserImportModel>();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage(file.OpenReadStream());
            var worksheet = package.Workbook.Worksheets[0];

            // Skip header row
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var user = new UserImportModel
                {
                    Name = worksheet.Cells[row, 1].Text?.Trim(),
                    Email = worksheet.Cells[row, 2].Text?.Trim(),
                    Password = worksheet.Cells[row, 3].Text?.Trim(),
                    Role = worksheet.Cells[row, 4].Text?.Trim(),
                    StudentCode = worksheet.Cells[row, 5].Text?.Trim(),
                    DateOfBirth = ParseDate(worksheet.Cells[row, 6].Text),
                    Phone = worksheet.Cells[row, 7].Text?.Trim(),
                    Gender = worksheet.Cells[row, 8].Text?.Trim(),
                    Address = worksheet.Cells[row, 9].Text?.Trim()
                };

                users.Add(user);
            }

            return users;
        }

        private DateTime? ParseDate(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return null;

            if (DateTime.TryParse(dateString, out var date))
                return date;

            return null;
        }

        private async Task InvalidateUserStatsCache()
        {
            _cache.Remove(USER_STATS_CACHE_KEY);
            await Task.CompletedTask;
        }
    }

    public class UserImportModel
    {
        [CsvHelper.Configuration.Attributes.Index(0)]
        public string? Name { get; set; }
        [CsvHelper.Configuration.Attributes.Index(1)]
        public string? Email { get; set; }
        [CsvHelper.Configuration.Attributes.Index(2)]
        public string? Password { get; set; }
        [CsvHelper.Configuration.Attributes.Index(3)]
        public string? Role { get; set; }
        [CsvHelper.Configuration.Attributes.Index(4)]
        public string? StudentCode { get; set; }
        [CsvHelper.Configuration.Attributes.Index(5)]
        public DateTime? DateOfBirth { get; set; }
        [CsvHelper.Configuration.Attributes.Index(6)]
        public string? Phone { get; set; }
        [CsvHelper.Configuration.Attributes.Index(7)]
        public string? Gender { get; set; }
        [CsvHelper.Configuration.Attributes.Index(8)]
        public string? Address { get; set; }
    }
}