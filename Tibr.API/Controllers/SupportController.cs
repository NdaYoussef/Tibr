using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos.SupportDtos;
using Tibr.Application.Services.SupportServices;

namespace Tibr.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupportController(ISupportService supportService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var supports = await supportService.GetAllSupportsAsync();
            if (!supports.IsSuccess)
                return BadRequest(supports.ErrorMessage);
            return Ok(supports);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var support = await supportService.GetSupportByIdAsync(id);
            if (!support.IsSuccess)
                return NotFound(support.ErrorMessage);
            return Ok(support);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSupportDto createSupportRequestDto)
        {
           
            var result = await supportService.AddSupportAsync(createSupportRequestDto);
            if (!result.IsSuccess)
                return BadRequest(result.ErrorMessage);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromQuery] UpdateSupportDto updateSupportRequestDto)
        {
           
            var result = await supportService.UpdateSupportAsync(id, updateSupportRequestDto);
            if (!result.IsSuccess)
                return BadRequest(result.ErrorMessage);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await supportService.DeleteSupportAsync(id);
            if (!result.IsSuccess)
                return BadRequest(result.ErrorMessage);
            return Ok(result);
        }
    }
}
