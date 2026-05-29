using Mapster;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.CategoryDto;
using Tibr.Domain.Entities;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.CategoryServices
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository
                ?? throw new ArgumentNullException(nameof(categoryRepository));
        }

        public async Task<Result<IEnumerable<CategoryDto>>> GetAllCategoriesAsync()
        {
            try
            {
                var dtos = await _categoryRepository.GetAll()
                    .Include(c => c.Products.Where(p => !p.IsDeleted))
                    .ProjectToType<CategoryDto>()
                    .ToListAsync();

                return Result<IEnumerable<CategoryDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<CategoryDto>>
                    .Failure($"Error retrieving categories: {ex.Message}");
            }
        }

        public async Task<Result<CategoryDto>> GetCategoryByIdAsync(long id)
        {
            try
            {
                var category = await _categoryRepository.GetAll()
                                     .Include(c => c.Products.Where(p => !p.IsDeleted))
                                     .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted); 

                if (category is null)
                    return Result<CategoryDto>.Failure("Category not found");

                return Result<CategoryDto>.Success(category.Adapt<CategoryDto>());
            }
            catch (Exception ex)
            {
                return Result<CategoryDto>
                    .Failure($"Error retrieving category: {ex.Message}");
            }
        }

        public async Task<Result<CategoryDto>> AddCategoryAsync(CreateCategoryDto dto)
        {
            try
            {
                // Business rule — no duplicate category names
                var exists = await _categoryRepository.GetByNameAsync(dto.Name);
                if (exists)
                    return Result<CategoryDto>.Failure(
                        $"Category '{dto.Name}' already exists");

                var category = dto.Adapt<Category>();

                await _categoryRepository.AddAsync(category);
                await _categoryRepository.SaveChangesAsync();

                return Result<CategoryDto>.Success(category.Adapt<CategoryDto>());
            }
            catch (Exception ex)
            {
                return Result<CategoryDto>
                    .Failure($"Error adding category: {ex.Message}");
            }
        }
        public async Task<Result<CategoryDto>> UpdateCategoryAsync(long id, UpdateCategoryDto dto)
        {
            try
            {
                var category = await _categoryRepository.GetAll()
                                     .Include(c => c.Products.Where(p => !p.IsDeleted))
                                     .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

                if (category is null)
                    return Result<CategoryDto>.Failure("Category not found");

                var exists = await _categoryRepository.GetByNameAsync(dto.Name);
                if (exists && category.Name.ToLower() != dto.Name.ToLower())
                    return Result<CategoryDto>.Failure(
                        $"Category name '{dto.Name}' already taken");

                dto.Adapt(category);
                category.UpdatedAt = DateTime.UtcNow;

                await _categoryRepository.UpdateAsync(category);
                await _categoryRepository.SaveChangesAsync();

                return Result<CategoryDto>.Success(category.Adapt<CategoryDto>());
            }
            catch (Exception ex)
            {
                return Result<CategoryDto>
                    .Failure($"Error updating category: {ex.Message}");
            }
        }

        public async Task<Result> DeleteCategoryAsync(long id)
        {
            try
            {
                var category = await _categoryRepository.GetById(id);

                if (category == null || category.IsDeleted)
                    return Result.Failure("Category not found");

                await _categoryRepository.DeleteAsync(category);
                await _categoryRepository.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error deleting category: {ex.Message}");
            }
        }
    }
}
