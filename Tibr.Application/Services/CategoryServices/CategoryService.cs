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

        // get product counter by category id
        public async Task<Result<IEnumerable<CategoryDto>>> GetAllCategoriesAsync()
        {
            try
            {
                var categories = await _categoryRepository.GetAll()
                    .Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        NameAr = c.NameAr,
                        NameEn = c.NameEn,
                        ProductCount = c.Products.Count(p => !p.IsDeleted)
                    })
                    .ToListAsync();

                return Result<IEnumerable<CategoryDto>>.Success(categories);
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
                var exists = await _categoryRepository.GetByNameAsync(dto.NameAr, dto.NameEn);
                if (exists)
                    return Result<CategoryDto>.Failure(
                        $"Category with name '{dto.NameAr}' or '{dto.NameEn}' already exists");

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

                var exists = await _categoryRepository.GetByNameAsync(dto.NameAr, dto.NameEn);
                if (exists && category.NameAr.ToLower() != dto.NameAr.ToLower()
                           && category.NameEn.ToLower() != dto.NameEn.ToLower())
                    return Result<CategoryDto>.Failure(
                        $"Category name '{dto.NameAr}' or '{dto.NameEn}' already taken");

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

        public async Task<Result<string>> DeleteCategoryAsync(long id)
        {
            try
            {
                var category = await _categoryRepository.GetById(id);

                if (category == null || category.IsDeleted)
                    return Result<string>.Failure("Category not found");

                await _categoryRepository.DeleteAsync(category);
                await _categoryRepository.SaveChangesAsync();

                return Result<string>.Success("Category Deleted Successfully");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Error deleting category: {ex.Message}");
            }
        }
    }
}
