using BulkyBook.Business.Services.IServices;
using BulkyBook.DataAccess;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.EntityFrameworkCore;

namespace BulkyBook.Business.Services
{
    public class OrderService : IOrderService
    {
        ApplicationDBContext _db;

        public OrderService(ApplicationDBContext db)
        {
            _db = db;
        }

        public async Task<OrderHeader> CreateOrderAync(OrderHeader orderHeader)
        {
            _db.OrderHeaders.Add(orderHeader);

            if (orderHeader.OrderDetails != null)
            {
                _db.OrderDetails.AddRange(orderHeader.OrderDetails);
            }

            await _db.SaveChangesAsync();

            return orderHeader;
        }

        public async Task<IEnumerable <OrderHeader>> GetAllOrderAsync(string? userID = null, string? status = null, bool includeUser = false, bool includeDetails = false)
        {
            var query = _db.OrderHeaders.AsQueryable();

            if (includeUser)
            {
                query = query.Include(u => u.ApplicationUser);
            }
            if (includeDetails)
            {
                query = query.Include(u => u.OrderDetails).ThenInclude(u => u.Product);
            }
            if (!string.IsNullOrEmpty(userID))
            {
                query = query.Where(u => u.ApplicationUserID == userID);
            }
            if (!string.IsNullOrEmpty(status) && status.ToLower() != "all")
            {
                query = query.Where(u => u.OrderStatus.ToLower() == status.ToLower());
            }

            return await query.ToListAsync();
        }

        public async Task<OrderHeader?> GetOrderByIDAsync(int id, bool includeUser = false, bool includeDetails = false)
        {
            var query = _db.OrderHeaders.AsQueryable();

            if (includeUser)
            {
                query = query.Include(u => u.ApplicationUser);
            }
            if (includeDetails)
            {
                query = query.Include(u => u.OrderDetails).ThenInclude(u=>u.Product);
            }

            return await query.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task UpdateOrderAsync(OrderHeader orderHeader)
        {
            _db.OrderHeaders.Update(orderHeader);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateOrderStatusAsync(int id, string orderStatus, string? carrier = null, string? trackingNumber = null)
        {
            var order = await _db.OrderHeaders.FindAsync(id);
            if(order == null)
            {
                throw new KeyNotFoundException($"Order {id} not found");
            }
            order.OrderStatus = orderStatus;

            if (orderStatus == SD.StatusShipped)
            {
                order.ShippingDate = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(carrier))
                {
                    order.Carrier = carrier;
                }
                if (!string.IsNullOrEmpty(trackingNumber))
                {
                    order.TrackingNumber = trackingNumber;
                }
            }
                await _db.SaveChangesAsync();
        }
    }
}
