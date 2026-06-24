using Tibr.Application.Dtos.ChatDtos;

namespace Tibr.Application.Services.AiChatServices
{
    public enum ProposalResolution { Confirm, Cancel, Modify, Unrelated }

    public interface IProposalResolutionClassifier
    {
        Task<ProposalResolution> ClassifyAsync(string userMessage, OrderProposalDto pendingProposal);
    }
}
