
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Tibr.Application.Dtos.Common;
using Tibr.Application.Dtos.ProductDto;
using Tibr.Application.Services.CategoryServices;
using Tibr.Application.Services.ProductServices;
using Tibr.Domain.Enums;
using Tibr.MVC.Models.Products;

namespace Tibr.MVC.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IProductService productService,
            ICategoryService categoryService,
            IWebHostEnvironment env,
            ILogger<ProductController> logger)
        {
            _productService = productService;
            _categoryService = categoryService;
            _env = env;
            _logger = logger;
        }

        //  GET /Product 
        public async Task<IActionResult> Index(
    string? search = null,
    string? metalType = null,
    string? status = null,
    long? categoryId = null,
    string sortBy = "newest",
    int pageNumber = 1,
    int pageSize = 10)
        {
            var safeSortBy = sortBy == "popularity" ? "newest" : sortBy;

            List<ProductSummaryDto> pageItems;
            int totalCount;

            if (string.IsNullOrEmpty(status))
            {
                // No status filter selected — fetch Active and Inactive counts separately
                // then combine the COUNTS, but only fetch the current page from Active
                // (since Active is what the service defaults to anyway).
                // Better approach: fetch both full lists once, merge, paginate in memory.
                // This is correct because the total catalog is small (hundreds of products).

                var (activeItems, _) = await FetchProducts(search, metalType, categoryId, safeSortBy, 1, 9999, ProductStatus.Active);
                var (inactiveItems, _) = await FetchProducts(search, metalType, categoryId, safeSortBy, 1, 9999, ProductStatus.Inactive);

                var merged = SortMerged([.. activeItems, .. inactiveItems], safeSortBy);
                totalCount = merged.Count;

                pageItems = merged
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
            }
            else
            {
                var parsed = Enum.TryParse<ProductStatus>(status, ignoreCase: true, out var ps)
                    ? ps : ProductStatus.Active;

                // Status explicitly selected — let the service paginate in DB
                var (items, total) = await FetchProducts(search, metalType, categoryId, safeSortBy, pageNumber, pageSize, parsed);
                pageItems = items;
                totalCount = total;
            }

            // ── Stats (always unfiltered) 
            var categoriesResult = await _categoryService.GetAllCategoriesAsync();
            var (allActive, activeCount) = await FetchProducts( null, null, null, "newest", 1, 9999, ProductStatus.Active);
            var (_, inactiveCount) = await FetchProducts(null, null, null, "newest", 1, 1, ProductStatus.Inactive);
            int lowStock = allActive.Count(p => p.Stock > 0 && p.Stock <= 5);
            int outOfStock = allActive.Count(p => p.Stock == 0);

            var rows = pageItems.Select(p => new ProductRowViewModel
            {
                Id = p.Id,
                Name = p.Name,
                MetalType = p.MetalType,
                Weight = p.Weight,
                BuyPrice = p.BuyPrice,
                SellPrice = p.SellPrice,
                Status = p.Status,
                Stock = p.Stock,
                ImageUrl = string.IsNullOrEmpty(p.ImageUrl) ? null : p.ImageUrl,
                PopularityScore = p.PopularityScore,
                CreatedAt = p.CreatedAt
            }).ToList();

            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var vm = new ProductListViewModel
            {
                Products = rows,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPrevious = pageNumber > 1,
                HasNext = pageNumber < totalPages,
                SearchKeyword = search,
                MetalTypeFilter = metalType,
                StatusFilter = status,
                CategoryIdFilter = categoryId,
                SortBy = safeSortBy,
                TotalActive = activeCount,
                TotalLowStock = lowStock,
                TotalOutOfStock = outOfStock,
                CategoryOptions = categoriesResult.IsSuccess
                    ? categoriesResult.Data!.Select(c =>
                        new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    : []
            };

            return View(vm);
        }

        //  GET /Product/Create 
        public async Task<IActionResult> Create()
        {
            return View(new CreateProductViewModel
            {
                CategoryOptions = await GetCategorySelectList()
            });
        }

        //  POST /Product/Create 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProductViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.CategoryOptions = await GetCategorySelectList();
                return View(vm);
            }

            if (vm.SellPrice <= vm.BuyPrice)
            {
                ModelState.AddModelError(nameof(vm.SellPrice),
                    "Sell price must be greater than buy price.");
                vm.CategoryOptions = await GetCategorySelectList();
                return View(vm);
            }

            var dto = new CreateProductDto
            {
                Name = vm.Name.Trim(),
                CategoryId = vm.CategoryId,
                MetalType = vm.MetalType,
                Purity = vm.Purity,
                Weight = vm.Weight,
                BuyPrice = vm.BuyPrice,
                SellPrice = vm.SellPrice,
                Stock = vm.Stock,
                ImageUrl = await SaveImageAsync(vm.ImageFile)
            };

            var result = await _productService.AddProductAsync(dto);

            if (result.IsFailure)
            {
                TempData["Error"] = result.ErrorMessage;
                vm.CategoryOptions = await GetCategorySelectList();
                return View(vm);
            }

            TempData["Success"] = $"Product \"{vm.Name}\" created successfully.";
            return RedirectToAction(nameof(Index));
        }

        //  GET /Product/Edit/5 
        public async Task<IActionResult> Edit(long id)
        {
            var result = await _productService.GetProductByIdAsync(id);

            if (result.IsFailure || result.Data == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction(nameof(Index));
            }

            var p = result.Data;

            // Parse MetalType safely — fallback to Gold if unknown
            var metalType = Enum.TryParse<MetalType>(p.MetalType, ignoreCase: true, out var mt)
                ? mt : MetalType.Gold;

            // Parse ProductStatus safely — fallback to Active if unknown
            var productStatus = Enum.TryParse<ProductStatus>(p.Status, ignoreCase: true, out var ps)
                ? ps : ProductStatus.Active;

           
            long categoryId = 0;
            var categoriesResult = await _categoryService.GetAllCategoriesAsync();
            if (categoriesResult.IsSuccess && !string.IsNullOrEmpty(p.CategoryName))
            {
                var match = categoriesResult.Data!
                    .FirstOrDefault(c => c.Name.Equals(p.CategoryName,
                        StringComparison.OrdinalIgnoreCase));
                if (match != null) categoryId = match.Id;
            }

            var vm = new EditProductViewModel
            {
                Id = p.Id,
                Name = p.Name,
                CategoryId = categoryId,
                MetalType = metalType,
                Purity = p.Purity,
                Weight = p.Weight,
                BuyPrice = p.BuyPrice,
                SellPrice = p.SellPrice,
                Status = productStatus,
                Stock = p.Stock,
                ExistingImageUrl = p.ImageUrl,
                CategoryOptions = categoriesResult.IsSuccess
                    ? categoriesResult.Data!.Select(c =>
                        new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    : []
            };

            return View(vm);
        }

        //  POST /Product/Edit/5 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, EditProductViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.CategoryOptions = await GetCategorySelectList();
                return View(vm);
            }

            if (vm.SellPrice <= vm.BuyPrice)
            {
                ModelState.AddModelError(nameof(vm.SellPrice),
                    "Sell price must be greater than buy price.");
                vm.CategoryOptions = await GetCategorySelectList();
                return View(vm);
            }

            var imageUrl = vm.ImageFile != null
                ? await SaveImageAsync(vm.ImageFile)
                : vm.ExistingImageUrl;

            var dto = new UpdateProductDto
            {
                Name = vm.Name.Trim(),
                CategoryId = vm.CategoryId,
                MetalType = vm.MetalType,
                Purity = vm.Purity,
                Weight = vm.Weight,
                BuyPrice = vm.BuyPrice,
                SellPrice = vm.SellPrice,
                Status = vm.Status,
                Stock = vm.Stock,
                ImageUrl = imageUrl
            };

            var result = await _productService.UpdateProductAsync(id, dto);

            if (result.IsFailure)
            {
                TempData["Error"] = result.ErrorMessage;
                vm.CategoryOptions = await GetCategorySelectList();
                return View(vm);
            }

            TempData["Success"] = $"Product \"{vm.Name}\" updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        //  POST /Product/Delete/5 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _productService.DeleteProductAsync(id);

            TempData[result.IsSuccess ? "Success" : "Error"] =
                result.IsSuccess ? "Product deleted successfully." : result.ErrorMessage;

            return RedirectToAction(nameof(Index));
        }

        // ── GET /Product/Inventory ───────────────────────────────────
        public async Task<IActionResult> Inventory(
            string? search = null,
            string? stockLevel = null,  // "ok" | "low" | "out" | null = all
            string? metalType = null,
            int pageNumber = 1,
            int pageSize = 10)
        {
            
            var (activeItems, _) = await FetchProducts(
                search, metalType, null, "newest", 1, 9999, ProductStatus.Active);
            var (inactiveItems, _) = await FetchProducts(
                search, metalType, null, "newest", 1, 9999, ProductStatus.Inactive);

            var allItems = new List<ProductSummaryDto>();
            allItems.AddRange(activeItems);
            allItems.AddRange(inactiveItems);

            var filtered = stockLevel switch
            {
                "out" => allItems.Where(p => p.Stock == 0).ToList(),
                "low" => allItems.Where(p => p.Stock > 0 && p.Stock <= 5).ToList(),
                "ok" => allItems.Where(p => p.Stock > 5).ToList(),
                _ => allItems
            };

            long totalStockUnits = allItems.Sum(p => p.Stock);
            decimal totalStockValue = allItems.Sum(p => p.Stock * p.SellPrice);
            int lowStockCount = allItems.Count(p => p.Stock > 0 && p.Stock <= 5);
            int outOfStockCount = allItems.Count(p => p.Stock == 0);

            int totalCount = filtered.Count;
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var pagedItems = filtered
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var rows = pagedItems.Select(p => new InventoryRowViewModel
            {
                Id = p.Id,
                Name = p.Name,
                MetalType = p.MetalType,
                Weight = p.Weight,
                Stock = p.Stock,
                SellPrice = p.SellPrice,
                BuyPrice = p.BuyPrice,
                ImageUrl = string.IsNullOrEmpty(p.ImageUrl) ? null : p.ImageUrl,
                Status = p.Status
            }).ToList();

            var vm = new InventoryViewModel
            {
                Products = rows,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPrevious = pageNumber > 1,
                HasNext = pageNumber < totalPages,
                SearchKeyword = search,
                StockFilter = stockLevel,
                MetalTypeFilter = metalType,
                TotalStockUnits = totalStockUnits,
                TotalStockValue = totalStockValue,
                LowStockCount = lowStockCount,
                OutOfStockCount = outOfStockCount
            };

            return View(vm);
        }

        //  POST /Product/UpdateStock 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(UpdateStockViewModel vm)
        {
            if (vm.NewStock < 0)
            {
                TempData["Error"] = "Stock cannot be negative.";
                return RedirectToAction(nameof(Inventory));
            }

            var result = await _productService.UpdateStockAsync(vm.Id, vm.NewStock);

            TempData[result.IsSuccess ? "Success" : "Error"] =
                result.IsSuccess
                    ? $"Stock updated to {vm.NewStock} units."
                    : result.ErrorMessage;

            return RedirectToAction(nameof(Inventory));
        }

        // PRIVATE HELPERS

        /// Calls GetProductsWithFiltersAsync with a guaranteed non-null Status.
        /// This bypasses the service's default-to-Active behaviour.
        /// Returns (items, totalCount).
        private async Task<(List<ProductSummaryDto> items, int total)> FetchProducts(
            string? search,
            string? metalType,
            long? categoryId,
            string sortBy,
            int pageNumber,
            int pageSize,
            ProductStatus status)
        {
            var filterParams = new ProductFilterParams
            {
                SearchKeyword = search,
                MetalType = metalType,
                CategoryId = categoryId,
                SortBy = sortBy,
                PageNumber = pageNumber,
                PageSize = pageSize,
                IncludeOutOfStock = true,   
                Status = status  
            };

            var result = await _productService.GetProductsWithFiltersAsync(filterParams);

            if (result.IsFailure || result.Data == null)
                return ([], 0);

            return (result.Data.Items.ToList(), result.Data.TotalCount);
        }

      
        private static List<ProductSummaryDto> SortMerged(
            List<ProductSummaryDto> items, string sortBy)
        {
            return sortBy switch
            {
                "price_asc" => items.OrderBy(p => p.SellPrice).ToList(),
                "price_desc" => items.OrderByDescending(p => p.SellPrice).ToList(),
                "weight_asc" => items.OrderBy(p => p.Weight).ToList(),
                "weight_desc" => items.OrderByDescending(p => p.Weight).ToList(),
                "purity_asc" => items.OrderBy(p => p.BuyPrice).ToList(),   // no Purity in SummaryDto
                "purity_desc" => items.OrderByDescending(p => p.BuyPrice).ToList(),
                _ => items.OrderByDescending(p => p.CreatedAt).ToList()
            };
        }

        private async Task<IEnumerable<SelectListItem>> GetCategorySelectList()
        {
            var result = await _categoryService.GetAllCategoriesAsync();
            return result.IsSuccess
                ? result.Data!.Select(c =>
                    new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                : [];
        }

        /// Saves an uploaded image to wwwroot/images/products/ and returns the relative URL.
        /// Returns null if no file provided or extension not allowed.
        private async Task<string?> SaveImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(ext)) return null;

            var uploadsDir = Path.Combine(_env.WebRootPath, "images", "products");
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/images/products/{fileName}";
        }
    }
}