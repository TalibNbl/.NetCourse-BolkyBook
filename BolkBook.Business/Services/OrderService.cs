using BulkyBook.Business.Services.IServices;
using BulkyBook.DataAccess;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Stripe.Climate;

namespace BulkyBook.Business.Services
{
    public class OrderService : IOrderService
    {
        ApplicationDBContext _db;

        public OrderService(ApplicationDBContext db)
        {
            _db = db;
        }

        public async Task<bool> CancelOrderWithRefundAsync(int orderId)
        {
            var order = await _db.OrderHeaders.FindAsync(orderId);

            if(order == null)
            {
                throw new KeyNotFoundException($"Order {orderId} not found");
            }

            if(order.OrderStatus == SD.StatusShipped)
            {
                throw new InvalidOperationException("Cannot cancel orders that have already benn shipped. Customer must initiate a return instead of cancel");
            }

            if(order.OrderStatus == SD.StatusCancelled || order.OrderStatus == SD.StatusRefunded)
            {
                throw new InvalidOperationException("This order has already been cancelled");
            }

            bool refundIssued = false;

            if(!string.IsNullOrEmpty(order.PaymentIntentID) && (order.OrderStatus == SD.StatusApproved ||
                order.OrderStatus == SD.StatusInProcess))
            {
                try
                {
                    var options = new RefundCreateOptions { PaymentIntent = order.PaymentIntentID, Reason = RefundReasons.RequestedByCustomer};
                    var service = new RefundService();
                    Refund refund = service.Create(options);

                    if (refund.Status == "succeeded" || refund.Status == "pending")
                    {
                        refundIssued = true;
                        order.OrderStatus = SD.StatusRefunded;
                    }
                }
                catch(StripeException ex)
                {
                    order.OrderStatus = SD.StatusCancelled;
                    await _db.SaveChangesAsync();
                    throw new InvalidOperationException($"Stripe refund failed: {ex.Message}. Order has been cancelled, but refund must be processed.");
                }
            }
            else
            {
                order.OrderStatus = SD.StatusCancelled;
            }

            await _db.SaveChangesAsync();
            return refundIssued;
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

        public async Task<string> CreateStripeCheckoutSessionAsync(OrderHeader orderHeader, IEnumerable<ShoppingCart> cartItems, string domain)
        {
            if(orderHeader == null)
            {
                throw new ArgumentNullException(nameof(orderHeader));
            }

            if(cartItems == null || !cartItems.Any())
            {
                throw new ArgumentException("Cart items cannot be empty", nameof(cartItems));
            }

            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={orderHeader.Id}",
                CancelUrl = domain + "customer/car/index",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                Metadata = new Dictionary<string, string>
                    {
                        {"OrderID", orderHeader.Id.ToString()}
                    }
            };

            foreach (var items in cartItems)
            {
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(items.Price * 100),
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = items.Product.Title
                            }
                        },
                        Quantity = items.Count,
                    };
                    options.LineItems.Add(sessionLineItem);
                }

            }

            var service = new SessionService();
            Session session = service.Create(options);
            await UpdateStripePaymentAsync(orderHeader.Id, session.Id, session.PaymentIntentId);
            return session.Url;
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

        public async Task UpdateStripePaymentAsync(int orderID, string sessionID, string paymentIntentID)
        {
            var order = await _db.OrderHeaders.FindAsync(orderID);
            if (order == null)
            {
                throw new KeyNotFoundException($"Order {orderID} not found");
            }
            if (!string.IsNullOrEmpty(sessionID))
            {
                order.PaymentIntentID = paymentIntentID;
            }

            await _db.SaveChangesAsync();
        }

        public async Task<bool> VerifyStripePaymentAsync(OrderHeader orderHeader)
        {
            var service = new SessionService();
            Session session = service.Get(orderHeader.SessionID);
            if (session.PaymentStatus.ToLower() == "paid")
            {
                await UpdateStripePaymentAsync(orderHeader.Id, session.Id, session.PaymentIntentId);
                await UpdateOrderStatusAsync(orderHeader.Id, SD.StatusApproved);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
