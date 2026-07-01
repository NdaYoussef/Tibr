using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Domain.Entities;

namespace Tibr.Application.Dtos.NotificationsDtos
{
    public class CreateNotificationRequest
    {
        public long? AdminId { get; set; } 
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; } = NotificationType.General;
        public long? RelatedEntityId { get; set; }
        public string? ActionUrl { get; set; }
    }
}
