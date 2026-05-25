using Mapster;
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;

namespace Tibr.Application.Mappers
{
    public class OrderMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Order, OrderDto>()
                .Map(dest => dest.UserFullName,
                    src => src.User.FirstName + " " + src.User.LastName)
                .Map(dest => dest.Items,
                    src => src.OrderItems);

            config.NewConfig<OrderItem, OrderItemDto>()
                .Map(dest => dest.ProductName,
                    src => src.Product.Name);
        }
    }
}
