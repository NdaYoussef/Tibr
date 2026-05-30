using System;
using System.Collections.Generic;
using System.Text;

namespace Tibr.Application.Dtos.SupportDtos
{
    public class SupportResponse
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
