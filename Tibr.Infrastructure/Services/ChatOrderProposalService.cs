using Microsoft.EntityFrameworkCore;
using Tibr.Application.Dtos;
using Tibr.Application.Dtos.ChatDtos;
using Tibr.Application.Services.AiChatServices;
using Tibr.Application.Services.AssetPriceServices;
using Tibr.Application.Services.TradeServices;
using Tibr.Application.Services.WalletServices;
using Tibr.Domain.Entities;
using Tibr.Domain.Enums;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;

namespace Tibr.Infrastructure.Services
{
    public class ChatOrderProposalService : IChatOrderProposalService
    {
        private readonly IGenericRepository<ChatOrderProposal, long> _proposalRepo;
        private readonly IGenericRepository<ChatConversation, long> _conversationRepo;
        private readonly IGenericRepository<ChatMessage, long> _messageRepo;
        private readonly IWalletService _walletService;
        private readonly IAssetPriceService _priceService;
        private readonly ITradeService _tradeService;
        private readonly IAiProviderService _aiProvider;
        private readonly IGenericRepository<Trade, long> _tradeRepo;
        private static readonly TimeSpan ProposalLifetime = TimeSpan.FromMinutes(10);

        public ChatOrderProposalService(
            IGenericRepository<ChatOrderProposal, long> proposalRepo,
            IGenericRepository<ChatConversation, long> conversationRepo,
            IGenericRepository<ChatMessage, long> messageRepo,
            IWalletService walletService,
            IAssetPriceService priceService,
            ITradeService tradeService,
            IAiProviderService aiProvider,
            IGenericRepository<Trade, long> tradeRepo)
        {
            _proposalRepo = proposalRepo;
            _conversationRepo = conversationRepo;
            _messageRepo = messageRepo;
            _walletService = walletService;
            _priceService = priceService;
            _tradeService = tradeService;
            _aiProvider = aiProvider;
            _tradeRepo = tradeRepo;
        }

        public async Task<Result<(OrderProposalDto Proposal, string Reply)>> BuildAsync(
            long userId, long conversationId, string action, string asset, string scope,
            decimal? amountGrams, decimal? amountEgp)
        {
            var isSilver = string.Equals(asset, "silver", StringComparison.OrdinalIgnoreCase);
            var isBuy = string.Equals(action, "buy", StringComparison.OrdinalIgnoreCase);
            var assetType = isSilver ? AssetType.Silver : AssetType.Gold;

            var priceResult = await _priceService.GetCurrentPriceAsync(assetType);
            if (priceResult.IsFailure || priceResult.Data is null)
                return Result<(OrderProposalDto, string)>.Failure("Unable to fetch current price.");

            var price = priceResult.Data;
            var quotedPrice = isBuy ? price.SellPrice : price.BuyPrice;
            decimal resolvedGrams;

            var isSpecific = string.Equals(scope, "specific_amount", StringComparison.OrdinalIgnoreCase);
            var isAllHoldings = string.Equals(scope, "all_holdings", StringComparison.OrdinalIgnoreCase);

            if (isSpecific)
            {
                if (amountGrams.HasValue)
                    resolvedGrams = amountGrams.Value;
                else if (amountEgp.HasValue)
                    resolvedGrams = amountEgp.Value / quotedPrice;
                else
                    return Result<(OrderProposalDto, string)>.Failure("No amount specified.");
            }
            else
            {
                var balanceResult = await _walletService.GetBalancesAsync(userId);
                if (balanceResult.IsFailure)
                    return Result<(OrderProposalDto, string)>.Failure("Unable to fetch wallet balances.");

                if (isAllHoldings)
                {
                    var walletType = isSilver ? WalletType.Silver : WalletType.Gold;
                    resolvedGrams = balanceResult.Data!
                        .FirstOrDefault(b => b.WalletType == walletType)
                        ?.AvailableBalance ?? 0;
                }
                else
                {
                    var trades = await _tradeRepo.GetAll(t => t.UserId == userId && t.AssetType == assetType).ToListAsync();
                    var profitableQty = trades
                        .Where(t => t.Side == TradeSide.Buy && quotedPrice > t.ExecutedPrice)
                        .Sum(t => t.Quantity);
                    resolvedGrams = profitableQty;
                }
            }

            if (resolvedGrams <= 0)
                return Result<(OrderProposalDto, string)>.Failure(
                    isBuy ? "Insufficient funds for purchase." : "No holdings available to sell.");

            var estimatedTotal = resolvedGrams * quotedPrice;

            var proposal = new OrderProposalDto
            {
                Action = action,
                Asset = asset,
                Scope = scope,
                AmountGrams = resolvedGrams,
                AmountEgp = amountEgp,
                QuotedPricePerGram = quotedPrice,
                EstimatedTotalEgp = estimatedTotal,
                QuotedAt = DateTime.UtcNow
            };

            var entity = new ChatOrderProposal
            {
                ConversationId = conversationId,
                ProposalJson = System.Text.Json.JsonSerializer.Serialize(proposal),
                Status = ProposalStatus.Pending,
                ExpiresAt = DateTime.UtcNow.Add(ProposalLifetime)
            };

            await _proposalRepo.AddAsync(entity);
            await _proposalRepo.SaveChangesAsync();

            var reply = $"Proposed {action} of {resolvedGrams:F4}g {asset} at {quotedPrice:N2} EGP/g "
                + $"(total: {estimatedTotal:N2} EGP). Reply 'confirm' or 'cancel'.";
            return Result<(OrderProposalDto, string)>.Success((proposal, reply));
        }

