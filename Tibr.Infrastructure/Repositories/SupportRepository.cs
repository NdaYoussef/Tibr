using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Domain.Entities;
using Tibr.Domain.IRepositories;
using Tibr.Infrastructure.Contexts;

namespace Tibr.Infrastructure.Repositories
{
    public class SupportRepository : GenericRepository<Support, long> ,ISupportRepository
    {
        private readonly ApplicationDbContext _context;
        public SupportRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Support?> GetSupportWithTicketsAsync(long id)
        {
            return await _context.Supports
        .Include(s => s.Tickets)         
        .Include(s => s.User)            
        .FirstOrDefaultAsync(s => s.Id == id);
        }
    }
}
