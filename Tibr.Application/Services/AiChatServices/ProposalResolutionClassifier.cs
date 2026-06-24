namespace Tibr.Application.Services.AiChatServices
{
    public class ProposalResolutionClassifier : IProposalResolutionClassifier
    {
        private readonly IAiProviderService _aiProvider;

        public ProposalResolutionClassifier(IAiProviderService aiProvider)
        {
            _aiProvider = aiProvider;
        }

        public async Task<ProposalResolution> ClassifyAsync(string userMessage, Dtos.ChatDtos.OrderProposalDto pendingProposal)
        {
            var systemPrompt = $"""
                You are a confirmation classifier for Tibr, a gold investment app.
                A pending order proposal exists. Determine if the user is confirming, canceling, modifying, or saying something unrelated.

                Pending proposal:
                - Action: {pendingProposal.Action}
                - Asset: {pendingProposal.Asset}
                - Amount: {pendingProposal.AmountGrams?.ToString("F4") ?? pendingProposal.AmountEgp?.ToString("N2") + " EGP"}
                - Estimated total: {pendingProposal.EstimatedTotalEgp:N2} EGP

                Reply with exactly one word:
                - "confirm" — user explicitly agrees or says "yes", "do it", "place it", "go ahead"
                - "cancel" — user says "no", "cancel", "stop", "forget it", "never mind"
                - "modify" — user changes quantity, asset, or action (e.g., "buy 5g instead", "sell silver")
                - "unrelated" — user asks something new or changes topic entirely

                Rules:
                - If uncertain, return "unrelated"
                - Only return the single word, no punctuation, no explanation
                """;

            var history = new List<Message> { new("user", userMessage) };
            var response = await _aiProvider.ChatAsync(systemPrompt, history);
            var raw = (response.Content ?? "unrelated").Trim().ToLowerInvariant();

            return raw switch
            {
                "confirm" => ProposalResolution.Confirm,
                "cancel" => ProposalResolution.Cancel,
                "modify" => ProposalResolution.Modify,
                _ => ProposalResolution.Unrelated
            };
        }
    }
}
