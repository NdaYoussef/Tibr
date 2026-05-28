using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Domain.Entities;
using Tibr.Domain.IRepositories;
using Tibr.Infrastructure.Contexts;

namespace Tibr.Infrastructure.Repositories
{
    public class ProductRepository : GenericRepository<Product , long> , IProductRepository
    {

        public ProductRepository(ApplicationDbContext context) : base(context)
        {
        }

    }
}
