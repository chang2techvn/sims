using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;
using SIMS.Models.ViewModels;
using SIMS.Services;

namespace SIMS.Controllers
{
    public class AccountController : BaseController
    {
        private readonly SignInManager<User> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IAccountService _accountService;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ApplicationDbContext context,
            IAccountService accountService) : base(userManager)
        {
            _signInManager = signInManager;
            _context = context;
            _accountService = accountService;
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
                var result = await _accountService.LoginAsync(
                    model.Email, 
                    model.Password, 
                    model.RememberMe);
                
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
            await _accountService.LogoutAsync();
            
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

            var profile = await _accountService.GetProfileAsync(user.Id);

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

                var result = await _accountService.UpdateProfileAsync(user.Id, model);

                return Json(new { 
                    success = result.Success, 
                    message = result.Message,
                    emailChanged = result.EmailChanged,
                    newEmail = result.NewEmail
                });
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

                var result = await _accountService.ChangePasswordAsync(user.Id, CurrentPassword, NewPassword, ConfirmPassword);

                return Json(new { success = result.Success, message = result.Message });
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
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                bool isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

                var result = await _accountService.UploadAvatarAsync(currentUser.Id, avatar, isAdmin, userId);

                return Json(new { success = result.Success, message = result.Message, avatarUrl = result.AvatarUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}