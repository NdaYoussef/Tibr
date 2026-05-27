using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Domain.Entities;

namespace Tibr.Domain.IRepositories
{
    public interface IProductRepository : IGenericRepository<Product,long>
    {
        // Fetch single product with specific includes
        Task<Product?> GetProductByIdAsync(long id);

        // Fetch all products with includes for listing
        Task<List<Product>> GetPagedProductsAsync(IQueryable<Product> query,
            int skip, int pageSize);

        // Count without loading data
        Task<int> CountAsync(IQueryable<Product> query);

        // GetAll with includes ready for filtering
        IQueryable<Product> GetProductsQuery();

        // GetAll with includes + popularity for sorting
        IQueryable<Product> GetProductsWithPopularityQuery();
    }
}
