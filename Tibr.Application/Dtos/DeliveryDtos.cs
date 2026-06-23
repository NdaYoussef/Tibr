using Tibr.Domain.Enums;

namespace Tibr.Application.Dtos
{
    public class CreateDeliveryRequestDto
    {
        public AssetType AssetType { get; set; }
        public decimal Quantity { get; set; }
        public long AddressId { get; set; }
    }

    public class DeliveryDto
    {
        public long Id { get; set; }
        public AssetType AssetType { get; set; }
        public decimal Quantity { get; set; }
        public DeliveryStatus Status { get; set; }
        public string? TrackingNumber { get; set; }
        public string? FullAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
