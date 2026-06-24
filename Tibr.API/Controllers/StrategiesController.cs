using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos;
using Tibr.Application.Services.InvestmentOrderServices;
using Tibr.Domain.Enums;

namespace Tibr.API.Controllers
{
    [ApiController]
    [Route("api/strategies")]
    [Authorize]
    public class StrategiesController : ControllerBase
    {
        private readonly IInvestmentOrderService _orderService;

        public StrategiesController(IInvestmentOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        public async Task<ActionResult<InvestmentOrderDto>> Create([FromBody] CreateStrategyFormDto dto)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            AssetType? assetType = dto.Asset?.ToLower() switch
            {
                "gold" => AssetType.Gold,
                "silver" => AssetType.Silver,
                _ => null
            };

            OrderType? orderType = dto.Side?.ToLower() switch
            {
                "buy" => OrderType.Buy,
                "sell" => OrderType.Sell,
                _ => null
            };

            ConditionOperator? conditionOp = dto.Operator?.ToLower() switch
            {
                "greater_than" or "gt" => ConditionOperator.GreaterThan,
                "greater_than_or_equal" or "gte" => ConditionOperator.GreaterThanOrEqual,
                "less_than" or "lt" => ConditionOperator.LessThan,
                "less_than_or_equal" or "lte" => ConditionOperator.LessThanOrEqual,
                "equal" or "eq" => ConditionOperator.Equal,
                _ => null
            };

            ExecutionType? execType = dto.ExecutionType?.ToLower() switch
            {
                "alert_only" or "alert" => ExecutionType.AlertOnly,
                "auto_execute" or "auto" => ExecutionType.AutoExecute,
                "alert_and_execute" => ExecutionType.AlertAndExecute,
                _ => null
            };

            if (assetType is null)
                return BadRequest("Invalid asset. Allowed: gold, silver.");

            if (orderType is null)
                return BadRequest("Invalid side. Allowed: buy, sell.");

            if (conditionOp is null)
                return BadRequest("Invalid operator. Allowed: less_than, less_than_or_equal, greater_than, greater_than_or_equal, equal.");

            if (execType is null)
                return BadRequest("Invalid executionType. Allowed: alert_only, auto_execute, alert_and_execute.");

            var serviceDto = new CreateStrategyOrderDto
            {
                AssetType = assetType.Value,
                OrderType = orderType.Value,
                ExecutionType = execType.Value,
                Quantity = dto.Quantity,
                ExpiryDate = dto.ExpiresAt,
                Conditions =
                [
                    new OrderConditionDto
                    {
                        ConditionType = ConditionType.PriceTarget,
                        Operator = conditionOp.Value,
                        TargetValue = dto.TargetPriceEgp
                    }
                ]
            };

            var result = await _orderService.CreateStrategyOrderAsync(userId.Value, serviceDto);
            if (result.IsFailure)
                return BadRequest(result.ErrorMessage);
            return Ok(result.Data);
        }

        private long? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim is null || !long.TryParse(claim.Value, out var userId))
                return null;
            return userId;
        }
    }
}
