using Tibr.Application.Dtos.ChatDtos;
using Tibr.Domain.Entities;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.AiChatServices
{
    public interface IChatOrderProposalService
    {
        Task<Result<(OrderProposalDto Proposal, string Reply)>> BuildAsync(
            long userId, long conversationId, string action, string asset, string scope,
            decimal? amountGrams, decimal? amountEgp);

        Task<ChatOrderProposal?> GetPendingAsync(long conversationId);

        Task<Result<ChatResponseDto>> ConfirmAsync(long userId, long conversationId);

        Task<Result> CancelAsync(long conversationId);

        Task<Result> ExpireAsync(long conversationId);
    }
}
