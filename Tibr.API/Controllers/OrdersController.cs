using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos;
using Tibr.Application.Services.OrderServices;

namespace Tibr.API.Controllers
{
    /// <summary>
    /// Manages order operations including creation, retrieval, updates, and deletion.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>Retrieves all orders.</summary>
        /// <returns>A list of all orders.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetAll()
        {
            var result = await _orderService.GetAllAsync();
            return Ok(result.Data);
        }

        /// <summary>Retrieves a specific order by its ID.</summary>
        /// <param name="id">The order ID.</param>
        /// <returns>The order if found; otherwise 404.</returns>
        [HttpGet("{id:long}")]
        public async Task<ActionResult<OrderDto>> GetById(long id)
        {
            var result = await _orderService.GetByIdAsync(id);
            if (result.IsFailure)
                return NotFound(result.ErrorMessage);
            return Ok(result.Data);
        }

        /// <summary>Retrieves all orders for a specific user.</summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>A list of orders belonging to the user.</returns>
        [HttpGet("user/{userId:long}")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetByUserId(long userId)
        {
            var result = await _orderService.GetByUserIdAsync(userId);
            return Ok(result.Data);
        }

        /// <summary>Creates a new order.</summary>
        /// <param name="dto">The order creation details.</param>
        /// <returns>The created order with 201 status.</returns>
        [HttpPost]
        public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderDto dto)
        {
            var result = await _orderService.CreateAsync(dto);
            if (result.IsFailure)
                return BadRequest(result.ErrorMessage);
            return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
        }

        /// <summary>Updates an existing order's status fields.</summary>
        /// <param name="id">The order ID.</param>
        /// <param name="dto">The updated status values.</param>
        /// <returns>The updated order if found; otherwise 404.</returns>
        [HttpPut("{id:long}")]
        public async Task<ActionResult<OrderDto>> Update(long id, [FromBody] UpdateOrderDto dto)
        {
            var result = await _orderService.UpdateAsync(id, dto);
            if (result.IsFailure)
                return NotFound(result.ErrorMessage);
            return Ok(result.Data);
        }

        /// <summary>Soft-deletes an order.</summary>
        /// <param name="id">The order ID.</param>
        /// <returns>204 No Content if successful; 404 if not found.</returns>
        [HttpDelete("{id:long}")]
        public async Task<ActionResult> Delete(long id)
        {
            var result = await _orderService.DeleteAsync(id);
            if (result.IsFailure)
                return NotFound(result.ErrorMessage);
            return NoContent();
        }
    }
}
