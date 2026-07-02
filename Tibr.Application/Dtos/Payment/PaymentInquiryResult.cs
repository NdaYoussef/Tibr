namespace Tibr.Application.Dtos.Payment;

public class PaymentInquiryResult
{
    public bool IsSuccess { get; set; }
    public bool IsPaid { get; set; }
    public string? ErrorMessage { get; set; }

    public static PaymentInquiryResult Paid() =>
        new() { IsSuccess = true, IsPaid = true };

    public static PaymentInquiryResult NotPaid() =>
        new() { IsSuccess = true, IsPaid = false };

    public static PaymentInquiryResult Failure(string error) =>
        new() { IsSuccess = false, IsPaid = false, ErrorMessage = error };
}

public class VerifyStatusResponse
{
    public long EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public bool InquiredPaymob { get; set; }
    public string? Message { get; set; }
}
