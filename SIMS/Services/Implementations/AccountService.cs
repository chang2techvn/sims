using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;
using SIMS.Models.ViewModels;
using System.IO;

namespace SIMS.Services
{
    public class AccountService : IAccountService
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        public AccountService(SignInManager<User> signInManager, UserManager<User> userManager, ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }

        public async Task<Microsoft.AspNetCore.Identity.SignInResult> LoginAsync(string email, string password, bool rememberMe)
        {
            return await _signInManager.PasswordSignInAsync(email, password, rememberMe, false);
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task<ProfileViewModel?> GetProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var profile = new ProfileViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email!,
                Phone = user.Phone,
                StudentCode = user.StudentCode,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                Address = user.Address,
                Avatar = user.Avatar,
                Role = user.Role
            };

            // Get additional info based on role
            if (user.Role == "student")
            {
                var student = await _context.Students
                    .Include(s => s.Major)
                    .ThenInclude(m => m.Department)
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);

                if (student != null)
                {
                    profile.MajorName = student.Major.Name;
                    profile.DepartmentName = student.Major.Department.Name;
                }
            }
            else if (user.Role == "lecturer")
            {
                var lecturer = await _context.Lecturers
                    .Include(l => l.Department)
                    .FirstOrDefaultAsync(l => l.UserId == user.Id);

                if (lecturer != null)
                {
                    profile.DepartmentName = lecturer.Department.Name;
                }
            }

            return profile;
        }

        public async Task<(bool Success, string Message, bool EmailChanged, string? NewEmail)> UpdateProfileAsync(string userId, ProfileUpdateModel model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (false, "User not found", false, null);
            }

            // Check if email is changing
            bool emailChanged = false;
            if (!string.IsNullOrEmpty(model.Email) && user.Email != model.Email)
            {
                // Check if new email already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    return (false, "This email is already in use by another account.", false, null);
                }

                emailChanged = true;
            }

            // Update user properties
            user.Name = model.Name;
            // Update email if changed
            if (emailChanged)
            {
                user.Email = model.Email;
                user.NormalizedEmail = model.Email.ToUpper();
                user.UserName = model.Email; // Also update username since we use email as username
                user.NormalizedUserName = model.Email.ToUpper();
            }
            user.Phone = model.Phone;
            user.StudentCode = model.StudentCode;
            user.DateOfBirth = model.DateOfBirth;
            user.Gender = model.Gender;
            user.Address = model.Address;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // If email changed, update the sign-in to reflect new email
                if (emailChanged)
                {
                    await _signInManager.RefreshSignInAsync(user);
                }
                return (true, emailChanged
                    ? "Profile updated successfully! Your new email will be used for future logins."
                    : "Profile updated successfully!", emailChanged, model.Email);
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, $"Failed to update profile: {errors}", false, null);
        }

        public async Task<(bool Success, string Message)> ChangePasswordAsync(string userId, string currentPassword, string newPassword, string confirmPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (false, "User not found");
            }

            // Check if user is student or lecturer (not admin)
            if (user.Role.ToLower() != "student" && user.Role.ToLower() != "lecturer")
            {
                return (false, "Unauthorized");
            }

            // Validate new password confirmation
            if (newPassword != confirmPassword)
            {
                return (false, "New password and confirmation do not match");
            }

            // Change password
            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (result.Succeeded)
            {
                return (true, "Password changed successfully");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, errors);
            }
        }

        public async Task<(bool Success, string Message, string? AvatarUrl)> UploadAvatarAsync(string userId, IFormFile avatar, bool isAdmin = false, string? targetUserId = null)
        {
            User user;
            if (!string.IsNullOrEmpty(targetUserId))
            {
                if (!isAdmin)
                {
                    return (false, "Unauthorized", null);
                }
                // Admin uploading for another user
                user = await _userManager.FindByIdAsync(targetUserId);
            }
            else
            {
                // User uploading for themselves
                user = await _userManager.FindByIdAsync(userId);
            }

            if (user == null)
            {
                return (false, "User not found", null);
            }

            if (avatar == null || avatar.Length == 0)
            {
                return (false, "No file uploaded", null);
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(avatar.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return (false, "Invalid file type. Only JPG, PNG, and GIF are allowed.", null);
            }

            // Validate file size (max 5MB)
            if (avatar.Length > 5 * 1024 * 1024)
            {
                return (false, "File size must be less than 5MB.", null);
            }

            // Create uploads directory if it doesn't exist
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate unique filename
            var uniqueFileName = $"{user.Id}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatar.CopyToAsync(stream);
            }

            // Update user avatar in database
            var avatarUrl = $"/uploads/avatars/{uniqueFileName}";
            user.Avatar = avatarUrl;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return (true, "Avatar uploaded successfully", avatarUrl);
            }

            return (false, "Failed to update avatar in database", null);
        }
    }
}