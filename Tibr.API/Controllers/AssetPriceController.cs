using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos;
using Tibr.Application.Services.AssetPriceServices;
using Tibr.Domain.Enums;

namespace Tibr.API.Controllers
{
    [ApiController]
    [Route("api/asset-price")]
    public class AssetPriceController : ControllerBase
    {
        private readonly IAssetPriceService _assetPriceService;

        public AssetPriceController(IAssetPriceService assetPriceService)
        {
            _assetPriceService = assetPriceService;
        }

        [HttpGet("current")]
        [AllowAnonymous]
        public async Task<ActionResult<CurrentPricesDto>> GetCurrentPrices()
        {
            var goldResult = await _assetPriceService.GetCurrentPriceAsync(AssetType.Gold);
            var silverResult = await _assetPriceService.GetCurrentPriceAsync(AssetType.Silver);

            return Ok(new CurrentPricesDto
            {
                Gold = goldResult.Data,
                Silver = silverResult.Data
            });
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> RecordPrice([FromBody] RecordPriceDto dto)
        {
            var result = await _assetPriceService.RecordPriceAsync(
                dto.AssetType, dto.BuyPrice, dto.SellPrice, dto.Source);

            if (result.IsFailure)
                return BadRequest(result.ErrorMessage);
            return Ok(result);
        }
    }

    public class CurrentPricesDto
    {
        public AssetPriceDto? Gold { get; set; }
        public AssetPriceDto? Silver { get; set; }
    }

    public class RecordPriceDto
    {
        public AssetType AssetType { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public string Source { get; set; } = string.Empty;
    }
}
