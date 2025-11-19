using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;
using SIMS.Models.ViewModels;

namespace SIMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
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
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                
                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    return RedirectToAction("Dashboard", "Home");
                }
                
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            ViewBag.Majors = await _context.Majors.Include(m => m.Department).ToListAsync();
            ViewBag.Departments = await _context.Departments.ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Name = model.Name,
                    Phone = model.Phone,
                    StudentCode = model.StudentCode,
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender,
                    Address = model.Address,
                    Role = model.Role
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                
                if (result.Succeeded)
                {
                    // Create role-specific record
                    switch (model.Role.ToLower())
                    {
                        case "student":
                            if (model.MajorId.HasValue)
                            {
                                var student = new Student
                                {
                                    UserId = user.Id,
                                    MajorId = model.MajorId.Value
                                };
                                _context.Students.Add(student);
                            }
                            break;
                        case "lecturer":
                            if (model.DepartmentId.HasValue)
                            {
                                var lecturer = new Lecturer
                                {
                                    UserId = user.Id,
                                    DepartmentId = model.DepartmentId.Value
                                };
                                _context.Lecturers.Add(lecturer);
                            }
                            break;
                        case "admin":
                            var admin = new Admin
                            {
                                UserId = user.Id
                            };
                            _context.Admins.Add(admin);
                            break;
                    }
                    
                    await _context.SaveChangesAsync();
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Dashboard", "Home");
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            
            ViewBag.Majors = await _context.Majors.Include(m => m.Department).ToListAsync();
            ViewBag.Departments = await _context.Departments.ToListAsync();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
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

                // Update user properties
                user.Name = model.Name;
                user.Phone = model.Phone;
                user.StudentCode = model.StudentCode;
                user.DateOfBirth = model.DateOfBirth;
                user.Gender = model.Gender;
                user.Address = model.Address;

                var result = await _userManager.UpdateAsync(user);
                
                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "Profile updated successfully" });
                }
                
                return Json(new { success = false, message = "Failed to update profile" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class ProfileUpdateModel
        {
            public string Name { get; set; } = "";
            public string? Phone { get; set; }
            public string? StudentCode { get; set; }
            public DateTime? DateOfBirth { get; set; }
            public string? Gender { get; set; }
            public string? Address { get; set; }
        }
    }
}