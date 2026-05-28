using Mapster;
using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.ProductDto;
using Tibr.Domain.Entities;
using Tibr.Domain.Enums;

namespace Tibr.Application.Mappers
{
    public class ProductMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // ProductSummaryDto mapping
            config.NewConfig<Product, ProductSummaryDto>()
                .Map(dest => dest.MetalType, src => src.MetalType.ToString())
                .Map(dest => dest.Status, src => src.Status.ToString())
                .Map(dest => dest.CreatedAt, src => src.CreatedAt)
                 .Map(dest => dest.PopularityScore, src =>
                                   src.Favorites.Count() + src.OrderItems.Count());

            // ProductDetailsDto mapping
            config.NewConfig<Product, ProductDetailsDto>()
               .Map(dest => dest.MetalType, src => src.MetalType.ToString())
               .Map(dest => dest.Status, src => src.Status.ToString())
               .Map(dest => dest.CategoryName, src => src.Category != null
                                                   ? src.Category.Name : string.Empty);

            // CreateProductDto to Product mapping
            config.NewConfig<CreateProductDto, Product>()
                .Ignore(dest => dest.Id)             
                .Ignore(dest => dest.Status)         
                .Ignore(dest => dest.Category)        
                .Ignore(dest => dest.Favorites)      
                .Ignore(dest => dest.CartItems)       
                .Ignore(dest => dest.OrderItems)      
                .Ignore(dest => dest.IsDeleted)       
                .Ignore(dest => dest.CreatedAt);

            // UpdateProductDto to Product mapping
            config.NewConfig<UpdateProductDto, Product>()
                .Map(dest => dest.Status, src => src.Status)
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.Category)
                .Ignore(dest => dest.Favorites)
                .Ignore(dest => dest.CartItems)
                .Ignore(dest => dest.OrderItems)
                .Ignore(dest => dest.IsDeleted)
                .Ignore(dest => dest.CreatedAt);     


        }
    }
}
