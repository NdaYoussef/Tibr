using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Domain.Entities;

namespace Tibr.Domain.IRepositories
{
    public interface ISupportRepository : IGenericRepository<Support,long>
    {
        Task<Support?> GetSupportWithTicketsAsync(long id);
        
    }
}
