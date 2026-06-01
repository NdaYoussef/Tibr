using System;
using System.Collections.Generic;
using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.Entities
{
    public class User : BaseEntity<long>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool OtpVerified { get; set; }
        public string KycStatus { get; set; } = string.Empty;
        public string? OtpCode { get; set; }
        public DateTime? OtpExpiry { get; set; }

        public virtual ICollection<KYCDocument> KYCDocuments { get; set; } = [];
        public virtual ICollection<Favorite> Favorites { get; set; } = [];
        public virtual ICollection<Cart> Carts { get; set; } = [];
        public virtual ICollection<Order> Orders { get; set; } = [];
        public virtual ICollection<Payment> Payments { get; set; } = [];
        public virtual ICollection<Notification> Notifications { get; set; } = [];
        public virtual ICollection<Support> SupportTickets { get; set; } = [];
    }
}
