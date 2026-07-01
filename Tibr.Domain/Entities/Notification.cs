using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.Entities
{
    public class Notification : BaseEntity<long>
    {
        public long? UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }

        public virtual User? User { get; set; } = null!;
        
        //admin notify
        public virtual Admin? Admin { get; set; } = null!;
        public long? AdminId { get; set; }
        public NotificationType Type { get; set; } = NotificationType.General;
        public long? RelatedEntityId { get; set; }
        public string? ActionUrl { get; set; }


    }

    public enum NotificationType
    {
        NewSupportTicket,
        TicketStatusChanged,
        NewOrder,
        LowStock,
        General
    }
}
