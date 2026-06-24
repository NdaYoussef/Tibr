namespace Tibr.Application.Dtos.Payment;

public class PaymentInitiationResult
{
    public string CheckoutUrl { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }

    public static PaymentInitiationResult Success(string checkoutUrl) =>
        new() { CheckoutUrl = checkoutUrl, IsSuccess = true };

    public static PaymentInitiationResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