        public async Task<ChatOrderProposal?> GetPendingAsync(long conversationId)
        {
            var proposals = await _proposalRepo.GetAllAsync();
            return proposals
                .Where(p => p.ConversationId == conversationId && p.Status == ProposalStatus.Pending)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault();
        }

        public async Task<Result<ChatResponseDto>> ConfirmAsync(long userId, long conversationId)
        {
            var proposal = await GetPendingAsync(conversationId);
            if (proposal is null)
                return Result<ChatResponseDto>.Failure("No pending proposal found.");

            if (proposal.ExpiresAt < DateTime.UtcNow)
            {
                proposal.Status = ProposalStatus.Expired;
                await _proposalRepo.UpdateAsync(proposal);
                await _proposalRepo.SaveChangesAsync();
                return Result<ChatResponseDto>.Failure("Proposal has expired. Please start a new order.");
            }

            var dto = System.Text.Json.JsonSerializer.Deserialize<OrderProposalDto>(proposal.ProposalJson);
            if (dto is null)
                return Result<ChatResponseDto>.Failure("Invalid proposal data.");

            var assetType = dto.Asset == "silver" ? AssetType.Silver : AssetType.Gold;

            if (dto.Action == "buy")
            {
                var buyResult = await _tradeService.ExecuteDirectBuyAsync(userId,
                    new DirectBuyDto { AssetType = assetType, Quantity = dto.AmountGrams ?? 0 });
                if (buyResult.IsFailure)
                    return Result<ChatResponseDto>.Failure(buyResult.ErrorMessage!);

                proposal.Status = ProposalStatus.Confirmed;
                await _proposalRepo.UpdateAsync(proposal);
                await _proposalRepo.SaveChangesAsync();

                var receipt = $"Order executed! Bought {dto.AmountGrams:F4}g {dto.Asset} "
                    + $"at {dto.QuotedPricePerGram:N2} EGP/g.";
                return Result<ChatResponseDto>.Success(new ChatResponseDto
                {
                    ConversationId = conversationId,
                    Reply = receipt,
                    Intent = "Agentic"
                });
            }
            else
            {
                var sellResult = await _tradeService.ExecuteDirectSellAsync(userId,
                    new DirectSellDto { AssetType = assetType, Quantity = dto.AmountGrams ?? 0 });
                if (sellResult.IsFailure)
                    return Result<ChatResponseDto>.Failure(sellResult.ErrorMessage!);

                proposal.Status = ProposalStatus.Confirmed;
                await _proposalRepo.UpdateAsync(proposal);
                await _proposalRepo.SaveChangesAsync();

                var receipt = $"Order executed! Sold {dto.AmountGrams:F4}g {dto.Asset} "
                    + $"at {dto.QuotedPricePerGram:N2} EGP/g.";
                return Result<ChatResponseDto>.Success(new ChatResponseDto
                {
                    ConversationId = conversationId,
                    Reply = receipt,
                    Intent = "Agentic"
                });
            }
        }

        public async Task<Result> CancelAsync(long conversationId)
        {
            var proposal = await GetPendingAsync(conversationId);
            if (proposal is null)
                return Result.Success();

            proposal.Status = ProposalStatus.Cancelled;
            await _proposalRepo.UpdateAsync(proposal);
            await _proposalRepo.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> ExpireAsync(long conversationId)
        {
            var proposal = await GetPendingAsync(conversationId);
            if (proposal is null)
                return Result.Success();

            proposal.Status = ProposalStatus.Expired;
            await _proposalRepo.UpdateAsync(proposal);
            await _proposalRepo.SaveChangesAsync();
            return Result.Success();
        }
    }
}
