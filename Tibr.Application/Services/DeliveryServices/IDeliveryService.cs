using Tibr.Application.Dtos;
using Tibr.Domain.Enums;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.DeliveryServices
{
    public interface IDeliveryService
    {
        Task<Result<DeliveryDto>> CreateRequestAsync(long userId, CreateDeliveryRequestDto dto);
        Task<Result> ConfirmDispatchAsync(long deliveryId, string trackingNumber);
        Task<Result> UpdateStatusAsync(long deliveryId, DeliveryStatus status);
        Task<Result<List<DeliveryDto>>> GetUserDeliveriesAsync(long userId);
        Task<Result<DeliveryDto>> GetByIdAsync(long userId, long deliveryId);
    }
}
