using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Domain.Entities;

namespace Tibr.Domain.IRepositories
{
    public interface ICategoryRepository : IGenericRepository<Category, long>
    {
        IQueryable<Category> GetAll();
        Task<bool>GetByNameAsync(string name);
    }
}
