using Mapster;
using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.ProductDto;
using Tibr.Domain.Entities;

namespace Tibr.Application.Mappers
{
    public class ProductMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Product, ProductSummaryDto>()
                .Map(dest => dest.Status, src => src.Status.ToString())
                .Map(dest => dest.CreatedAt, src => src.CreatedAt)
                .Map(dest => dest.PopularityScore, src =>
                                           (src.Favorites != null ? src.Favorites.Count : 0) +
                                           (src.OrderItems != null ? src.OrderItems.Count : 0)); 

            config.NewConfig<Product, ProductDetailsDto>()
              .Map(dest => dest.CategoryName, src => src.Category != null
                                                   ? src.Category.Name : string.Empty)
              .Map(dest => dest.Status, src => src.Status.ToString());

            config.NewConfig<CreateProductDto, Product>()
                .Ignore(dest => dest.Id)             
                .Ignore(dest => dest.ImageUrl)        
                .Ignore(dest => dest.Status)         
                .Ignore(dest => dest.Category)        
                .Ignore(dest => dest.Favorites)      
                .Ignore(dest => dest.CartItems)       
                .Ignore(dest => dest.OrderItems)      
                .Ignore(dest => dest.IsDeleted)       
                .Ignore(dest => dest.CreatedAt);

            config.NewConfig<UpdateProductDto, Product>()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.ImageUrl)
                .Ignore(dest => dest.Status)
                .Ignore(dest => dest.Category)
                .Ignore(dest => dest.Favorites)
                .Ignore(dest => dest.CartItems)
                .Ignore(dest => dest.OrderItems)
                .Ignore(dest => dest.IsDeleted)
                .Ignore(dest => dest.CreatedAt);     
                

        }
    }
}
