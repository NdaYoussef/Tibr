using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Tibr.Domain.Entities;
using Tibr.Domain.IRepositories;
using Tibr.Infrastructure.Contexts;

namespace Tibr.Infrastructure.Repositories
{
    public class AssetPriceRepository : IAssetPriceRepository
    {
        private readonly ApplicationDbContext _context;

        public AssetPriceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<AssetPrice> GetAll(
            Expression<Func<AssetPrice, bool>>? predicate = null)
        {
            IQueryable<AssetPrice> query = _context.AssetPrices;

            if (predicate != null)
                query = query.Where(predicate);

            return query;
        }

        public async Task AddAsync(AssetPrice entity)
        {
            await _context.AssetPrices.AddAsync(entity);
        }

        public void Update(AssetPrice entity)
        {
            _context.AssetPrices.Update(entity);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
