namespace Tibr.Application.Dtos.Payment;

public class PaymentIntentionRequest
{
    public int AmountCents { get; set; }
    public string Currency { get; set; } = "EGP";
    public string SpecialReference { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
