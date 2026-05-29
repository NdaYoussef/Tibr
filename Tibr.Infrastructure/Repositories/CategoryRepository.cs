using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Domain.Entities;
using Tibr.Domain.IRepositories;
using Tibr.Infrastructure.Contexts;

namespace Tibr.Infrastructure.Repositories
{
    public class CategoryRepository : GenericRepository<Category, long>, ICategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public CategoryRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> GetByNameAsync(string name)
        {
            return await _context.Categories
                .AnyAsync(c => c.Name.ToLower() == name.ToLower()
                            && !c.IsDeleted);
        }
    }

}
