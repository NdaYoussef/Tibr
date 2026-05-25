using System.Linq.Expressions;
using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.IRepositories
{
    public interface IGenericRepository<TEntity>
        where TEntity : BaseEntity<long>
    {
        Task<TEntity?> GetByIdAsync(long id);
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
        Task<TEntity> AddAsync(TEntity entity);
        Task UpdateAsync(TEntity entity);
        Task DeleteAsync(TEntity entity);
        Task<bool> ExistsAsync(long id);
        Task<int> SaveChangesAsync();
    }
}
