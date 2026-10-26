using BulkyBook.Business.Services.IServices;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyBook.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IEmailService _emailService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IApplicationUserService _applicationUserService;

        public CartController(IOrderService orderService, IShoppingCartService shoppingCartService, 
            IApplicationUserService applicationUserService, IEmailService emailService)
        {
            _orderService = orderService;
            _shoppingCartService = shoppingCartService;
            _applicationUserService = applicationUserService;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userID = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userID))
            {
                return Unauthorized();
            }

            var cartItems = await _shoppingCartService.GetUserCartItemsAsync(userID);
            var user = await _applicationUserService.GetUserByIDAsync(userID);
            ShoppingCartVM shoppingCartVM = new ()
            {
                ShoppingCartList = cartItems,
                OrderHeader = new()
            };

            shoppingCartVM.OrderHeader.ApplicationUser = user;
            shoppingCartVM.OrderHeader.ApplicationUserID = userID;
            shoppingCartVM.OrderHeader.Name = user.Name;
            shoppingCartVM.OrderHeader.PhoneNumber = user.PhoneNumber;
            shoppingCartVM.OrderHeader.StreetAdrress = user.StreetAdrress;
            shoppingCartVM.OrderHeader.City = user.City;
            shoppingCartVM.OrderHeader.State = user.State;
            shoppingCartVM.OrderHeader.PostalCode = user.PostalCode;

            foreach(var cart in shoppingCartVM.ShoppingCartList)
            {
                shoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(shoppingCartVM);
        }

        [HttpPost]
        [ActionName("Index")]
        public async Task<IActionResult> IndexPOST(ShoppingCartVM shoppingCartVM)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userID = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userID))
            {
                return Unauthorized();
            }

            var cartItems = await _shoppingCartService.GetUserCartItemsAsync(userID);
            shoppingCartVM.ShoppingCartList = cartItems;
            shoppingCartVM.OrderHeader.OrderDate = DateTime.UtcNow;
            shoppingCartVM.OrderHeader.ApplicationUserID = userID;

            foreach (var cart in shoppingCartVM.ShoppingCartList)
            {
                shoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            shoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            shoppingCartVM.OrderHeader.OrderDetails = shoppingCartVM.ShoppingCartList.Select(cart => new OrderDetails
            {
                ProductID = cart.ProductID,
                Price = cart.Price,
                Count = cart.Count,
            }).ToList();
            await _orderService.CreateOrderAync(shoppingCartVM.OrderHeader);

            try
            {
                var domain = Request.Scheme + "://" + Request.Host.Value + "/";


                var sessionUrl = await _orderService.CreateStripeCheckoutSessionAsync(shoppingCartVM.OrderHeader, shoppingCartVM.ShoppingCartList, domain);
                Response.Headers.Append("Location", sessionUrl);
                return new StatusCodeResult(303);
            }
            catch(StripeException ex)
            {
                TempData["error"] = "Payment procession failed. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> OrderConfirmation(int id)
        {
            var orderHeader = await _orderService.GetOrderByIDAsync(id, includeUser: true);
            if(orderHeader == null)
            {
                return NotFound();
            }

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userID = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userID))
            {
                return Unauthorized();
            }
            if(orderHeader.ApplicationUserID != userID)
            {
                return RedirectToAction("AccessDenied", "Account", new { area = "Identity" });
            }

            try
            {
                var result = await _orderService.VerifyStripePaymentAsync(orderHeader);

                if (result)
                {
                    TempData["success"] = "Payment completed successfully! Your order has been confirmed.";
                }
                else
                {
                    TempData["error"] = "Payment status is pending. Please contact support if you completed the payment.";
                }
            }
            catch (StripeException ex) 
            {
                TempData["error"] = "Unable to verify payment status. Please contact support with your order number.";
            }

            var user = await _applicationUserService.GetUserByIDAsync(userID);

            await _emailService.SendOrderConfirmationEmailAsync(user.Email,
                orderHeader.Id, (decimal)orderHeader.OrderTotal);

            return View(id);
        }


        public async Task<IActionResult> Plus(int cartID)
        {
            var cart = await _shoppingCartService.GetCartByIdAsync(cartID);

            if(cart != null)
            {
                if(cart.Count >= 1000)
                {
                }
                else 
                { 
                   cart.Count++;
                    await _shoppingCartService.UpdateCartAsync(cart);
                    await UpdateCartSessionAsync();
                }
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Minus(int cartID)
        {
            var cart = await _shoppingCartService.GetCartByIdAsync(cartID);

            if (cart != null)
            {
                cart.Count--;
                await _shoppingCartService.UpdateCartAsync(cart);
                await UpdateCartSessionAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Remove(int cartID)
        {
            var cart = await _shoppingCartService.GetCartByIdAsync(cartID);

            if (cart != null)
            {
                cart.Count = 0;
                await _shoppingCartService.UpdateCartAsync(cart);
                await UpdateCartSessionAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> UpdateCart(int cartID, int count)
        {
            var cart = await _shoppingCartService.GetCartByIdAsync(cartID);
            if(cart == null)
            {
                return NotFound();
            }

            if(count <= 1)
            {
                cart.Count = 0;
                
            }
            else
            {
                if(count >= 1000)
                {
                    cart.Count = 1000;
                }
                else
                {
                    cart.Count = count;
                }
            }
            
            await _shoppingCartService.UpdateCartAsync(cart);
            await UpdateCartSessionAsync();
            return Ok(new { success = true });
        }

        private async Task UpdateCartSessionAsync()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userID = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userID))
            {
                var count = await _shoppingCartService.GetCartCountAsync(userID);
                HttpContext.Session.SetInt32(SD.SessionCart, count);
            }
        }
    }
}
