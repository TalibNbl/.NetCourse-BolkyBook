using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBook.Business.Services.IServices
{
    public interface IShoppingCartService
    {
        Task<ShoppingCart?> GetCartByIdAsync(int cartID);
        Task<IEnumerable<ShoppingCart>> GetUserCartItemsAsync(string userID);
        Task<int> GetCartCountAsync(string userID);
        Task<ShoppingCart> AddToCartAsync(ShoppingCart cart);
        Task UpdateCartAsync(ShoppingCart cart);
        Task ClearCartAsync(string userID);
    }
}