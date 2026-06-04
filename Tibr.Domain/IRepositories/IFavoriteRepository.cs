using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Domain.Entities;

namespace Tibr.Domain.IRepositories
{
    public interface IFavoriteRepository : IGenericRepository<Favorite, long>
    {
        Task<Favorite?> GetByUniqueKeysAsync(long userId, long productId);
        Task<IEnumerable<Favorite>> GetUserFavoritesWithProductsAsync(long userId);
    }
}
