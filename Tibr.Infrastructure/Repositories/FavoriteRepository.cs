using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Domain.Entities;
using Tibr.Domain.IRepositories;
using Tibr.Infrastructure.Contexts;

namespace Tibr.Infrastructure.Repositories
{
    public class FavoriteRepository : GenericRepository<Favorite, long>, IFavoriteRepository
    {
        private readonly ApplicationDbContext _context;
        public FavoriteRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Favorite?> GetByUniqueKeysAsync(long userId, long productId)
        {
            return await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);
        }

        public async Task<IEnumerable<Favorite>> GetUserFavoritesWithProductsAsync(long userId)
        {
            return await _context.Favorites
                .Include(f => f.Product) 
                .Where(f => f.UserId == userId)
                .ToListAsync();
        }
    }
}
