using BulkyBook.Business.Services.IServices;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.RoleAdmin + "," + SD.RoleEmployee)]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IApplicationUserService _userService;

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
            IApplicationUserService userService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _userService = userService;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        // ✅ Parametre ismi userId yapıldı
        public async Task<IActionResult> RoleManagment(string userId)
        {
            var user = await _userService.GetUserByIDAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            RoleManagmentVM RoleVM = new()
            {
                ApplicationUser = user,
                RoleList = _roleManager.Roles.Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Text = u.Name,
                    Value = u.Name
                })
            };
            RoleVM.ApplicationUser.Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

            return View(RoleVM);
        }

        [HttpPost]
        public async Task<IActionResult> RoleManagment(RoleManagmentVM roleManagmentVM)
        {
            var user = await _userService.GetUserByIDAsync(roleManagmentVM.ApplicationUser.Id);

            if (user == null)
            {
                return NotFound();
            }

            string oldRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

            if (roleManagmentVM.ApplicationUser.Role != oldRole)
            {
                if (!string.IsNullOrEmpty(oldRole))
                {
                    await _userManager.RemoveFromRoleAsync(user, oldRole);
                }
                await _userManager.AddToRoleAsync(user, roleManagmentVM.ApplicationUser.Role);
            }

            TempData["success"] = "Role has been updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ChangePassword(string userId)
        {
            var user = await _userService.GetUserByIDAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            AdminChangePasswordVM adminChangePasswordVM = new()
            {
                UserId = user.Id,
                UserEmail = user.Email,
                UserName = user.Name
            };

            return View(adminChangePasswordVM);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(AdminChangePasswordVM adminChangePasswordVM)
        {
            var user = await _userService.GetUserByIDAsync(adminChangePasswordVM.UserId);

            if (user == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                adminChangePasswordVM.UserEmail = user.Email;
                adminChangePasswordVM.UserName = user.Name;
                return View(adminChangePasswordVM);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, adminChangePasswordVM.NewPassword);

            if (result.Succeeded)
            {
                TempData["success"] = $"Password for {user.Email} has been changed successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            adminChangePasswordVM.UserEmail = user.Email;
            adminChangePasswordVM.UserName = user.Name;
            return View(adminChangePasswordVM);
        }

        #region API calls
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllUsersAsync();
            var userList = new List<object>();

            foreach (var user in users)
            {
                var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "No Role";
                userList.Add(new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    phoneNumber = user.PhoneNumber,
                    role = role,
                    lockoutEnd = user.LockoutEnd
                });
            }

            return Json(new { data = userList });
        }

        [HttpPost]
        public async Task<IActionResult> LockUnlock([FromBody] string userId)
        {
            var user = await _userService.GetUserByIDAsync(userId);

            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            await _userManager.SetLockoutEnabledAsync(user, true);

            if (await _userManager.IsLockedOutAsync(user))
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow);
                return Json(new { success = true, message = "User unlocked successfully" });
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(1000));
                return Json(new { success = true, message = "User locked successfully" });
            }
        }
        #endregion
    }
}