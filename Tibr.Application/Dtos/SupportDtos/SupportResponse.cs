using System;
using System.Collections.Generic;
using System.Text;
using static Tibr.Domain.Entities.Support;

namespace Tibr.Application.Dtos.SupportDtos
{
    public class SupportResponse
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public SupportStatus Status { get; set; } 
    }
}
