
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos.CategoryDto;
using Tibr.Application.Services.CategoryServices;
using Tibr.MVC.Models.Categories;

namespace Tibr.MVC.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(
            ICategoryService categoryService,
            ILogger<CategoryController> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        //  GET /Category 
        // Shows all categories + inline Add form on the same page
        public async Task<IActionResult> Index()
        {
            var result = await _categoryService.GetAllCategoriesAsync();

            if (result.IsFailure)
            {
                TempData["Error"] = result.ErrorMessage;
                return View(new CategoryListViewModel());
            }

            var vm = new CategoryListViewModel
            {
                Categories = result.Data!.Select(c => new CategoryRowViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    ProductCount = c.ProductCount
                }),
                NewCategory = new CreateCategoryViewModel()
            };

            return View(vm);
        }

        //  POST /Category/Create 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Re-render index with validation errors
                var allResult = await _categoryService.GetAllCategoriesAsync();
                var vm = new CategoryListViewModel
                {
                    Categories = allResult.IsSuccess
                        ? allResult.Data!.Select(c => new CategoryRowViewModel
                        { Id = c.Id, Name = c.Name, ProductCount = c.ProductCount })
                        : [],
                    NewCategory = model
                };
                return View("Index", vm);
            }

            var dto = new CreateCategoryDto { Name = model.Name.Trim() };
            var result = await _categoryService.AddCategoryAsync(dto);

            TempData[result.IsSuccess ? "Success" : "Error"] =
                result.IsSuccess
                    ? $"Category \"{model.Name}\" created successfully."
                    : result.ErrorMessage;

            return RedirectToAction(nameof(Index));
        }

        //  GET /Category/Edit/5  (returns partial for modal) 
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var result = await _categoryService.GetCategoryByIdAsync(id);

            if (result.IsFailure || result.Data == null)
                return NotFound();

            var vm = new EditCategoryViewModel
            {
                Id = result.Data.Id,
                Name = result.Data.Name
            };

            return PartialView("_EditCategoryModal", vm);
        }

        //  POST /Category/Edit/5 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, EditCategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please provide a valid category name.";
                return RedirectToAction(nameof(Index));
            }

            var dto = new UpdateCategoryDto { Name = model.Name.Trim() };
            var result = await _categoryService.UpdateCategoryAsync(id, dto);

            TempData[result.IsSuccess ? "Success" : "Error"] =
                result.IsSuccess
                    ? $"Category updated to \"{model.Name}\"."
                    : result.ErrorMessage;

            return RedirectToAction(nameof(Index));
        }

        //  POST /Category/Delete/5 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _categoryService.DeleteCategoryAsync(id);

            TempData[result.IsSuccess ? "Success" : "Error"] =
                result.IsSuccess
                    ? "Category deleted successfully."
                    : result.ErrorMessage;

            return RedirectToAction(nameof(Index));
        }
    }
}