using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.IRepositories
{
    public interface IGenericRepository<TEntity, TId>
        where TEntity : BaseEntity<TId>
    {
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task<TEntity?> GetByIdAsync(TId id);
        Task<TEntity?> GetById(TId id);
        Task AddAsync(TEntity entity);
        Task UpdateAsync(TEntity entity);
        Task DeleteAsync(TEntity entity);
        Task UpdateIsDeleteAsync(TEntity entity);
        Task<int> SaveChangesAsync();
        IQueryable<TEntity> GetAll(Expression<Func<TEntity, bool>> predicate);
    }
}

