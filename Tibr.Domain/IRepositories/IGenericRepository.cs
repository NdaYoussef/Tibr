using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.IRepositories
{
    public interface IGenericRepository<TEntity,TId> where TEntity : BaseEntity<TId>

    {
        public Task<IQueryable<TEntity>> GetAll();
        public Task AddAsync(TEntity entity);
        public Task UpdateAsync(TEntity entity);
        public Task DeleteAsync(TEntity entity);
        public Task<int> SaveChangesAsync();
    }
}
