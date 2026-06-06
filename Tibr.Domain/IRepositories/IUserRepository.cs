using System.Linq;
using Tibr.Domain.Entities;

namespace Tibr.Domain.IRepositories
{
    public interface IUserRepository : IGenericRepository<User, long>
    {
        IQueryable<User> GetAll();
    }
}
