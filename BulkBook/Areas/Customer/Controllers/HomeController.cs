using BulkyBook.Business.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBook.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly IProductService _productService;

        public HomeController(IProductService productService)
        {
            _productService = productService;
        }

        public async Task<IActionResult> Index()
        {
            var product = await _productService.GetAllProductsAsync(includeCategory:true);
            return View(product);
        }

        public async Task<IActionResult> Details(int productID)
        {
            var products = await _productService.GetProductByIDAsync(productID, includeCategory:true);
            return View(products);
        }

        public IActionResult Privacy()
        {
            return View();
        }

    }
}
