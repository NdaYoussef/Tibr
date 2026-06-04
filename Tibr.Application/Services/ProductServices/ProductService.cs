using Mapster;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tibr.Application.Dtos.Common;
using Tibr.Application.Dtos.ProductDto;
using Tibr.Domain.Entities;
using Tibr.Domain.Enums;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.ProductServices
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository
                ?? throw new ArgumentNullException(nameof(productRepository));
        }

        // GET OPERATIONS
        /// Get all active products with pagination
        public async Task<Result<PaginatedResult<ProductSummaryDto>>> GetAllProductsAsync(PaginationParams paginationParams)
        {
            try
            {

                var query = _productRepository.GetAll()
                    .Include(p => p.Category)
                    .Include(p => p.Favorites)    
                    .Include(p => p.OrderItems)   
                    .OrderByDescending(p => p.CreatedAt);

                var totalCount = await query.CountAsync();
                var dtos = await query
                .Skip(paginationParams.Skip)
                .Take(paginationParams.PageSize)
                .ProjectToType<ProductSummaryDto>()
                .ToListAsync();

                var paginatedResult = new PaginatedResult<ProductSummaryDto>(
                    dtos,
                    paginationParams.PageNumber,
                    paginationParams.PageSize,
                    totalCount);

                return Result<PaginatedResult<ProductSummaryDto>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                return Result<PaginatedResult<ProductSummaryDto>>.Failure($"Error retrieving products: {ex.Message}");
            }
        }

        /// Get product details by ID
        public async Task<Result<ProductDetailsDto>> GetProductByIdAsync(long id)
        {
            try
            {
                var product = await _productRepository.GetAll()
                .Include(p => p.Category)   
                .FirstOrDefaultAsync(p => p.Id == id); 

                if (product == null || product.IsDeleted)
                    return Result<ProductDetailsDto>.Failure("Product not found");

                var dto = product.Adapt<ProductDetailsDto>();
                return Result<ProductDetailsDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<ProductDetailsDto>.Failure($"Error retrieving product: {ex.Message}");
            }
        }

        // ADVANCED FILTERING & PAGINATION
        /// Get products with advanced filtering, sorting, and pagination
        /// Supports: Weight, Purity, Price Range, Popularity, Newest, Price Sorting
        public async Task<Result<PaginatedResult<ProductSummaryDto>>> GetProductsWithFiltersAsync(ProductFilterParams filterParams)
        {
            try
            {
                // Choose query based on whether popularity sort is needed
                var needsPopularity = filterParams.SortBy == "popularity";
                IQueryable<Product> query = _productRepository.GetAll()
                    .Include(p => p.Category);


                // Only include these if needed for popularity sort
                if (needsPopularity)
                {
                    query = query
                        .Include(p => p.Favorites)
                        .Include(p => p.OrderItems);
                }

                query = ApplyFilters(query, filterParams);
                query = ApplySorting(query, filterParams.SortBy);

                var totalCount = await query.CountAsync();
                var dtos = await query
                                 .Skip(filterParams.Skip)
                                 .Take(filterParams.PageSize)
                                  .ProjectToType<ProductSummaryDto>()
                                 .ToListAsync();


                var paginatedResult = new PaginatedResult<ProductSummaryDto>(
                    dtos,
                    filterParams.PageNumber,
                    filterParams.PageSize,
                    totalCount);

                return Result<PaginatedResult<ProductSummaryDto>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                return Result<PaginatedResult<ProductSummaryDto>>.Failure($"Error filtering products: {ex.Message}");
            }
        }

        // CREATE, UPDATE, DELETE OPERATIONS
        /// Create a new product
        public async Task<Result<ProductDetailsDto>> AddProductAsync(CreateProductDto dto)
        {
            try
            {
                var product = dto.Adapt<Product>();
                product.Status = ProductStatus.Active;

                await _productRepository.AddAsync(product);
                await _productRepository.SaveChangesAsync();

                var resultDto = product.Adapt<ProductDetailsDto>();
                return Result<ProductDetailsDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                return Result<ProductDetailsDto>.Failure($"Error adding product: {ex.Message}");
            }
        }

        /// Update an existing product
        public async Task<Result<ProductDetailsDto>> UpdateProductAsync(long id, UpdateProductDto dto)
        {
            try
            {
                var product = await _productRepository.GetAll()
                                                 .Include(p => p.Category)
                                                 .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null || product.IsDeleted)
                    return Result<ProductDetailsDto>.Failure("Product not found");

                // Map the DTO to the entity
                dto.Adapt(product);

                // Update the modification timestamp
                product.UpdatedAt = DateTime.UtcNow;

                await _productRepository.UpdateAsync(product);
                await _productRepository.SaveChangesAsync();

                var resultDto = product.Adapt<ProductDetailsDto>();
                return Result<ProductDetailsDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                return Result<ProductDetailsDto>.Failure($"Error updating product: {ex.Message}");
            }
        }

        /// Update product stock
        public async Task<Result<string>> UpdateStockAsync(long id, long newStock)
        {
            try
            {
                var product = await _productRepository.GetById(id);

                if (product == null || product.IsDeleted)
                    return Result<string>.Failure("Product not found");

                if (newStock < 0)
                    return Result<string>.Failure("Stock cannot be negative");

                product.Stock = newStock;
                product.UpdatedAt = DateTime.UtcNow;

                await _productRepository.UpdateAsync(product);
                await _productRepository.SaveChangesAsync();

               
                return Result<string>.Success($"Stock updated successfully to {newStock} units.");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Error updating stock: {ex.Message}");
            }
        }

        /// Check available stock for a product
        public async Task<Result<decimal>> CheckStockAsync(long id)
        {
            try
            {
                var product = await _productRepository.GetById(id);

                if (product == null || product.IsDeleted)
                    return Result<decimal>.Failure("Product not found");

                return Result<decimal>.Success(product.Stock);
            }
            catch (Exception ex)
            {
                return Result<decimal>.Failure($"Error checking stock: {ex.Message}");
            }
        }

        /// Delete a product (soft delete)
        public async Task<Result<string>> DeleteProductAsync(long id)
        {
            try
            {
                var product = await _productRepository.GetById(id);

                if (product == null || product.IsDeleted)
                    return Result<string>.Failure("Product not found");

                await _productRepository.DeleteAsync(product);
                await _productRepository.SaveChangesAsync();

                return Result<string>.Success("Product deleted successfully.");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Error deleting product: {ex.Message}");
            }
        }

        // HELPER METHODS 
        /// Apply Filters t
        private IQueryable<Product> ApplyFilters(
           IQueryable<Product> query, ProductFilterParams filterParams)
        {
            query = filterParams.Status.HasValue
                ? query.Where(p => p.Status == filterParams.Status.Value)
                : query.Where(p => p.Status == ProductStatus.Active);

            if (!string.IsNullOrWhiteSpace(filterParams.SearchKeyword))
            {
                var keyword = filterParams.SearchKeyword.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(keyword) ||
                    p.MetalType.ToString().ToLower().Contains(keyword));
            }

            if (filterParams.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == filterParams.CategoryId.Value);

            if (!string.IsNullOrWhiteSpace(filterParams.CategoryName))
                query = query.Where(p => p.Category.Name == filterParams.CategoryName);

            if (!string.IsNullOrWhiteSpace(filterParams.MetalType))
                if (Enum.TryParse<MetalType>(filterParams.MetalType,
                    ignoreCase: true, out var metalType))
                    query = query.Where(p => p.MetalType == metalType);

            if (filterParams.MinWeight.HasValue)
                query = query.Where(p => p.Weight >= filterParams.MinWeight.Value);
            if (filterParams.MaxWeight.HasValue)
                query = query.Where(p => p.Weight <= filterParams.MaxWeight.Value);

            if (filterParams.MinPurity.HasValue)
                query = query.Where(p => p.Purity >= filterParams.MinPurity.Value);
            if (filterParams.MaxPurity.HasValue)
                query = query.Where(p => p.Purity <= filterParams.MaxPurity.Value);

            if (filterParams.MinPrice.HasValue)
                query = query.Where(p => p.SellPrice >= filterParams.MinPrice.Value);
            if (filterParams.MaxPrice.HasValue)
                query = query.Where(p => p.SellPrice <= filterParams.MaxPrice.Value);

            if (!filterParams.IncludeOutOfStock)
                query = query.Where(p => p.Stock > 0);

            return query;
        }
        /// Apply sorting to the query based on sortBy parameter
        private IQueryable<Product> ApplySorting(IQueryable<Product> query, string sortBy)
        {
            return sortBy?.ToLower() switch
            {
                "price_asc" => query.OrderBy(p => p.SellPrice),
                "price_desc" => query.OrderByDescending(p => p.SellPrice),
                "weight_asc" => query.OrderBy(p => p.Weight),
                "weight_desc" => query.OrderByDescending(p => p.Weight),
                "purity_asc" => query.OrderBy(p => p.Purity),
                "purity_desc" => query.OrderByDescending(p => p.Purity),
                "popularity" => query.OrderByDescending(p => p.Favorites != null ? p.Favorites.Count : 0),
                "newest" or _ => query.OrderByDescending(p => p.CreatedAt)
            };
        }
    }
}
