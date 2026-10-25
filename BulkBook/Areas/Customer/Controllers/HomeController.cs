using BulkyBook.Business.Services.IServices;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BulkyBook.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly IProductService _productService;
        private readonly IShoppingCartService _shoppingCartService;

        public HomeController(IProductService productService, IShoppingCartService shoppingCartService)
        {
            _productService = productService;
            _shoppingCartService = shoppingCartService;
        }

        public async Task<IActionResult> Index()
        {
            var product = await _productService.GetAllProductsAsync(includeCategory:true);
            return View(product);
        }

        public async Task<IActionResult> Details(int productID)
        {
            var product = await _productService.GetProductByIDAsync(productID, includeCategory:true);
            if(product == null)
            {
                return NotFound();
            }

            ShoppingCart cart = new()
            {
                Product = product,
                Count = 1,
                ProductID = productID
            };

            return View(cart);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Details(ShoppingCart shoppingCart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userID = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userID))
            {
                return Unauthorized();
            }

            shoppingCart.ApplicationUserID = userID;
            await _shoppingCartService.AddToCartAsync(shoppingCart);
            var count = await _shoppingCartService.GetCartCountAsync(userID);
            HttpContext.Session.SetInt32(SD.SessionCart, count);
            TempData["success"] = "Item added to cart";
            return RedirectToAction("Index");
        }
    }
}
