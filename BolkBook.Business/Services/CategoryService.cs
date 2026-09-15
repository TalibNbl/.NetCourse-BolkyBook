using BulkyBook.Business.Services.IServices;
using BulkyBook.DataAccess;
using BulkyBook.Models;
using Microsoft.EntityFrameworkCore;

namespace BulkyBook.Business.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDBContext _context;

        public CategoryService(ApplicationDBContext context)
        {
            _context = context;
        }
        
        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await _context.Catogeries.ToListAsync();
        }
        
        public async Task<Category?> GetCategoryByIDAsync(int id)
        {
            return await _context.Catogeries.FindAsync(id);
        }

        public async Task<Category> CreateCategoryAsync(Category category)
        {
            _context.Catogeries.Add(category);
           await _context.SaveChangesAsync();
            return category;
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = _context.Catogeries.Find(id);

            if (category == null) throw new KeyNotFoundException($"Category {id} not found");

            _context.Catogeries.Remove(category);
           await _context.SaveChangesAsync();
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            _context.Catogeries.Update(category);
          await  _context.SaveChangesAsync();
        }

        public async Task<bool> IsCategoryNameUniqueAsync(string name, int? categoryID = null)
        {
            if (categoryID.HasValue)
            {
             return !await _context.Catogeries.AnyAsync(c => c.Name.ToLower() == name.ToLower()
                && c.CategoryID != categoryID.Value);
            }
            else
            {
                return !await _context.Catogeries.AnyAsync(c => c.Name.ToLower() == name.ToLower());
            }
        }
    }
}
