using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Domain.Entities;
using Tibr.Domain.IRepositories;
using Tibr.Infrastructure.Contexts;

namespace Tibr.Infrastructure.Repositories
{
    public class ProductRepository : GenericRepository<Product , long> , IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        // Single product with Category included
        public async Task<Product?> GetProductByIdAsync(long id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        }

        // For listing — includes needed for mapping
        public IQueryable<Product> GetProductsQuery()
        {
            return _context.Products
                .Include(p => p.Category)
                .Where(p => !p.IsDeleted);
        }

        // For listing with popularity score sorting
        public IQueryable<Product> GetProductsWithPopularityQuery()
        {
            return _context.Products
                .Include(p => p.Category)
                .Include(p => p.Favorites)
                .Include(p => p.OrderItems)
                .Where(p => !p.IsDeleted);
        }

        // Executes the query with paging 
        public async Task<List<Product>> GetPagedProductsAsync(
            IQueryable<Product> query, int skip, int pageSize)
        {
            return await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }

        // Count 
        public async Task<int> CountAsync(IQueryable<Product> query)
        {
            return await query.CountAsync();
        }
    }
}
