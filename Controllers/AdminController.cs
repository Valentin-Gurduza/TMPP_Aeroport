using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMPP_Aeroport.Models;
using TMPP_Aeroport.Data;

namespace TMPP_Aeroport.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _dbContext;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _dbContext.Users.ToListAsync();
            var userRoles = await _dbContext.UserRoles.ToListAsync();
            var roles = await _dbContext.Roles.ToListAsync();

            var userRolesViewModel = users.Select(user => new UserRolesViewModel
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Roles = userRoles.Where(ur => ur.UserId == user.Id)
                                 .Select(ur => roles.FirstOrDefault(r => r.Id == ur.RoleId)?.Name)
                                 .Where(name => name != null)
                                 .Select(name => name!)
                                 .ToList()
            }).ToList();

            return View(userRolesViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ManageRoles(string userId)
        {
            ViewBag.userId = userId;
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
                return View("NotFound");
            }

            ViewBag.UserName = user.Email;
            var model = new List<ManageUserRolesViewModel>();

            var userRoles = await _userManager.GetRolesAsync(user);

            foreach (var role in await _roleManager.Roles.ToListAsync())
            {
                var userRolesViewModel = new ManageUserRolesViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                    Selected = role.Name != null && userRoles.Contains(role.Name)
                };

                model.Add(userRolesViewModel);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ManageRoles(List<ManageUserRolesViewModel> model, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return View("NotFound");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var result = await _userManager.RemoveFromRolesAsync(user, roles);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot remove user existing roles");
                return View(model);
            }

            result = await _userManager.AddToRolesAsync(user, model.Where(x => x.Selected).Select(y => y.RoleName!));

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot add selected roles to user");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Roles updated successfully for user {user.Email}.";
            return RedirectToAction("Index");
        }
    }
}
