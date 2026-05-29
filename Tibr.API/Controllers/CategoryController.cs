using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos.CategoryDto;
using Tibr.Application.Services.CategoryServices;

namespace Tibr.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // GET api/categories
        [HttpGet]
        public async Task<IActionResult> GetAllCategories()
        {
            var result = await _categoryService.GetAllCategoriesAsync();

            if (result.IsFailure)
                return BadRequest(result.ErrorMessage);

            return Ok(result.Data);
        }

        // GET api/categories/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(long id)
        {
            var result = await _categoryService.GetCategoryByIdAsync(id);

            if (result.IsFailure)
                return NotFound(result.ErrorMessage);

            return Ok(result.Data);
        }

        // POST api/categories
        // [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _categoryService.AddCategoryAsync(dto);

            if (result.IsFailure)
                return BadRequest(result.ErrorMessage);

            return CreatedAtAction(
                nameof(GetCategoryById),
                new { id = result.Data!.Id },
                result.Data);
        }

        // PUT api/categories/{id}
        // [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(long id, [FromBody] UpdateCategoryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _categoryService.UpdateCategoryAsync(id, dto);

            if (result.IsFailure)
                return NotFound(result.ErrorMessage);

            return Ok(result.Data);
        }

        // DELETE api/categories/{id}
        // [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(long id)
        {
            var result = await _categoryService.DeleteCategoryAsync(id);

            if (result.IsFailure)
                return NotFound(result.ErrorMessage);

            return NoContent();
        }
    }
}
