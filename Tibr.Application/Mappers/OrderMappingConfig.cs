using Mapster;
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;

namespace Tibr.Application.Mappers
{
    public class OrderMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // =======================
            // Order -> OrderDto
            // =======================
            config.NewConfig<Order, OrderDto>()
                .Map(dest => dest.UserFullName,
                    src => src.User != null
                        ? $"{src.User.FirstName} {src.User.LastName}"
                        : string.Empty)
                .Map(dest => dest.UserId,
                    src => src.UserId)
                .Map(dest => dest.Items,
                    src => src.OrderItems ?? new List<OrderItem>())
                .Map(dest => dest.OrderNumber,
                    src => src.OrderNumber)
                .Map(dest => dest.TotalAmount,
                    src => src.TotalAmount)
                .Map(dest => dest.PaymentStatus,
                    src => src.PaymentStatus.ToString())
                .Map(dest => dest.OrderStatus,
                    src => src.OrderStatus.ToString())
                .Map(dest => dest.CreatedAt,
                    src => src.CreatedAt);

            // =======================
            // OrderItem -> OrderItemDto
            // =======================
            config.NewConfig<OrderItem, OrderItemDto>()
                .Map(dest => dest.ProductName,
                    src => src.Product != null
                        ? src.Product.Name
                        : string.Empty)
                .Map(dest => dest.ProductId,
                    src => src.ProductId)
                .Map(dest => dest.Quantity,
                    src => src.Quantity)
                .Map(dest => dest.Price,
                    src => src.Price);
        }
    }
}