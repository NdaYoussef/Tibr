using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.SupportDtos;
using Tibr.Application.Dtos.TicketDtos;
using static Tibr.Domain.Entities.Support;

namespace Tibr.Application.Services.TicketServices
{
    public interface ITicketService
    {
        Task<Result<TicketDto>> ReplyToTicketAsync(CreateTicketDto dto, long adminId);

    
        Task<Result<string>> DeleteMessageAsync(long ticketId);
    }
}
