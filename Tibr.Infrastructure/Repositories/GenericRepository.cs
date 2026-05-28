using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using Tibr.Domain.Common.Classes;
using Tibr.Domain.IRepositories;
using Tibr.Infrastructure.Contexts;

namespace Tibr.Infrastructure.Repositories
{
    public class GenericRepository<TEntity,TId> : IGenericRepository<TEntity,TId> where TEntity : BaseEntity<TId>

    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<TEntity> _dbSet;
        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<TEntity>();
        }
        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await _context.Set<TEntity>().AsNoTracking().Where(t => !t.IsDeleted).ToListAsync();
        }
        public async Task<TEntity?> GetById(TId id)
        {
            return await _dbSet.FindAsync(id);
        }
        public async Task AddAsync(TEntity entity)
        {
            await _dbSet.AddAsync(entity);
        }
        public async Task UpdateAsync(TEntity entity)
        {
            _dbSet.Update(entity);
        }
        public async Task DeleteAsync(TEntity entity)
        {
            entity.IsDeleted = true;
            _dbSet.Update(entity);
        }
        public async Task<int> SaveChangesAsync()
        {
           return await _context.SaveChangesAsync();
        }

        public IQueryable<TEntity> GetAll(Expression<Func<TEntity, bool>> predicate)
        
            => _dbSet.Where(predicate).AsNoTracking();
    }
    }

