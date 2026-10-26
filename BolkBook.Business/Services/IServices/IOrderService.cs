using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBook.Business.Services.IServices
{
    public interface IOrderService
    {
        Task<OrderHeader> CreateOrderAync(OrderHeader orderHeader);
        Task<OrderHeader?> GetOrderByIDAsync(int id, bool includeUser = false, bool includeDetails = false);
        Task<IEnumerable<OrderHeader>> GetAllOrderAsync(string? userID = null,string? status = null, bool includeUser = false, bool includeDetails = false);
        Task UpdateOrderAsync(OrderHeader orderHeader);
        Task UpdateOrderStatusAsync(int id, string orderStatus, string? carrier = null, string? trackingNumber = null);
        Task UpdateStripePaymentAsync(int orderID, string sessionID, string paymentIntentID);
        Task<bool> CancelOrderWithRefundAsync(int orderId);
        Task<string> CreateStripeCheckoutSessionAsync(OrderHeader orderHeader, IEnumerable<ShoppingCart> cartItems, string domain);
        Task<bool> VerifyStripePaymentAsync(OrderHeader orderHeader);
    }
}
