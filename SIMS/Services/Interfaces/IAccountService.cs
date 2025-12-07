using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SIMS.Models;
using SIMS.Models.ViewModels;

namespace SIMS.Services
{
    public interface IAccountService
    {
        Task<Microsoft.AspNetCore.Identity.SignInResult> LoginAsync(string email, string password, bool rememberMe);
        Task LogoutAsync();
        Task<ProfileViewModel?> GetProfileAsync(string userId);
        Task<(bool Success, string Message, bool EmailChanged, string? NewEmail)> UpdateProfileAsync(string userId, ProfileUpdateModel model);
        Task<(bool Success, string Message)> ChangePasswordAsync(string userId, string currentPassword, string newPassword, string confirmPassword);
        Task<(bool Success, string Message, string? AvatarUrl)> UploadAvatarAsync(string userId, IFormFile avatar, bool isAdmin = false, string? targetUserId = null);
    }
}