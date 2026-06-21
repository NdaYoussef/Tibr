using Microsoft.Extensions.Logging;
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;
using Tibr.Domain.Enums;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.DeliveryServices
{
    public class DeliveryService : IDeliveryService
    {
        private readonly IGenericRepository<DeliveryRequest, long> _deliveryRepo;
        private readonly IGenericRepository<Wallet, long> _walletRepo;
        private readonly IGenericRepository<WalletTransaction, long> _walletTransactionRepo;
        private readonly IGenericRepository<Address, long> _addressRepo;
        private readonly ILogger<DeliveryService> _logger;

        public DeliveryService(
            IGenericRepository<DeliveryRequest, long> deliveryRepo,
            IGenericRepository<Wallet, long> walletRepo,
            IGenericRepository<WalletTransaction, long> walletTransactionRepo,
            IGenericRepository<Address, long> addressRepo,
            ILogger<DeliveryService> logger)
        {
            _deliveryRepo = deliveryRepo;
            _walletRepo = walletRepo;
            _walletTransactionRepo = walletTransactionRepo;
            _addressRepo = addressRepo;
            _logger = logger;
        }

        public async Task<Result<DeliveryDto>> CreateRequestAsync(long userId, CreateDeliveryRequestDto dto)
        {
            var address = await _addressRepo.GetByIdAsync(dto.AddressId);
            if (address is null || address.UserId != userId)
                return Result<DeliveryDto>.Failure("Address not found.");

            var metalType = dto.AssetType == AssetType.Gold ? WalletType.Gold : WalletType.Silver;
            var wallet = _walletRepo.GetAll(w => w.UserId == userId && w.WalletType == metalType).FirstOrDefault();
            if (wallet is null)
                return Result<DeliveryDto>.Failure($"{metalType} wallet not found.");

            var available = wallet.Balance - wallet.ReservedBalance;
            if (available < dto.Quantity)
                return Result<DeliveryDto>.Failure(
                    $"Insufficient {dto.AssetType} balance. Available: {available:F4}, Required: {dto.Quantity:F4}.");

            wallet.ReservedBalance += dto.Quantity;
            await _walletRepo.UpdateAsync(wallet);

            var delivery = new DeliveryRequest
            {
                UserId = userId,
                AssetType = dto.AssetType,
                Quantity = dto.Quantity,
                AddressId = dto.AddressId,
                Status = DeliveryStatus.Pending,
                CreatedAt = DateTime.UtcNow,
            };
            await _deliveryRepo.AddAsync(delivery);

            await _walletTransactionRepo.AddAsync(new WalletTransaction
            {
                WalletId = wallet.Id,
                Type = WalletTransactionType.Reserve,
                Amount = dto.Quantity,
                ReferenceType = ReferenceType.Delivery,
                ReferenceId = delivery.Id,
                CreatedAt = DateTime.UtcNow,
            });

            await _deliveryRepo.SaveChangesAsync();

            var dtoResult = new DeliveryDto
            {
                Id = delivery.Id,
                AssetType = delivery.AssetType,
                Quantity = delivery.Quantity,
                Status = delivery.Status,
                CreatedAt = delivery.CreatedAt
            };

            return Result<DeliveryDto>.Success(dtoResult);
        }

        public async Task<Result> ConfirmDispatchAsync(long deliveryId, string trackingNumber)
        {
            var delivery = await _deliveryRepo.GetByIdAsync(deliveryId);
            if (delivery is null)
                return Result.Failure("Delivery request not found.");

            if (delivery.Status != DeliveryStatus.Pending)
                return Result.Failure("Only pending delivery requests can be dispatched.");

            var metalType = delivery.AssetType == AssetType.Gold ? WalletType.Gold : WalletType.Silver;
            var wallet = _walletRepo.GetAll(w => w.UserId == delivery.UserId && w.WalletType == metalType).FirstOrDefault();
            if (wallet is null)
                return Result.Failure($"{metalType} wallet not found.");

            wallet.ReservedBalance -= delivery.Quantity;
            wallet.Balance -= delivery.Quantity;
            await _walletRepo.UpdateAsync(wallet);

            delivery.Status = DeliveryStatus.Processing;
            delivery.TrackingNumber = trackingNumber;
            await _deliveryRepo.UpdateAsync(delivery);

            await _walletTransactionRepo.AddAsync(new WalletTransaction
            {
                WalletId = wallet.Id,
                Type = WalletTransactionType.Debit,
                Amount = delivery.Quantity,
                ReferenceType = ReferenceType.Delivery,
                ReferenceId = delivery.Id,
                CreatedAt = DateTime.UtcNow,
            });

            await _deliveryRepo.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> UpdateStatusAsync(long deliveryId, DeliveryStatus status)
        {
            var delivery = await _deliveryRepo.GetByIdAsync(deliveryId);
            if (delivery is null)
                return Result.Failure("Delivery request not found.");

            var previousStatus = delivery.Status;
            delivery.Status = status;
            await _deliveryRepo.UpdateAsync(delivery);

            if (status == DeliveryStatus.Failed && previousStatus == DeliveryStatus.Pending)
            {
                var metalType = delivery.AssetType == AssetType.Gold ? WalletType.Gold : WalletType.Silver;
                var wallet = _walletRepo.GetAll(w => w.UserId == delivery.UserId && w.WalletType == metalType).FirstOrDefault();
                if (wallet is not null)
                {
                    wallet.ReservedBalance -= delivery.Quantity;
                    await _walletRepo.UpdateAsync(wallet);

                    await _walletTransactionRepo.AddAsync(new WalletTransaction
                    {
                        WalletId = wallet.Id,
                        Type = WalletTransactionType.Release,
                        Amount = delivery.Quantity,
                        ReferenceType = ReferenceType.Delivery,
                        ReferenceId = delivery.Id,
                        CreatedAt = DateTime.UtcNow,
                    });
                }
            }

            await _deliveryRepo.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result<List<DeliveryDto>>> GetUserDeliveriesAsync(long userId)
        {
            var deliveries = _deliveryRepo.GetAll(d => d.UserId == userId).ToList();

            var dtos = new List<DeliveryDto>();
            foreach (var d in deliveries)
            {
                var address = await _addressRepo.GetByIdAsync(d.AddressId);
                dtos.Add(new DeliveryDto
                {
                    Id = d.Id,
                    AssetType = d.AssetType,
                    Quantity = d.Quantity,
                    Status = d.Status,
                    TrackingNumber = d.TrackingNumber,
                    FullAddress = address is null ? null :
                        $"{address.Building}, {address.Street}, {address.Area}, {address.City}",
                    CreatedAt = d.CreatedAt
                });
            }

            return Result<List<DeliveryDto>>.Success(dtos);
        }

        public async Task<Result<DeliveryDto>> GetByIdAsync(long userId, long deliveryId)
        {
            var delivery = await _deliveryRepo.GetByIdAsync(deliveryId);
            if (delivery is null || delivery.UserId != userId)
                return Result<DeliveryDto>.Failure("Delivery request not found.");

            var address = await _addressRepo.GetByIdAsync(delivery.AddressId);

            var dto = new DeliveryDto
            {
                Id = delivery.Id,
                AssetType = delivery.AssetType,
                Quantity = delivery.Quantity,
                Status = delivery.Status,
                TrackingNumber = delivery.TrackingNumber,
                FullAddress = address is null ? null :
                    $"{address.Building}, {address.Street}, {address.Area}, {address.City}",
                CreatedAt = delivery.CreatedAt
            };

            return Result<DeliveryDto>.Success(dto);
        }
    }
}
