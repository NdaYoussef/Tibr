using System.ComponentModel.DataAnnotations;
using static Tibr.Domain.Entities.Support;

namespace Tibr.MVC.Models.Support
{
    // SUPPORT LIST PAGE
    public class SupportListViewModel
    {
        public IEnumerable<SupportRowViewModel> Tickets { get; set; } = [];
        public IEnumerable<SupportRowViewModel> Filtered { get; set; } = [];

        // Filter state
        public string? StatusFilter { get; set; }
        public string? SearchKeyword { get; set; }

        // Quick stat counts
        public int TotalOpen { get; set; }
        public int TotalPending { get; set; }
        public int TotalResolved { get; set; }
        public int TotalClosed { get; set; }
        public int TotalAll { get; set; }
    }

    public class SupportRowViewModel
    {
        public long Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public long CustomerId { get; set; }

        // Status comes from SupportResponse.Status as string ("Open", "Pending" etc.)
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ReplyCount { get; set; }

        // UI helpers
        public string StatusClass => Status switch
        {
            "Open" => "status-open",
            "Pending" => "status-pending",
            "Resolved" => "status-resolved",
            "Closed" => "status-closed",
            _ => "status-open"
        };

        public string StatusLabel => Status switch
        {
            "Open" => "Open",
            "Pending" => "In Progress",
            "Resolved" => "Resolved",
            "Closed" => "Closed",
            _ => Status
        };

        public string StatusBadgeClass => Status switch
        {
            "Open" => "badge-info",
            "Pending" => "badge-warning",
            "Resolved" => "badge-success",
            "Closed" => "badge-secondary",
            _ => "badge-info"
        };

        public string TimeAgo => GetTimeAgo(CreatedAt);

        private static string GetTimeAgo(DateTime dt)
        {
            var diff = DateTime.UtcNow - dt;
            if (diff.TotalMinutes < 1) return "just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
            return dt.ToString("MMM dd, yyyy");
        }
    }

    // TICKET DETAIL PAGE

    public class SupportDetailViewModel
    {
        // Ticket header
        public long Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public long CustomerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;

        // Status helpers
        public string StatusLabel => Status switch
        {
            "Open" => "Open",
            "Pending" => "In Progress",
            "Resolved" => "Resolved",
            "Closed" => "Closed",
            _ => Status
        };

        public string StatusBadgeClass => Status switch
        {
            "Open" => "badge-info",
            "Pending" => "badge-warning",
            "Resolved" => "badge-success",
            "Closed" => "badge-secondary",
            _ => "badge-info"
        };

        public bool IsClosed => Status == "Closed";

        // Conversation thread — includes the original message + admin replies
        public List<MessageViewModel> Messages { get; set; } = [];

        // Reply form
        public ReplyViewModel Reply { get; set; } = new();

        // Status change form
        public string NewStatus { get; set; } = string.Empty;

        // Available statuses for the dropdown (maps to SupportStatus enum)
        public static IEnumerable<(string Value, string Label)> AvailableStatuses =>
        [
            ("Open",     "Open"),
            ("Pending",  "In Progress"),
            ("Resolved", "Resolved"),
            ("Closed",   "Closed")
        ];
    }

    public class MessageViewModel
    {
        public long Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public bool IsFromAdmin { get; set; }
        public DateTime SentAt { get; set; }

        public string TimeDisplay => SentAt.ToString("MMM dd, yyyy · HH:mm");
        public string AvatarLetter => string.IsNullOrEmpty(SenderName)
            ? "?"
            : SenderName[0].ToString().ToUpper();
    }

    // REPLY FORM (embedded in detail page)
    public class ReplyViewModel
    {
        public long SupportId { get; set; }

        [Required(ErrorMessage = "Reply message is required.")]
        [StringLength(2000, MinimumLength = 5, ErrorMessage = "Message must be between 5 and 2000 characters.")]
        [Display(Name = "Message")]
        public string Message { get; set; } = string.Empty;
    }

    // STATUS UPDATE FORM (embedded in detail page)
    public class UpdateStatusViewModel
    {
        public long SupportId { get; set; }
        public SupportStatus Status { get; set; }
    }
}