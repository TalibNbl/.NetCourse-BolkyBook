using BulkyBook.DataAccess;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BulkyBook.Areas.Admin.Controllers
{

    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDBContext _db;

        public DashboardController(ApplicationDBContext db)
        {
            _db = db;
        }


        public async Task<IActionResult> Index()
        {
            var orders = await _db.OrderDetails.ToListAsync();
            var productCount = await _db.Products.CountAsync();
            var usersCount = await _db.ApplicationUsers.CountAsync();

            DashboardVM dashboardVM = new()
            {
                TotalOrders = orders.Count,
                TotalProducts = productCount,
                TotalUsers = usersCount,
                TotalRevenue = orders.Where(o => o.OrderStatus == SD.StatusApproved || o.OrderStatus == SD.StatusShipped)
                .Sum(o => o.OrderTotal)
            };

            return View(dashboardVM);
        }
    }
}
