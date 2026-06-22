using Tibr.Application.Dtos.ChatDtos;
using Tibr.Application.Services.AssetPriceServices;
using Tibr.Application.Services.WalletServices;
using Tibr.Domain.Entities;
using Tibr.Domain.Enums;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.AiChatServices
{
    public class ChatService : IChatService
    {
        private readonly IntentClassifier _classifier;
        private readonly ChatRouter _router;
        private readonly IGenericRepository<ChatConversation, long> _conversationRepo;
        private readonly IGenericRepository<ChatMessage, long> _messageRepo;
        private readonly IChatOrderProposalService _proposalService;
        private readonly IProposalResolutionClassifier _resolutionClassifier;
        private const int ContextWindowSize = 10;

        public ChatService(
            IAiProviderService aiProvider,
            IVectorStoreService vectorStore,
            IWalletService walletService,
            IAssetPriceService priceService,
            IGenericRepository<Trade, long> tradeRepo,
            IGenericRepository<ChatConversation, long> conversationRepo,
            IGenericRepository<ChatMessage, long> messageRepo,
            IChatOrderProposalService proposalService,
            IProposalResolutionClassifier resolutionClassifier)
        {
            _classifier = new IntentClassifier(aiProvider);
            _router = new ChatRouter(aiProvider, vectorStore, walletService, priceService, tradeRepo, proposalService, new GoalParser(aiProvider));
            _conversationRepo = conversationRepo;
            _messageRepo = messageRepo;
            _proposalService = proposalService;
            _resolutionClassifier = resolutionClassifier;
        }

        public async Task<Result<ChatResponseDto>> SendMessageAsync(long userId, ChatRequestDto request)
        {
            ChatConversation conversation = null!;

            if (request.ConversationId.HasValue)
            {
                conversation = (await _conversationRepo.GetByIdAsync(request.ConversationId.Value))!;
                if (conversation is null || conversation.UserId != userId)
                    return Result<ChatResponseDto>.Failure("Conversation not found.");
            }
            else
            {
                conversation = new ChatConversation
                {
                    UserId = userId,
                    Title = request.Message.Length > 50
                        ? request.Message[..50] + "..."
                        : request.Message
                };
                await _conversationRepo.AddAsync(conversation);
                await _conversationRepo.SaveChangesAsync();
            }

            var userMessage = new ChatMessage
            {
                ConversationId = conversation.Id,
                Role = ChatRole.User,
                Content = request.Message
            };
            await _messageRepo.AddAsync(userMessage);

            // Check for pending proposal first
            var pending = await _proposalService.GetPendingAsync(conversation.Id);
            if (pending is not null && pending.ExpiresAt > DateTime.UtcNow)
            {
                var dto = System.Text.Json.JsonSerializer.Deserialize<OrderProposalDto>(pending.ProposalJson);
                if (dto is not null)
                {
                    var resolution = await _resolutionClassifier.ClassifyAsync(request.Message, dto);

                    switch (resolution)
                    {
                        case ProposalResolution.Confirm:
                            var confirmResult = await _proposalService.ConfirmAsync(userId, conversation.Id);
                            if (confirmResult.IsFailure)
                            {
                                var errMsg = new ChatMessage
                                {
                                    ConversationId = conversation.Id,
                                    Role = ChatRole.Assistant,
                                    Content = confirmResult.ErrorMessage ?? "Could not execute order."
                                };
                                await _messageRepo.AddAsync(errMsg);
                                await _messageRepo.SaveChangesAsync();
                                return Result<ChatResponseDto>.Failure(confirmResult.ErrorMessage!);
                            }
                            var confirmMsg = new ChatMessage
                            {
                                ConversationId = conversation.Id,
                                Role = ChatRole.Assistant,
                                Content = confirmResult.Data!.Reply
                            };
                            await _messageRepo.AddAsync(confirmMsg);
                            await _messageRepo.SaveChangesAsync();
                            return confirmResult;

                        case ProposalResolution.Cancel:
                            await _proposalService.CancelAsync(conversation.Id);
                            var cancelReply = "Order cancelled. Anything else I can help with?";
                            var cancelMsg = new ChatMessage
                            {
                                ConversationId = conversation.Id,
                                Role = ChatRole.Assistant,
                                Content = cancelReply
                            };
                            await _messageRepo.AddAsync(cancelMsg);
                            await _messageRepo.SaveChangesAsync();
                            return Result<ChatResponseDto>.Success(new ChatResponseDto
                            {
                                ConversationId = conversation.Id,
                                Reply = cancelReply,
                                Intent = "Agentic"
                            });

                        case ProposalResolution.Modify:
                            await _proposalService.CancelAsync(conversation.Id);
                            break;

                        case ProposalResolution.Unrelated:
                            var reminder = $"You have a pending order to {dto.Action} {dto.AmountGrams:F4}g {dto.Asset}. "
                                + "Reply 'confirm' to proceed or 'cancel' to discard it.\n\n";
                            var unrelatedResult = await ClassifyAndRouteAsync(request.Message, userId, conversation.Id);
                            var combined = reminder + unrelatedResult;
                            var combinedMsg = new ChatMessage
                            {
                                ConversationId = conversation.Id,
                                Role = ChatRole.Assistant,
                                Content = combined
                            };
                            await _messageRepo.AddAsync(combinedMsg);
                            await _messageRepo.SaveChangesAsync();
                            return Result<ChatResponseDto>.Success(new ChatResponseDto
                            {
                                ConversationId = conversation.Id,
                                Reply = combined,
                                Intent = "Agentic"
                            });
                    }
                }
            }

            // Expire stale proposals
            if (pending is not null && pending.ExpiresAt <= DateTime.UtcNow)
            {
                await _proposalService.ExpireAsync(conversation.Id);
            }

            var (reply, intent) = await ClassifyAndRouteAsync(request.Message, userId, conversation.Id, request.Intent);

            var assistantMessage = new ChatMessage
            {
                ConversationId = conversation.Id,
                Role = ChatRole.Assistant,
                Content = reply
            };
            await _messageRepo.AddAsync(assistantMessage);
            await _messageRepo.SaveChangesAsync();

            return Result<ChatResponseDto>.Success(new ChatResponseDto
            {
                ConversationId = conversation.Id,
                Reply = reply,
                Intent = intent
            });
        }

        private async Task<(string Reply, string Intent)> ClassifyAndRouteAsync(string message, long userId, long conversationId, string? overrideIntent = null)
        {
            if (!string.IsNullOrWhiteSpace(overrideIntent))
            {
                var overrideReply = overrideIntent.ToLowerInvariant() switch
                {
                    "faq" => await _router.HandleFaqAsync(message),
                    "facts" => await _router.HandleFactsAsync(message),
                    "price" => await _router.HandlePriceAsync(message),
                    "portfolio_read" => await _router.HandlePortfolioReadAsync(message, userId),
                    "agentic" => await HandleAgenticAsync(message, userId, conversationId),
                    "planner" => await _router.HandlePlannerAsync(message, userId),
                    _ => _router.HandleOutOfScope()
                };
                return (overrideReply, overrideIntent);
            }

            var classification = await _classifier.ClassifyAsync(message);

            var reply = classification.Intent switch
            {
                Intent.Faq => await _router.HandleFaqAsync(message),
                Intent.Facts => await _router.HandleFactsAsync(message),
                Intent.Price => await _router.HandlePriceAsync(message),
                Intent.PortfolioRead => await _router.HandlePortfolioReadAsync(message, userId),
                Intent.Agentic => await HandleAgenticAsync(message, userId, conversationId),
                Intent.Planner => await _router.HandlePlannerAsync(message, userId),
                _ => _router.HandleOutOfScope()
            };

            return (reply, classification.Intent.ToString());
        }

        private async Task<string> HandleAgenticAsync(string message, long userId, long conversationId)
        {
            var (reply, toolCall) = await _router.HandleAgenticAsync(message, userId, conversationId);

            if (toolCall is not null)
            {
                var tc = (Tibr.Application.Services.AiChatServices.ToolCall)toolCall;
                if (tc.FunctionName == "propose_order")
                {
                    var (action, asset, scope, amountGrams, amountEgp) =
                        Tibr.Application.Services.AiChatServices.Tools.OrderBuilderTool.ParseArgs(tc.Arguments);

                    var result = await _proposalService.BuildAsync(userId, conversationId,
                        action, asset, scope, amountGrams, amountEgp);

                    if (result.IsFailure)
                    {
                        return result.ErrorMessage ?? "Could not create order proposal.";
                    }

                    return result.Data.Reply;
                }
            }

            return reply;
        }

        public async Task<Result<List<ConversationSummaryDto>>> GetConversationsAsync(long userId)
        {
            var conversations = (await _conversationRepo.GetAllAsync())
                .Where(c => c.UserId == userId)
                .ToList();

            var conversationIds = conversations.Select(c => c.Id).ToList();
            var allMessages = (await _messageRepo.GetAllAsync())
                .Where(m => conversationIds.Contains(m.ConversationId))
                .GroupBy(m => m.ConversationId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var dtos = conversations.Select(c =>
            {
                var msgs = allMessages.GetValueOrDefault(c.Id, []);
                return new ConversationSummaryDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    MessageCount = msgs.Count,
                    LastMessage = msgs.Count > 0
                        ? msgs.OrderByDescending(m => m.CreatedAt).First().Content
                        : null,
                    UpdatedAt = msgs.Count > 0
                        ? msgs.Max(m => m.CreatedAt)
                        : c.CreatedAt
                };
            })
            .OrderByDescending(d => d.UpdatedAt)
            .ToList();

            return Result<List<ConversationSummaryDto>>.Success(dtos);
        }

        public async Task<Result<ConversationDetailDto>> GetConversationAsync(long userId, long conversationId)
        {
            var conversation = await _conversationRepo.GetByIdAsync(conversationId);
            if (conversation is null || conversation.UserId != userId)
                return Result<ConversationDetailDto>.Failure("Conversation not found.");

            var messages = (await _messageRepo.GetAllAsync())
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    Role = m.Role.ToString(),
                    Content = m.Content,
                    CreatedAt = m.CreatedAt
                })
                .ToList();

            var dto = new ConversationDetailDto
            {
                Id = conversation.Id,
                Title = conversation.Title,
                Messages = messages
            };

            return Result<ConversationDetailDto>.Success(dto);
        }

        public async Task<Result> DeleteConversationAsync(long userId, long conversationId)
        {
            var conversation = await _conversationRepo.GetByIdAsync(conversationId);
            if (conversation is null || conversation.UserId != userId)
                return Result.Failure("Conversation not found.");

            await _conversationRepo.DeleteAsync(conversation);
            await _conversationRepo.SaveChangesAsync();
            return Result.Success();
        }
    }
}
