using System;
using System.Collections.Generic;
using System.Text;

namespace Tibr.Application.Dtos.TicketDtos
{
    public class TicketDto
    {
        public long Id { get; set; }
        public long SupportId { get; set; }
        public long? AdminId { get; set; }
        public string? AdminName { get; set; } 
        public bool IsFromAdmin => AdminId.HasValue; 
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
