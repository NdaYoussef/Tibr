using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
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
        {
            var adminEmails = _context.Admins.Select(a => a.Email);
            return _context.Users.Where(e => !e.IsDeleted && !adminEmails.Contains(e.Email)).AsNoTracking();
        }

        public override async Task<User?> GetById(long id)
        {
            var user = await base.GetById(id);
            if (user == null || user.IsDeleted) return null;
            var isAdmin = await _context.Admins.AnyAsync(a => a.Email == user.Email);
            if (isAdmin) return null;
            return user;
        }

        public override async Task<User?> GetByIdAsync(long id)
        {
            var user = await base.GetByIdAsync(id);
            if (user == null || user.IsDeleted) return null;
            var isAdmin = await _context.Admins.AnyAsync(a => a.Email == user.Email);
            if (isAdmin) return null;
            return user;
        }
    }
}
