using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tibr.Application.Dtos.Common;
using Tibr.Application.Dtos.ProductDto;
using Tibr.Application.IServices;
using Tibr.Domain.Entities;
using Tibr.Domain.Enums;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services
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

                var query = _productRepository.GetProductsWithPopularityQuery()
                .OrderByDescending(p => p.CreatedAt);

                var totalCount = await _productRepository.CountAsync(query);
                var products = await _productRepository.GetPagedProductsAsync(
                    query, paginationParams.Skip, paginationParams.PageSize);

                var dtos = products.Adapt<List<ProductSummaryDto>>();
                for (int i = 0; i < products.Count; i++)
                    dtos[i].PopularityScore =
                        (products[i].Favorites?.Count ?? 0) +
                        (products[i].OrderItems?.Count ?? 0);

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
                var product = await _productRepository.GetProductByIdAsync(id);

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
                var query = filterParams.SortBy == "popularity"
                    ? _productRepository.GetProductsWithPopularityQuery()
                    : _productRepository.GetProductsQuery();

                // Apply status filter
                if (filterParams.Status.HasValue)
                    query = query.Where(p => p.Status == filterParams.Status.Value);
                else
                    query = query.Where(p => p.Status == ProductStatus.Active);

                // Apply search keyword filter (Name or MetalType)
                if (!string.IsNullOrWhiteSpace(filterParams.SearchKeyword))
                {
                    var keyword = filterParams.SearchKeyword.ToLower();
                    query = query.Where(p =>
                        p.Name.ToLower().Contains(keyword) ||
                        p.MetalType.ToString().ToLower().Contains(keyword));
                }

                // Apply category filter
                if (filterParams.CategoryId.HasValue)
                    query = query.Where(p => p.CategoryId == filterParams.CategoryId.Value);

                if (!string.IsNullOrWhiteSpace(filterParams.CategoryName))
                    query = query.Where(p => p.Category.Name == filterParams.CategoryName);

                // Apply metal type filter
                if (!string.IsNullOrWhiteSpace(filterParams.MetalType))
                {
                    if (Enum.TryParse<MetalType>(filterParams.MetalType, ignoreCase: true, out var metalType))
                        query = query.Where(p => p.MetalType == metalType);
                }

                // Apply weight range filter
                if (filterParams.MinWeight.HasValue)
                    query = query.Where(p => p.Weight >= filterParams.MinWeight.Value);
                if (filterParams.MaxWeight.HasValue)
                    query = query.Where(p => p.Weight <= filterParams.MaxWeight.Value);

                // Apply purity range filter
                if (filterParams.MinPurity.HasValue)
                    query = query.Where(p => p.Purity >= filterParams.MinPurity.Value);
                if (filterParams.MaxPurity.HasValue)
                    query = query.Where(p => p.Purity <= filterParams.MaxPurity.Value);

                // Apply price range filter (using SellPrice)
                if (filterParams.MinPrice.HasValue)
                    query = query.Where(p => p.SellPrice >= filterParams.MinPrice.Value);
                if (filterParams.MaxPrice.HasValue)
                    query = query.Where(p => p.SellPrice <= filterParams.MaxPrice.Value);

                // Apply stock filter
                if (!filterParams.IncludeOutOfStock)
                    query = query.Where(p => p.Stock > 0);

                // SORTING
                query = ApplySorting(query, filterParams.SortBy);

                var totalCount = await _productRepository.CountAsync(query);
                var products = await _productRepository.GetPagedProductsAsync(
                    query, filterParams.Skip, filterParams.PageSize);

                var dtos = products.Adapt<List<ProductSummaryDto>>();
                for (int i = 0; i < products.Count; i++)
                    dtos[i].PopularityScore =
                        (products[i].Favorites?.Count ?? 0) +
                        (products[i].OrderItems?.Count ?? 0);

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
                var product = await _productRepository.GetProductByIdAsync(id);

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
        public async Task<Result> UpdateStockAsync(long id, decimal newStock)
        {
            try
            {
                var product = await _productRepository.GetById(id);

                if (product == null || product.IsDeleted)
                    return Result.Failure("Product not found");

                if (newStock < 0)
                    return Result.Failure("Stock cannot be negative");

                product.Stock = newStock;
                product.UpdatedAt = DateTime.UtcNow;

                await _productRepository.UpdateAsync(product);
                await _productRepository.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error updating stock: {ex.Message}");
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
        public async Task<Result> DeleteProductAsync(long id)
        {
            try
            {
                var product = await _productRepository.GetById(id);

                if (product == null || product.IsDeleted)
                    return Result.Failure("Product not found");

                await _productRepository.DeleteAsync(product);
                await _productRepository.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error deleting product: {ex.Message}");
            }
        }

        // HELPER METHODS - SORTING
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
