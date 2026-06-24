using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Tibr.Domain.Entities;

namespace Tibr.Domain.IRepositories
{
    public interface IAssetPriceRepository
    {
        IQueryable<AssetPrice> GetAll(
            Expression<Func<AssetPrice, bool>>? predicate = null);

        Task AddAsync(AssetPrice entity);

        void Update(AssetPrice entity);

        Task<int> SaveChangesAsync();
    }
}
