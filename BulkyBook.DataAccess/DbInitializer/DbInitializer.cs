using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBook.DataAccess.DbInitializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleMananger;
        private readonly ApplicationDBContext _db;


        public DbInitializer(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleMananger, ApplicationDBContext db)
        {
            _userManager = userManager;
            _roleMananger = roleMananger;
            _db = db;
        }

        public async Task InitialAsync()
        {
            try
            {
                if((await _db.Database.GetPendingMigrationsAsync()).Any())
                {
                    await _db.Database.MigrateAsync();
                }
            }
            catch (Exception ex) { 
            
            }

            if (!await _roleMananger.RoleExistsAsync(SD.RoleCustomer))
            {
                await _roleMananger.CreateAsync(new IdentityRole(SD.RoleCustomer));
                await _roleMananger.CreateAsync(new IdentityRole(SD.RoleAdmin));
                await _roleMananger.CreateAsync(new IdentityRole(SD.RoleEmployee));
            }

            ApplicationUser user = await _userManager.FindByEmailAsync("admin@dotnetmastery.com");
            if(user == null)
            {
                var result = await _userManager.CreateAsync(new ApplicationUser { 
                UserName = "admin@dotnetmastery.com",
                Email = "admin@dotnetmastery.com",
                EmailConfirmed = true,
                Name = "Bhrugen Patel",
                PhoneNumber = "11112222333",
                StreetAdrress = "teset 123 Ave",
                State = "IL",
                PostalCode = "23422",
                City = "Chicago"
                }, "Admin123*");

                if (result.Succeeded)
                {
                    user = await _userManager.FindByEmailAsync("admin@dotnetmastery.com");
                    await _userManager.AddToRoleAsync(user, SD.RoleAdmin);
                }
            }
        }
    }
}
