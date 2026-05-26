using System;
using System.Collections.Generic;
using Tibr.Application.Dtos.Common;
using Tibr.Application.Dtos.ProductDto;
using Tibr.Domain.Enums;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.IServices
{
    public interface IProductService
    {
        // GET OPERATIONS
        Task<Result<PaginatedResult<ProductSummaryDto>>> GetAllProductsAsync(PaginationParams paginationParams);

        /// Get product details by ID
        Task<Result<ProductDetailsDto>> GetProductByIdAsync(long id);

        // ADVANCED FILTERING & PAGINATION
        /// Get products with advanced filtering, sorting, and pagination
        /// Supports: Weight, Purity, Price Range, Popularity, Newest, Price Sorting
        Task<Result<PaginatedResult<ProductSummaryDto>>> GetProductsWithFiltersAsync(ProductFilterParams filterParams);     

        // CREATE, UPDATE, DELETE OPERATIONS
        Task<Result<ProductDetailsDto>> AddProductAsync(CreateProductDto dto);
        Task<Result<ProductDetailsDto>> UpdateProductAsync(long id, UpdateProductDto dto);

        /// Update product stock
        Task<Result> UpdateStockAsync(long id, decimal newStock);

        /// Check available stock for a product
        Task<Result<decimal>> CheckStockAsync(long id);

        /// Delete a product
        Task<Result> DeleteProductAsync(long id);
    }
}
                    