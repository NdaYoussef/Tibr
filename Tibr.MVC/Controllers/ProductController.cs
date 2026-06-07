
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
            var filterParams = new ProductFilterParams
            {
                SearchKeyword = search,
                MetalType = metalType,
                CategoryId = categoryId,
                SortBy = sortBy,
                PageNumber = pageNumber,
                PageSize = pageSize,
                IncludeOutOfStock = true    
            };

            // Parse status filter
            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<ProductStatus>(status, ignoreCase: true, out var ps))
                    filterParams.Status = ps;
            }

            var result = await _productService.GetProductsWithFiltersAsync(filterParams);
            var categoriesResult = await _categoryService.GetAllCategoriesAsync();

            if (result.IsFailure)
            {
                TempData["Error"] = result.ErrorMessage;
                return View(new ProductListViewModel());
            }

            var paged = result.Data!;

            // Map to row ViewModels
            var rows = paged.Items.Select(p => new ProductRowViewModel
            {
                Id = p.Id,
                Name = p.Name,
                MetalType = p.MetalType,
                Weight = p.Weight,
                BuyPrice = p.BuyPrice,
                SellPrice = p.SellPrice,
                Status = p.Status,
                Stock = p.Stock,
                ImageUrl = p.ImageUrl,
                PopularityScore = p.PopularityScore,
                CreatedAt = p.CreatedAt
            }).ToList();

            // Quick stats from current filter results
            var allResult = await _productService.GetAllProductsAsync(
                new PaginationParams { PageNumber = 1, PageSize = 1000 });
            var allItems = allResult.Data?.Items ?? [];

            var vm = new ProductListViewModel
            {
                Products = rows,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount,
                TotalPages = paged.TotalPages,
                HasPrevious = paged.HasPreviousPage,
                HasNext = paged.HasNextPage,
                SearchKeyword = search,
                MetalTypeFilter = metalType,
                StatusFilter = status,
                CategoryIdFilter = categoryId,
                SortBy = sortBy,
                TotalActive = allItems.Count(p => p.Status == "Active"),
                TotalLowStock = allItems.Count(p => p.Stock > 0 && p.Stock <= 5),
                TotalOutOfStock = allItems.Count(p => p.Stock == 0),
                CategoryOptions = categoriesResult.IsSuccess
                    ? categoriesResult.Data!.Select(c => new SelectListItem
                    { Value = c.Id.ToString(), Text = c.Name })
                    : []
            };

            return View(vm);
        }

        //  GET /Product/Create 
        public async Task<IActionResult> Create()
        {
            var vm = new CreateProductViewModel
            {
                CategoryOptions = await GetCategorySelectList()
            };
            return View(vm);
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

            // Validate pricing
            if (vm.SellPrice <= vm.BuyPrice)
            {
                ModelState.AddModelError(nameof(vm.SellPrice),
                    "Sell price must be greater than buy price.");
                vm.CategoryOptions = await GetCategorySelectList();
                return View(vm);
            }

            var dto = new CreateProductDto
            {
                Name = vm.Name,
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
            var vm = new EditProductViewModel
            {
                Id = p.Id,
                Name = p.Name,
                CategoryId = 0,    
                MetalType = Enum.Parse<MetalType>(p.MetalType),
                Purity = p.Purity,
                Weight = p.Weight,
                BuyPrice = p.BuyPrice,
                SellPrice = p.SellPrice,
                Status = Enum.Parse<ProductStatus>(p.Status),
                Stock = p.Stock,
                ExistingImageUrl = p.ImageUrl,
                CategoryOptions = await GetCategorySelectList()
            };

            return View(vm);
        }

        // POST /Product/Edit/5 
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

            // Handle image: use new upload if provided, else keep existing
            var imageUrl = vm.ImageFile != null
                ? await SaveImageAsync(vm.ImageFile)
                : vm.ExistingImageUrl;

            var dto = new UpdateProductDto
            {
                Name = vm.Name,
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

        // POST /Product/Delete/5 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _productService.DeleteProductAsync(id);

            TempData[result.IsSuccess ? "Success" : "Error"] =
                result.IsSuccess ? "Product deleted successfully." : result.ErrorMessage;

            return RedirectToAction(nameof(Index));
        }

        //  GET /Product/Inventory 
        public async Task<IActionResult> Inventory(
            string? search = null,
            string? stockLevel = null,
            string? metalType = null,
            int pageNumber = 1,
            int pageSize = 10)
        {
            var filterParams = new ProductFilterParams
            {
                SearchKeyword = search,
                MetalType = metalType,
                PageNumber = pageNumber,
                PageSize = pageSize,
                IncludeOutOfStock = true,
                SortBy = "newest"
            };

            // Stock level filter
            if (stockLevel == "out")
            {
                filterParams.MinPrice = null;   // no price filter
                // We'll filter in memory after fetch for out-of-stock
            }

            var result = await _productService.GetProductsWithFiltersAsync(filterParams);

            if (result.IsFailure)
            {
                TempData["Error"] = result.ErrorMessage;
                return View(new InventoryViewModel());
            }

            var paged = result.Data!;
            var items = paged.Items.ToList();

            // Apply stock level filter in memory (simple, small result sets)
            if (stockLevel == "out") items = items.Where(p => p.Stock == 0).ToList();
            if (stockLevel == "low") items = items.Where(p => p.Stock > 0 && p.Stock <= 5).ToList();
            if (stockLevel == "ok") items = items.Where(p => p.Stock > 5).ToList();

            var rows = items.Select(p => new InventoryRowViewModel
            {
                Id = p.Id,
                Name = p.Name,
                MetalType = p.MetalType,
                Weight = p.Weight,
                Stock = p.Stock,
                SellPrice = p.SellPrice,
                BuyPrice = p.BuyPrice,
                ImageUrl = p.ImageUrl,
                Status = p.Status
            }).ToList();

            var vm = new InventoryViewModel
            {
                Products = rows,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount,
                TotalPages = paged.TotalPages,
                HasPrevious = paged.HasPreviousPage,
                HasNext = paged.HasNextPage,
                SearchKeyword = search,
                StockFilter = stockLevel,
                MetalTypeFilter = metalType,
                TotalStockUnits = rows.Sum(r => r.Stock),
                TotalStockValue = rows.Sum(r => r.StockValue),
                LowStockCount = rows.Count(r => r.Stock > 0 && r.Stock <= 5),
                OutOfStockCount = rows.Count(r => r.Stock == 0)
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

        //  Helpers 

        private async Task<IEnumerable<SelectListItem>> GetCategorySelectList()
        {
            var result = await _categoryService.GetAllCategoriesAsync();
            return result.IsSuccess
                ? result.Data!.Select(c => new SelectListItem
                { Value = c.Id.ToString(), Text = c.Name })
                : [];
        }

        /// Saves an uploaded image to wwwroot/images/products/ and returns the relative URL.
        /// Returns null if no file is provided.
        private async Task<string?> SaveImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            var uploadsDir = Path.Combine(_env.WebRootPath, "images", "products");
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(ext)) return null;

            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/images/products/{fileName}";
        }
    }
}