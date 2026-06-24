namespace Tibr.Application.Dtos.Payment;

public class PaymentWebhookData
{
    public string TransactionId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int AmountCents { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string SpecialReference { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
}
