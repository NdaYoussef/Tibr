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
        public IQueryable<Category> GetAll()
        {
           return _context.Categories.Where(e => !e.IsDeleted).AsNoTracking();
        }
        public async Task<bool> GetByNameAsync(string nameAr, string nameEn)
        {
            return await _context.Categories
                .AnyAsync(c => !c.IsDeleted &&
                    (c.NameAr.ToLower() == nameAr.ToLower() ||
                     c.NameEn.ToLower() == nameEn.ToLower()));
        }
    }

}
