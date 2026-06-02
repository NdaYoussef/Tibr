using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Domain.Entities;

namespace Tibr.Domain.IRepositories
{
    public interface ITicketRepository : IGenericRepository<Ticket,long>
    {
    }
}
