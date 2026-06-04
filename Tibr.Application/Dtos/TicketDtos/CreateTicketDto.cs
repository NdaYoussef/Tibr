using System;
using System.Collections.Generic;
using System.Text;

namespace Tibr.Application.Dtos.TicketDtos
{
    public class CreateTicketDto
    {
        public long SupportId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
