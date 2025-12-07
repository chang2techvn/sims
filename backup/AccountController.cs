using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;
using SIMS.Models.ViewModels;

namespace SIMS.Controllers
{
    public class AccountController : BaseController
    {
        private readonly SignInManager<User> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ApplicationDbContext context) : base(userManager)
        {
            _signInManager = signInManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // isPersistent = true to create persistent cookie that survives browser closure
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email, 
                    model.Password, 
                    isPersistent: model.RememberMe, 
                    lockoutOnFailure: false);
                
                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    
                    // Store user info in session
                    HttpContext.Session.SetString("UserId", user.Id);
                    HttpContext.Session.SetString("UserName", user.Name ?? "");
                    HttpContext.Session.SetString("UserRole", user.Role ?? "");
                    
                    return RedirectToAction("Dashboard", "Home");
                }
                
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            
            // Clear session data
            HttpContext.Session.Clear();
            
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

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

            return View(profile);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateModel model)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }
                // Check if email is changing
                bool emailChanged = false;
                if (!string.IsNullOrEmpty(model.Email) && user.Email != model.Email)
                {
                    // Check if new email already exists
                    var existingUser = await _userManager.FindByEmailAsync(model.Email);
                    if (existingUser != null && existingUser.Id != user.Id)
                    {
                        return Json(new { success = false, message = "This email is already in use by another account." });
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
                    return Json(new { 
                        success = true, 
                        message = emailChanged 
                            ? "Profile updated successfully! Your new email will be used for future logins." 
                            : "Profile updated successfully!",
                        emailChanged = emailChanged,
                        newEmail = model.Email
                    });                }
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Json(new { success = false, message = $"Failed to update profile: {errors}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Check if user is student or lecturer (not admin)
                if (user.Role.ToLower() != "student" && user.Role.ToLower() != "lecturer")
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                // Validate new password confirmation
                if (NewPassword != ConfirmPassword)
                {
                    return Json(new { success = false, message = "New password and confirmation do not match" });
                }

                // Change password
                var result = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);
                
                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "Password changed successfully" });
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = errors });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar, string? userId = null)
        {
            try
            {
                User user;
                if (!string.IsNullOrEmpty(userId))
                {
                    // Check if current user is admin using ASP.NET Identity roles
                    if (User.IsInRole("Admin"))
                    {
                        // Admin uploading for another user
                        user = await _userManager.FindByIdAsync(userId);
                        Console.WriteLine($"Admin uploading for user: {userId}, user found: {user != null}");
                    }
                    else
                    {
                        Console.WriteLine("Unauthorized: Current user is not admin");
                        return Json(new { success = false, message = "Unauthorized" });
                    }
                }
                else
                {
                    // User uploading for themselves
                    user = await _userManager.GetUserAsync(User);
                    Console.WriteLine($"Self-upload user: {user?.Id}");
                }
                
                if (user == null)
                {
                    Console.WriteLine("Target user not found");
                    return Json(new { success = false, message = "User not found" });
                }

                if (avatar == null || avatar.Length == 0)
                {
                    return Json(new { success = false, message = "No file uploaded" });
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(avatar.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return Json(new { success = false, message = "Invalid file type. Only JPG, PNG, and GIF are allowed." });
                }

                // Validate file size (max 5MB)
                if (avatar.Length > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "File size must be less than 5MB." });
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
                    return Json(new { success = true, avatarUrl = avatarUrl, message = "Avatar uploaded successfully" });
                }

                return Json(new { success = false, message = "Failed to update avatar in database" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class ProfileUpdateModel
        {
            public string Name { get; set; } = "";
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string? StudentCode { get; set; }
            public DateTime? DateOfBirth { get; set; }
            public string? Gender { get; set; }
            public string? Address { get; set; }
        }
    }
}