using Microsoft.EntityFrameworkCore;
using System.Linq;
using Tibr.Domain.Entities;
using Tibr.Domain.IRepositories;
using Tibr.Infrastructure.Contexts;

namespace Tibr.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<User, long>, IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public IQueryable<User> GetAll()
            => _context.Users.Where(e => !e.IsDeleted).AsNoTracking();
    }
}
