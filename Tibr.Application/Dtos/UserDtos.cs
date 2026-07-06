using System;
using System.Collections.Generic;

namespace Tibr.Application.Dtos
{
    public class UserListItemDto
    {
        public long Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool OtpVerified { get; set; }
        public string KycStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class UserOrderHistoryDto
    {
        public long OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class UserDetailsDto
    {
        public long Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool OtpVerified { get; set; }
        public string KycStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? KycDocumentFront { get; set; }
        public string? KycDocumentBack { get; set; }
        public string? KycSelfieImage { get; set; }
        public List<UserOrderHistoryDto> Orders { get; set; } = [];
    }

    public class UpdateUserDto
    {
        public long Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string KycStatus { get; set; } = string.Empty;
    }

    public class UpdateUserKycStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}