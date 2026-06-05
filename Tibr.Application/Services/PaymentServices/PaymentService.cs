using Microsoft.Extensions.Logging;
using Tibr.Application.Dtos.Paymob;
using Tibr.Application.Services.DepositServices;
using Tibr.Domain.Entities;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.PaymentServices;

public class PaymentService
{
    private readonly IPaymentGateway _gateway;
    private readonly IGenericRepository<Order, long> _orderRepo;
    private readonly IGenericRepository<Payment, long> _paymentRepo;
    private readonly IDepositService _depositService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentGateway gateway,
        IGenericRepository<Order, long> orderRepo,
        IGenericRepository<Payment, long> paymentRepo,
        IDepositService depositService,
        ILogger<PaymentService> logger)
    {
        _gateway = gateway;
        _orderRepo = orderRepo;
        _paymentRepo = paymentRepo;
        _depositService = depositService;
        _logger = logger;
    }

    public async Task<Result<string>> InitiateOrderPaymentAsync(long orderId, CreatePaymentRequest request)
    {
        var order = await _orderRepo.GetByIdAsync(orderId);
        if (order is null)
            return Result<string>.Failure("Order not found.");

        var existingPayments = _paymentRepo.GetAll(p => p.OrderId == orderId).ToList();

        if (existingPayments.Any(p => p.Status == "Completed"))
            return Result<string>.Failure("This order has already been paid.");

        if (existingPayments.Any(p => p.Status == "Pending"))
            return Result<string>.Failure("A payment for this order is already in progress.");

        var payment = new Payment
        {
            OrderId = orderId,
            UserId = order.UserId,
            Amount = request.AmountCents / 100m,
            Status = "Pending",
        };

        await _paymentRepo.AddAsync(payment);
        await _paymentRepo.SaveChangesAsync();

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var specialReference = $"payment:{payment.Id}:{timestamp}";

        var intentionRequest = new Dtos.Payment.PaymentIntentionRequest
        {
            AmountCents = request.AmountCents,
            Currency = request.Currency,
            SpecialReference = specialReference,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
        };

        var result = await _gateway.CreateIntentionAsync(intentionRequest);

        if (!result.IsSuccess)
        {
            payment.Status = "Failed";
            await _paymentRepo.UpdateAsync(payment);
            await _paymentRepo.SaveChangesAsync();
            return Result<string>.Failure(result.ErrorMessage ?? "Payment initiation failed.");
        }

        return Result<string>.Success(result.CheckoutUrl);
    }

    public async Task HandlePaymentCallbackAsync(string rawBody)
    {
        var data = _gateway.ExtractWebhookData(rawBody);

        var parts = data.SpecialReference.Split(':');
        if (parts.Length < 2)
        {
            _logger.LogWarning("Invalid special_reference format: {Ref}", data.SpecialReference);
            return;
        }

        var entityType = parts[0];
        if (!long.TryParse(parts[1], out var entityId))
        {
            _logger.LogWarning("Invalid entity ID in special_reference: {Ref}", data.SpecialReference);
            return;
        }

        switch (entityType)
        {
            case "deposit":
                await _depositService.HandleCallbackAsync(entityId, data.Success);
                break;

            case "payment":
                await HandleOrderCallbackAsync(entityId, data);
                break;

            default:
                _logger.LogWarning("Unknown entity type in special_reference: {Type}", entityType);
                break;
        }
    }

    private async Task HandleOrderCallbackAsync(long paymentId, Dtos.Payment.PaymentWebhookData data)
    {
        var payment = await _paymentRepo.GetByIdAsync(paymentId);
        if (payment is null)
        {
            _logger.LogWarning("Payment callback for non-existent PaymentId={PaymentId}", paymentId);
            return;
        }

        if (payment.Status == "Completed")
        {
            _logger.LogInformation("Payment {PaymentId} already completed — skipping.", paymentId);
            return;
        }

        if (data.Success)
        {
            payment.Status = "Completed";
            payment.PaidAt = DateTime.UtcNow;
            payment.PaymentMethod = data.PaymentMethod;
            await _paymentRepo.UpdateAsync(payment);

            var order = await _orderRepo.GetByIdAsync(payment.OrderId);
            if (order is not null)
            {
                order.PaymentStatus = "Paid";
                order.OrderStatus = "Processing";
                await _orderRepo.UpdateAsync(order);
            }
        }
        else
        {
            payment.Status = "Failed";
            await _paymentRepo.UpdateAsync(payment);
        }

        await _paymentRepo.SaveChangesAsync();
    }
}
