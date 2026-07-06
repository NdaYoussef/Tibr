using System;
using System.Collections.Generic;
using System.Text;

namespace Tibr.Application.Dtos.NotificationsDtos
{
    public class NotificationDto
    {
        public long Id { get; set; }
        public long AdminId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public long? RelatedEntityId { get; set; }
        public string? ActionUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}