using Tibr.Application.Dtos.Payment;

namespace Tibr.Application.Services.PaymentServices;

public interface IPaymentGateway
{
    Task<PaymentInitiationResult> CreateIntentionAsync(PaymentIntentionRequest request);
    bool VerifyWebhook(string rawBody, string signature);
    PaymentWebhookData ExtractWebhookData(string rawBody);
    Task<PaymentInquiryResult> InquireByMerchantOrderAsync(string merchantOrderId);
}
