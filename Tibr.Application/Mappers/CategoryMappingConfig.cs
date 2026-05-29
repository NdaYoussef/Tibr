using Mapster;
using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.CategoryDto;
using Tibr.Domain.Entities;

namespace Tibr.Application.Mappers
{
    public class CategoryMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Category, CategoryDto>()
                .Map(dest => dest.ProductCount, src =>
                    src.Products != null ? src.Products.Count : 0);

            config.NewConfig<CreateCategoryDto, Category>()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.Products)
                .Ignore(dest => dest.IsDeleted)
                .Ignore(dest => dest.CreatedAt);

            config.NewConfig<UpdateCategoryDto, Category>()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.Products)
                .Ignore(dest => dest.IsDeleted)
                .Ignore(dest => dest.CreatedAt);
        }
    }

}
