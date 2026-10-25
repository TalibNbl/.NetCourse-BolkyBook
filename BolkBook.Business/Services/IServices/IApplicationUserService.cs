using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBook.Business.Services.IServices
{
    public interface IApplicationUserService
    {
        Task<ApplicationUser?> GetUserByIDAsync(string userID);
        Task<IEnumerable<ApplicationUser>> GetAllUsersAsync();
    }
}
