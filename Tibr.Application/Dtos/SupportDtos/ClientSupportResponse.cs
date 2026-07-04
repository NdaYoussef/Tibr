using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.TicketDtos;
using Tibr.Domain.Entities;

namespace Tibr.Application.Dtos.SupportDtos
{
    public class ClientSupportResponse
    {
        public long Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public Support.SupportStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public string ClientName { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public string ClientPhone { get; set; } = string.Empty;

        public List<TicketDto> Tickets { get; set; } = new();
    }
}
