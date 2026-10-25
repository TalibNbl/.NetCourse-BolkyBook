using BulkyBook.Business.Services.IServices;
using BulkyBook.DataAccess;
using BulkyBook.Models;
using Microsoft.EntityFrameworkCore;

namespace BulkyBook.Business.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDBContext _context;

        public ProductService(ApplicationDBContext context)
        {
            _context = context;
        }
        
        public async Task<IEnumerable<Product>> GetAllProductsAsync(bool includeCategory=false)
        {
            if (includeCategory)
            {
                return await _context.Products.Include(u=>u.Category).ToListAsync();
            }
            else
            {
                return await _context.Products.ToListAsync();
            }
        }
        
        public async Task<Product?> GetProductByIDAsync(int id, bool includeCategory=false)
        {
            if (includeCategory)
            {
                return await _context.Products.Include(u => u.Category).FirstOrDefaultAsync(u=>u.ProductID==id);
            }
            else
            {
                return await _context.Products.FirstOrDefaultAsync(u => u.ProductID == id);
            }
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            _context.Products.Add(product);
           await _context.SaveChangesAsync();
            return product;
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = _context.Products.Find(id);

            if (product == null) throw new KeyNotFoundException($"Product {id} not found");

            _context.Products.Remove(product);
           await _context.SaveChangesAsync();
        }

        public async Task UpdateProductAsync(Product product)
        {
            _context.Products.Update(product);
          await  _context.SaveChangesAsync();
        }
    }
}
