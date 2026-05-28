using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.IRepositories
{
    public interface IGenericRepository<TEntity, TId> where TEntity : BaseEntity<TId>
    {
         IQueryable<TEntity> GetAll();
         Task<TEntity?> GetById(TId id);
         Task AddAsync(TEntity entity);
        Task UpdateAsync(TEntity entity);
        Task DeleteAsync(TEntity entity);
        Task<int> SaveChangesAsync();
    }
    }

