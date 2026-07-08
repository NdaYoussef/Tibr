using Microsoft.EntityFrameworkCore;
using Tibr.Application.InfrastructureContracts;
using Tibr.Domain.Entities;
using Tibr.Infrastructure.Contexts;

namespace Tibr.Infrastructure.Queries
{
    public class OrderQueryService : IOrderQueryService
    {
        private readonly ApplicationDbContext _context;

        public OrderQueryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Order?> GetByIdWithDetailsAsync(long id)
        {
            return await _context
                .Orders.Where(o => !o.IsDeleted && o.Id == id)
                .Include(o => o.User)
                .Include(o => o.Payments)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Order>> GetAllWithDetailsAsync()
        {
            return await _context
                .Orders.Where(o => !o.IsDeleted)
                .Include(o => o.User)
                .Include(o => o.Payments)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetByUserIdWithDetailsAsync(long userId)
        {
            return await _context
                .Orders.Where(o => !o.IsDeleted && o.UserId == userId)
                .Include(o => o.User)
                .Include(o => o.Payments)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .ToListAsync();
        }
    }
}
