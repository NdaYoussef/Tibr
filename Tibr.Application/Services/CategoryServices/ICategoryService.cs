using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.CategoryDto;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.CategoryServices
{
    public interface ICategoryService
    {
        Task<Result<IEnumerable<CategoryDto>>> GetAllCategoriesAsync();
        Task<Result<CategoryDto>> GetCategoryByIdAsync(long id);
        Task<Result<CategoryDto>> AddCategoryAsync(CreateCategoryDto dto);
        Task<Result<CategoryDto>> UpdateCategoryAsync(long id, UpdateCategoryDto dto);
        Task<Result<string>> DeleteCategoryAsync(long id);
    }
}
