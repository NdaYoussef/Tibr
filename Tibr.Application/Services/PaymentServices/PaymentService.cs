using Microsoft.Extensions.Logging;
using Tibr.Application.Dtos.Payment;
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
        ILogger<PaymentService> logger
    )
    {
        _gateway = gateway;
        _orderRepo = orderRepo;
        _paymentRepo = paymentRepo;
        _depositService = depositService;
        _logger = logger;
    }

    public async Task<Result<string>> InitiateOrderPaymentAsync(
        long orderId,
        CreatePaymentRequest request
    )
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
            CreatedAt = DateTime.UtcNow,
        };

        await _paymentRepo.AddAsync(payment);
        await _paymentRepo.SaveChangesAsync();

        var timestamp = new DateTimeOffset(payment.CreatedAt).ToUnixTimeSeconds();
        var specialReference = $"payment:{payment.Id}:{orderId}:{timestamp}";

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

    public async Task<Result<VerifyStatusResponse>> VerifyPaymentAsync(long paymentId)
    {
        var payment = await _paymentRepo.GetByIdAsync(paymentId);
        if (payment is null)
            return Result<VerifyStatusResponse>.Failure("Payment not found.");

        if (payment.Status == "Completed")
        {
            return Result<VerifyStatusResponse>.Success(new VerifyStatusResponse
            {
                EntityId = payment.Id,
                EntityType = "payment",
                Status = "Completed",
                IsCompleted = true,
                InquiredPaymob = false,
                Message = "Payment is already completed.",
            });
        }

        if (payment.Status != "Pending")
        {
            return Result<VerifyStatusResponse>.Success(new VerifyStatusResponse
            {
                EntityId = payment.Id,
                EntityType = "payment",
                Status = payment.Status,
                IsCompleted = false,
                InquiredPaymob = false,
                Message = $"Payment is {payment.Status}.",
            });
        }

        var timestamp = new DateTimeOffset(payment.CreatedAt).ToUnixTimeSeconds();
        var merchantOrderId = $"payment:{payment.Id}:{payment.OrderId}:{timestamp}";

        var inquiry = await _gateway.InquireByMerchantOrderAsync(merchantOrderId);
        if (!inquiry.IsSuccess)
            return Result<VerifyStatusResponse>.Failure(inquiry.ErrorMessage!);

        if (inquiry.IsPaid)
        {
            payment.Status = "Completed";
            payment.PaidAt = DateTime.UtcNow;
            await _paymentRepo.UpdateAsync(payment);

            var order = await _orderRepo.GetByIdAsync(payment.OrderId);
            if (order is not null)
            {
                order.PaymentStatus = "Paid";
                order.OrderStatus = "Processing";
                await _orderRepo.UpdateAsync(order);
            }

            await _paymentRepo.SaveChangesAsync();

            return Result<VerifyStatusResponse>.Success(new VerifyStatusResponse
            {
                EntityId = payment.Id,
                EntityType = "payment",
                Status = "Completed",
                IsCompleted = true,
                InquiredPaymob = true,
                Message = "Payment confirmed via Paymob inquiry.",
            });
        }

        return Result<VerifyStatusResponse>.Success(new VerifyStatusResponse
        {
            EntityId = payment.Id,
            EntityType = "payment",
            Status = "Pending",
            IsCompleted = false,
            InquiredPaymob = true,
            Message = "Payment is still pending on Paymob's side.",
        });
    }

    public async Task HandlePaymentCallbackAsync(string rawBody)
    {
        _logger.LogInformation("[PaymentService] HandlePaymentCallbackAsync called");
        _logger.LogInformation("[PaymentService] Raw webhook body received: {Body}", rawBody);

        try
        {
            var data = _gateway.ExtractWebhookData(rawBody);
            _logger.LogInformation("[PaymentService] Webhook data extracted successfully");
            _logger.LogInformation("[PaymentService] Success: {Success}", data.Success);
            _logger.LogInformation(
                "[PaymentService] Special Reference: {SpecialReference}",
                data.SpecialReference
            );
            _logger.LogInformation(
                "[PaymentService] Payment Method: {PaymentMethod}",
                data.PaymentMethod
            );

            var parts = data.SpecialReference.Split(':');
            _logger.LogInformation(
                "[PaymentService] Special reference parts count: {Count}",
                parts.Length
            );

            if (parts.Length < 2)
            {
                _logger.LogWarning(
                    "[PaymentService] ❌ Invalid special_reference format: {Ref} (expected at least 2 parts)",
                    data.SpecialReference
                );
                return;
            }

            var entityType = parts[0];
            _logger.LogInformation("[PaymentService] Entity Type: {Type}", entityType);

            if (!long.TryParse(parts[1], out var entityId))
            {
                _logger.LogWarning(
                    "[PaymentService] ❌ Invalid entity ID in special_reference: {Ref} (cannot parse: {Value})",
                    data.SpecialReference,
                    parts[1]
                );
                return;
            }

            _logger.LogInformation("[PaymentService] Entity ID: {EntityId}", entityId);

            switch (entityType)
            {
                case "deposit":
                    _logger.LogInformation(
                        "[PaymentService] Processing DEPOSIT callback for ID: {DepositId}",
                        entityId
                    );
                    await _depositService.HandleCallbackAsync(entityId, data.Success);
                    _logger.LogInformation("[PaymentService] ✓ Deposit callback processed");
                    break;

                case "payment":
                    _logger.LogInformation(
                        "[PaymentService] Processing ORDER PAYMENT callback for PaymentId: {PaymentId}",
                        entityId
                    );
                    await HandleOrderCallbackAsync(entityId, data);
                    _logger.LogInformation("[PaymentService] ✓ Order payment callback processed");
                    break;

                default:
                    _logger.LogWarning(
                        "[PaymentService] ❌ Unknown entity type in special_reference: {Type}",
                        entityType
                    );
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[PaymentService] ❌ Error in HandlePaymentCallbackAsync: {Message}",
                ex.Message
            );
            _logger.LogError("[PaymentService] Stack Trace: {StackTrace}", ex.StackTrace);
            throw;
        }
    }

    private async Task HandleOrderCallbackAsync(
        long paymentId,
        Dtos.Payment.PaymentWebhookData data
    )
    {
        _logger.LogInformation(
            "[PaymentService.HandleOrderCallback] Starting order payment callback for PaymentId: {PaymentId}",
            paymentId
        );

        var payment = await _paymentRepo.GetByIdAsync(paymentId);
        if (payment is null)
        {
            _logger.LogWarning(
                "[PaymentService.HandleOrderCallback] ❌ Payment callback for non-existent PaymentId={PaymentId}",
                paymentId
            );
            return;
        }

        _logger.LogInformation(
            "[PaymentService.HandleOrderCallback] Payment found - Current Status: {Status}, OrderId: {OrderId}, UserId: {UserId}, Amount: {Amount}",
            payment.Status,
            payment.OrderId,
            payment.UserId,
            payment.Amount
        );

        if (payment.Status == "Completed")
        {
            _logger.LogInformation(
                "[PaymentService.HandleOrderCallback] ℹ Payment {PaymentId} already completed — skipping duplicate callback.",
                paymentId
            );
            return;
        }

        _logger.LogInformation(
            "[PaymentService.HandleOrderCallback] Webhook success status: {Success}",
            data.Success
        );
        _logger.LogInformation(
            "[PaymentService.HandleOrderCallback] Payment method: {Method}",
            data.PaymentMethod
        );

        if (data.Success)
        {
            _logger.LogInformation(
                "[PaymentService.HandleOrderCallback] ✓ Payment successful - Updating payment status to Completed"
            );
            payment.Status = "Completed";
            payment.PaidAt = DateTime.UtcNow;
            payment.PaymentMethod = data.PaymentMethod;
            await _paymentRepo.UpdateAsync(payment);
            _logger.LogInformation(
                "[PaymentService.HandleOrderCallback] Payment updated: PaidAt={PaidAt}, Method={Method}",
                payment.PaidAt,
                payment.PaymentMethod
            );

            var order = await _orderRepo.GetByIdAsync(payment.OrderId);
            if (order is not null)
            {
                _logger.LogInformation(
                    "[PaymentService.HandleOrderCallback] Order found - OrderId: {OrderId}, CurrentStatus: {Status}, PaymentStatus: {PaymentStatus}",
                    order.Id,
                    order.OrderStatus,
                    order.PaymentStatus
                );

                order.PaymentStatus = "Paid";
                order.OrderStatus = "Processing";
                _logger.LogInformation(
                    "[PaymentService.HandleOrderCallback] Updating order entity: OrderId={OrderId}, NewPaymentStatus={PaymentStatus}, NewOrderStatus={OrderStatus}",
                    order.Id,
                    order.PaymentStatus,
                    order.OrderStatus
                );
                await _orderRepo.UpdateAsync(order);
                _logger.LogInformation(
                    "[PaymentService.HandleOrderCallback] ✓ Order entity update queued for OrderId={OrderId}",
                    order.Id
                );

                _logger.LogInformation(
                    "[PaymentService.HandleOrderCallback] ✓ Order updated - New PaymentStatus: {PaymentStatus}, OrderStatus: {OrderStatus}",
                    order.PaymentStatus,
                    order.OrderStatus
                );
            }
            else
            {
                _logger.LogWarning(
                    "[PaymentService.HandleOrderCallback] ❌ Order with id {OrderId} not found during payment callback.",
                    payment.OrderId
                );
            }
        }
        else
        {
            _logger.LogWarning(
                "[PaymentService.HandleOrderCallback] ❌ Payment failed - Updating payment status to Failed"
            );
            payment.Status = "Failed";
            await _paymentRepo.UpdateAsync(payment);
            _logger.LogWarning(
                "[PaymentService.HandleOrderCallback] Payment marked as Failed for PaymentId: {PaymentId}",
                paymentId
            );
        }

        _logger.LogInformation(
            "[PaymentService.HandleOrderCallback] Saving changes to database..."
        );
        var saveResult = await _paymentRepo.SaveChangesAsync();
        _logger.LogInformation(
            "[PaymentService.HandleOrderCallback] Database save completed, rows affected: {RowsAffected}",
            saveResult
        );
        if (saveResult == 0)
        {
            _logger.LogWarning(
                "[PaymentService.HandleOrderCallback] ⚠ No database rows were modified by SaveChangesAsync. This may indicate no tracked changes were applied."
            );
        }
        _logger.LogInformation(
            "[PaymentService.HandleOrderCallback] ✓ Database changes saved successfully"
        );
        _logger.LogInformation("[PaymentService.HandleOrderCallback] === END ORDER CALLBACK ===");
    }
}
