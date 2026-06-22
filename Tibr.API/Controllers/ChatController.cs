using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos.ChatDtos;
using Tibr.Application.Services.AiChatServices;

namespace Tibr.API.Controllers
{
    [Route("api/chat")]
    [ApiController]
    [Authorize]
    public class ChatController(IChatService chatService) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<ChatResponseDto>> SendMessage(ChatRequestDto request)
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            var result = await chatService.SendMessageAsync(userId.Value, request);
            if (!result.IsSuccess)
                return BadRequest(result.ErrorMessage);

            return Ok(result.Data);
        }

        [HttpGet("conversations")]
        public async Task<ActionResult<List<ConversationSummaryDto>>> GetConversations()
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            var result = await chatService.GetConversationsAsync(userId.Value);
            return Ok(result.Data);
        }

        [HttpGet("conversations/{id:long}")]
        public async Task<ActionResult<ConversationDetailDto>> GetConversation(long id)
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            var result = await chatService.GetConversationAsync(userId.Value, id);
            if (!result.IsSuccess)
                return NotFound(result.ErrorMessage);

            return Ok(result.Data);
        }

        [HttpDelete("conversations/{id:long}")]
        public async Task<IActionResult> DeleteConversation(long id)
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            var result = await chatService.DeleteConversationAsync(userId.Value, id);
            if (!result.IsSuccess)
                return NotFound(result.ErrorMessage);

            return Ok();
        }

        private long? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim is null || !long.TryParse(claim.Value, out var userId)
                ? null
                : userId;
        }
    }
}
