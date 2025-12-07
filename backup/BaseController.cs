using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SIMS.Models;

namespace SIMS.Controllers
{
    public class BaseController : Controller
    {
        protected readonly UserManager<User> _userManager;

        public BaseController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (User.Identity!.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    ViewBag.UserRole = user.Role;
                    ViewBag.UserName = user.Name;
                    ViewBag.UserAvatar = user.Avatar;
                }
            }

            await next();
        }
    }
}