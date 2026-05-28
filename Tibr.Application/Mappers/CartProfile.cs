using AutoMapper;
using System.Linq;
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;

namespace Tibr.Application.Mappers
{
    public class CartProfile : Profile
    {
        public CartProfile()
        {
            CreateMap<CartItem, CartItemDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty));

            CreateMap<Cart, CartDto>()
                .ForMember(dest => dest.CartItems, opt => opt.MapFrom(src => src.CartItems.Where(ci => !ci.IsDeleted)));
        }
    }
}
