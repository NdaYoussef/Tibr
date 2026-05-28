using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos.Common;
using Tibr.Application.Dtos.ProductDto;
using Tibr.Application.Services.ProductServices;

namespace Tibr.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        // GET api/products?pageNumber=1&pageSize=10
        // User filtered + paginated products
        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] ProductFilterParams filterParams)
        {
            var result = await _productService.GetProductsWithFiltersAsync(filterParams);

            if (result.IsFailure)
                return BadRequest(result.ErrorMessage);

            return Ok(result.Data);
        }

        // GET api/products/{id}
        // Single product full details
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(long id)
        {
            var result = await _productService.GetProductByIdAsync(id);

            if (result.IsFailure)
                return NotFound(result.ErrorMessage);

            return Ok(result.Data);
        }

        // GET api/products/{id}/stock
        // Check stock for a product
        [HttpGet("{id}/stock")]
        public async Task<IActionResult> CheckStock(long id)
        {
            var result = await _productService.CheckStockAsync(id);

            if (result.IsFailure)
                return NotFound(result.ErrorMessage);

            return Ok(new { productId = id, stock = result.Data });
        }



        // GET api/products/admin?pageNumber=1&pageSize=10
        // Admin sees ALL products including inactive
        [HttpGet("admin")]
        // [Authorize(Roles = "Admin")]  
        public async Task<IActionResult> GetAllProducts([FromQuery] PaginationParams paginationParams)
        {
            var result = await _productService.GetAllProductsAsync(paginationParams);

            if (result.IsFailure)
                return BadRequest(result.ErrorMessage);

            return Ok(result.Data);
        }

        // POST api/products
        // Admin creates product
        [HttpPost]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _productService.AddProductAsync(dto);

            if (result.IsFailure)
                return BadRequest(result.ErrorMessage);

            return CreatedAtAction(
                nameof(GetProductById),
                new { id = result.Data!.Id },
                result.Data);
        }

        // PUT api/products/{id}
        // Admin updates product
        [HttpPut("{id}")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(long id, [FromBody] UpdateProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _productService.UpdateProductAsync(id, dto);

            if (result.IsFailure)
                return NotFound(result.ErrorMessage);

            return Ok(result.Data);
        }

        // PATCH api/products/{id}/stock
        // Admin updates stock only — no need to send full product
        [HttpPatch("{id}/stock")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStock(long id, [FromBody] decimal newStock)
        {
            var result = await _productService.UpdateStockAsync(id, newStock);

            if (result.IsFailure)
                return NotFound(result.ErrorMessage);

            return NoContent(); 
        }

        // DELETE api/products/{id}
        // Admin soft deletes product
        [HttpDelete("{id}")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(long id)
        {
            var result = await _productService.DeleteProductAsync(id);

            if (result.IsFailure)
                return NotFound(result.ErrorMessage);

            return NoContent(); 
        }
    }
}
