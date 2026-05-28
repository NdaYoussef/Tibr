using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Domain.Entities;
using Tibr.Infrastructure.Contexts;

namespace Tibr.Infrastructure.Repositories
{
    public class SupportRepository : GenericRepository<Support, long>
    {
        public SupportRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
