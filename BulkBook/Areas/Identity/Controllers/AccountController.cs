using BulkyBook.Business.Services;
using BulkyBook.Business.Services.IServices;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyBook.Areas.Identity.Controllers
{
    [Area("Identity")]

    public class AccountController : Controller
    {

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IShoppingCartService _shoppingCartService;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, 
            RoleManager<IdentityRole> roleManager, IShoppingCartService shoppingCartService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _shoppingCartService = shoppingCartService;
        }

        public IActionResult Login(string? returnURL = null)
        {
            ViewData["ReturnURL"] = returnURL;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM loginVM, string? returnURL = null)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(loginVM.Email);

                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(user.UserName, loginVM.Password, loginVM.RememberMe, lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        if (!string.IsNullOrEmpty(returnURL) && Url.IsLocalUrl(returnURL))
                        {
                            return Redirect(returnURL);
                        }

                        if(User.IsInRole(SD.RoleAdmin) || User.IsInRole(SD.RoleEmployee))
                        {
                            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                        }
                        return RedirectToAction("Index", "Home", new { area = "Customer" });
                    }
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View(loginVM);
        }

        public IActionResult Register(string? returnURL = null)
        {
            var model = new RegisterVM();

            if (User.IsInRole(SD.RoleAdmin))
            {
                model.RoleList =
                [
                    new SelectListItem { Text = SD.RoleCustomer, Value = SD.RoleCustomer },
                    new SelectListItem { Text = SD.RoleAdmin, Value = SD.RoleAdmin },
                    new SelectListItem { Text = SD.RoleEmployee, Value = SD.RoleEmployee },
                ];
            }
            else { 
            model.RoleList = new List<SelectListItem>();
                    }
            ViewData["ReturnURL"] = returnURL;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM registerVM, string? returnURL = null)
        {

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = registerVM.Name,
                    Email = registerVM.Email,
                    PhoneNumber = registerVM.PhoneNumber,
                    StreetAdrress = registerVM.StreetAdrress,
                    City = registerVM.City,
                    State = registerVM.State,
                    PostalCode = registerVM.PostalCode,
                };

                var result = await _userManager.CreateAsync(user, registerVM.Password);

                if (result.Succeeded)
                {
                    if(User.IsInRole(SD.RoleAdmin) && !string.IsNullOrEmpty(registerVM.Role))
                    {
                        await _userManager.AddToRoleAsync(user, registerVM.Role);
                        TempData["success"] = $"User '{user.Name}' created successfully with role '{registerVM.Role}'.";
                        return RedirectToAction("Index", "User", new {area="Admin"});
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, SD.RoleCustomer);
                        await _signInManager.SignInAsync(user, isPersistent: false);

                        if (!string.IsNullOrEmpty(returnURL) && Url.IsLocalUrl(returnURL))
                        {
                            return Redirect(returnURL);
                        }

                        return RedirectToAction("Index", "Home", new { area = "Customer" });
                    }
                }

                foreach(var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(registerVM);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.SetInt32(SD.SessionCart, 0);
            return RedirectToAction("Index", "Home", new { area = "Customer" });
        }
    }
}
