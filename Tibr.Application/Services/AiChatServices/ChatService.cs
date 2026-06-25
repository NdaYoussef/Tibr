using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tibr.Application.Dtos;
using Tibr.Application.Dtos.ChatDtos;
using Tibr.Application.Services.AssetPriceServices;
using Tibr.Application.Services.InvestmentOrderServices;
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
        private readonly IInvestmentOrderService _investmentOrderService;
        private readonly ChatRoutingOptions _routingOptions;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            IAiProviderService aiProvider,
            IVectorStoreService vectorStore,
            IWalletService walletService,
            IAssetPriceService priceService,
            IGenericRepository<Trade, long> tradeRepo,
            IGenericRepository<ChatConversation, long> conversationRepo,
            IGenericRepository<ChatMessage, long> messageRepo,
            IChatOrderProposalService proposalService,
            IProposalResolutionClassifier resolutionClassifier,
            IInvestmentOrderService investmentOrderService,
            ChatRoutingOptions routingOptions,
            ILogger<ChatService> logger
        )
        {
            _classifier = new IntentClassifier(aiProvider);
            _router = new ChatRouter(
                aiProvider,
                vectorStore,
                walletService,
                priceService,
                tradeRepo,
                proposalService,
                investmentOrderService,
                new GoalParser(aiProvider)
            );
            _investmentOrderService = investmentOrderService;
            _conversationRepo = conversationRepo;
            _messageRepo = messageRepo;
            _proposalService = proposalService;
            _resolutionClassifier = resolutionClassifier;
            _routingOptions = routingOptions;
            _logger = logger;
        }

        public async Task<Result<ChatResponseDto>> SendMessageAsync(
            long userId,
            ChatRequestDto request
        )
        {
            var language = request.Language ?? "en";
            ChatConversation conversation = null!;

            if (request.ConversationId.HasValue)
            {
                conversation = (
                    await _conversationRepo.GetByIdAsync(request.ConversationId.Value)
                )!;
                if (conversation is null || conversation.UserId != userId)
                    return Result<ChatResponseDto>.Failure("Conversation not found.");
            }
            else
            {
                conversation = new ChatConversation
                {
                    UserId = userId,
                    Title =
                        request.Message.Length > 50
                            ? request.Message[..50] + "..."
                            : request.Message,
                };
                await _conversationRepo.AddAsync(conversation);
                await _conversationRepo.SaveChangesAsync();
            }

            var userMessage = new ChatMessage
            {
                ConversationId = conversation.Id,
                Role = ChatRole.User,
                Content = request.Message,
            };
            await _messageRepo.AddAsync(userMessage);

            // Check for pending proposal first
            var pending = await _proposalService.GetPendingAsync(conversation.Id);
            if (pending is not null && pending.ExpiresAt > DateTime.UtcNow)
            {
                var dto = System.Text.Json.JsonSerializer.Deserialize<OrderProposalDto>(
                    pending.ProposalJson
                );
                if (dto is not null)
                {
                    var resolution = await _resolutionClassifier.ClassifyAsync(
                        request.Message,
                        dto
                    );

                    switch (resolution)
                    {
                        case ProposalResolution.Confirm:
                            var confirmResult = await _proposalService.ConfirmAsync(
                                userId,
                                conversation.Id,
                                language
                            );
                            if (confirmResult.IsFailure)
                            {
                                var errMsg = new ChatMessage
                                {
                                    ConversationId = conversation.Id,
                                    Role = ChatRole.Assistant,
                                    Content =
                                        confirmResult.ErrorMessage ?? "Could not execute order.",
                                };
                                await _messageRepo.AddAsync(errMsg);
                                await _messageRepo.SaveChangesAsync();
                                return Result<ChatResponseDto>.Failure(confirmResult.ErrorMessage!);
                            }
                            var confirmMsg = new ChatMessage
                            {
                                ConversationId = conversation.Id,
                                Role = ChatRole.Assistant,
                                Content = confirmResult.Data!.Reply,
                            };
                            await _messageRepo.AddAsync(confirmMsg);
                            await _messageRepo.SaveChangesAsync();
                            return confirmResult;

                        case ProposalResolution.Cancel:
                            await _proposalService.CancelAsync(conversation.Id);
                            var cancelReply = SystemMessages.CancelReply(language);
                            var cancelMsg = new ChatMessage
                            {
                                ConversationId = conversation.Id,
                                Role = ChatRole.Assistant,
                                Content = cancelReply,
                            };
                            await _messageRepo.AddAsync(cancelMsg);
                            await _messageRepo.SaveChangesAsync();
                            return Result<ChatResponseDto>.Success(
                                new ChatResponseDto
                                {
                                    ConversationId = conversation.Id,
                                    Reply = cancelReply,
                                    Intent = "Agentic",
                                    Source = "system",
                                    Language = language,
                                }
                            );

                        case ProposalResolution.Modify:
                            await _proposalService.CancelAsync(conversation.Id);
                            break;

                        case ProposalResolution.Unrelated:
                            var reminder = SystemMessages.UnrelatedReminder(
                                language,
                                dto.Action,
                                dto.AmountGrams ?? 0,
                                dto.Asset
                            );
                            var (unrelatedReply, _, _, _) = await ClassifyAndRouteAsync(
                                request.Message,
                                userId,
                                conversation.Id,
                                requestLanguage: language
                            );
                            var combined = reminder + unrelatedReply;
                            var combinedMsg = new ChatMessage
                            {
                                ConversationId = conversation.Id,
                                Role = ChatRole.Assistant,
                                Content = combined,
                            };
                            await _messageRepo.AddAsync(combinedMsg);
                            await _messageRepo.SaveChangesAsync();
                            return Result<ChatResponseDto>.Success(
                                new ChatResponseDto
                                {
                                    ConversationId = conversation.Id,
                                    Reply = combined,
                                    Intent = "Agentic",
                                    Source = "system",
                                    Language = language,
                                }
                            );
                    }
                }
            }

            // Expire stale proposals
            if (pending is not null && pending.ExpiresAt <= DateTime.UtcNow)
            {
                await _proposalService.ExpireAsync(conversation.Id);
            }

            var (reply, intent, source, lang) = await ClassifyAndRouteAsync(
                request.Message,
                userId,
                conversation.Id,
                request.Intent,
                request.Language
            );

            var assistantMessage = new ChatMessage
            {
                ConversationId = conversation.Id,
                Role = ChatRole.Assistant,
                Content = reply,
            };
            await _messageRepo.AddAsync(assistantMessage);
            await _messageRepo.SaveChangesAsync();

            return Result<ChatResponseDto>.Success(
                new ChatResponseDto
                {
                    ConversationId = conversation.Id,
                    Reply = reply,
                    Intent = intent,
                    Source = source,
                    Language = lang,
                }
            );
        }

        private async Task<(
            string Reply,
            string Intent,
            string Source,
            string Language
        )> ClassifyAndRouteAsync(
            string message,
            long userId,
            long conversationId,
            string? overrideIntent = null,
            string? requestLanguage = null
        )
        {
            if (!string.IsNullOrWhiteSpace(overrideIntent))
            {
                var lang = requestLanguage ?? "en";
                var (reply, source) = overrideIntent.ToLowerInvariant() switch
                {
                    "faq" => await _router.HandleFaqAsync(message, lang),
                    "facts" => await _router.HandleFactsAsync(message, lang),
                    "price" => await _router.HandlePriceAsync(message, lang),
                    "portfolio_read" => await _router.HandlePortfolioReadAsync(
                        message,
                        userId,
                        lang
                    ),
                    "agentic" => await HandleAgenticAsync(message, userId, conversationId, lang),
                    "conditional_order" => await HandleConditionalOrderAsync(
                        message,
                        userId,
                        conversationId,
                        lang
                    ),
                    "planner" => await _router.HandlePlannerAsync(message, userId, lang),
                    _ => _router.HandleOutOfScope(lang),
                };
                return (reply, overrideIntent, source, lang);
            }

            var classification = await _classifier.ClassifyAsync(message);
            var classifiedLang = requestLanguage ?? classification.Language;

            var (classifiedReply, classifiedSource) = await RouteByConfidenceAsync(
                classification, message, userId, conversationId, classifiedLang);

            return (
                classifiedReply,
                classification.Intent.ToString(),
                classifiedSource,
                classifiedLang
            );
        }

        private async Task<(string Reply, string Source)> RouteByConfidenceAsync(
            ClassificationResult classification,
            string message,
            long userId,
            long conversationId,
            string language)
        {
            var intent = classification.Intent;
            var confidence = classification.Confidence;

            if (intent is Intent.Faq or Intent.Facts
                && confidence < _routingOptions.DirectHitConfidenceThreshold)
            {
                _logger.LogInformation(
                    "Dual-RAG triggered: intent={Intent}, confidence={Confidence:P}, threshold={Threshold:P}",
                    intent, confidence, _routingOptions.DirectHitConfidenceThreshold);

                return await _router.HandleDualRagAsync(
                    message, language, _routingOptions.DualRagTopK);
            }

            return intent switch
            {
                Intent.Faq => await _router.HandleFaqAsync(message, language),
                Intent.Facts => await _router.HandleFactsAsync(message, language),
                Intent.Price => await _router.HandlePriceAsync(message, language),
                Intent.PortfolioRead => await _router.HandlePortfolioReadAsync(
                    message, userId, language),
                Intent.Agentic => await HandleAgenticAsync(
                    message, userId, conversationId, language),
                Intent.ConditionalOrder => await HandleConditionalOrderAsync(
                    message, userId, conversationId, language),
                Intent.Planner => await _router.HandlePlannerAsync(message, userId, language),
                _ => _router.HandleOutOfScope(language),
            };
        }

        private async Task<(string Reply, string Source)> HandleConditionalOrderAsync(
            string message,
            long userId,
            long conversationId,
            string language
        )
        {
            var (reply, toolCall, source) = await _router.HandleConditionalOrderAsync(
                message,
                userId,
                language
            );

            if (toolCall is not null)
            {
                var tc = (ToolCall)toolCall;
                if (tc.FunctionName == "create_strategy_order")
                {
                    using var doc = JsonDocument.Parse(tc.Arguments);
                    var root = doc.RootElement;

                    var assetStr = root.GetProperty("asset").GetString()!;
                    var sideStr = root.GetProperty("side").GetString()!;
                    var operatorStr = root.GetProperty("operator").GetString()!;
                    var targetPrice = root.GetProperty("target_price_egp").GetDecimal();
                    var execTypeStr = root.GetProperty("execution_type").GetString()!;
                    var quantity = root.GetProperty("quantity_grams").GetDecimal();
                    var expiresInDays = root.TryGetProperty("expires_in_days", out var e)
                        ? e.GetInt32()
                        : 30;

                    var assetType = assetStr == "silver" ? AssetType.Silver : AssetType.Gold;
                    var orderType = sideStr == "buy" ? OrderType.Buy : OrderType.Sell;
                    var conditionOp =
                        operatorStr == "greater_than"
                            ? ConditionOperator.GreaterThan
                            : ConditionOperator.LessThan;
                    var executionType = execTypeStr switch
                    {
                        "auto_execute" => ExecutionType.AutoExecute,
                        "alert_and_execute" => ExecutionType.AlertAndExecute,
                        _ => ExecutionType.AlertOnly,
                    };

                    var dto = new CreateStrategyOrderDto
                    {
                        AssetType = assetType,
                        OrderType = orderType,
                        ExecutionType = executionType,
                        Quantity = quantity,
                        ExpiryDate = DateTime.UtcNow.AddDays(expiresInDays),
                        Conditions =
                        [
                            new OrderConditionDto
                            {
                                ConditionType = ConditionType.PriceTarget,
                                Operator = conditionOp,
                                TargetValue = targetPrice,
                            },
                        ],
                    };

                    var result = await _investmentOrderService.CreateStrategyOrderAsync(
                        userId,
                        dto
                    );

                    if (result.IsFailure)
                        return (
                            result.ErrorMessage ?? SystemMessages.ConditionalCreateFailed(language),
                            "system"
                        );

                    var opLabel = operatorStr == "greater_than" ? "rises above" : "drops below";
                    var execLabel = execTypeStr switch
                    {
                        "auto_execute" => "automatically executed",
                        "alert_and_execute" => "alerted and automatically executed",
                        _ => "you'll be alerted",
                    };

                    return (
                        SystemMessages.StrategyCreated(
                            language,
                            sideStr,
                            quantity,
                            assetStr,
                            opLabel,
                            targetPrice,
                            expiresInDays,
                            execLabel
                        ),
                        "system"
                    );
                }
            }

            return (reply, source);
        }

        private async Task<(string Reply, string Source)> HandleAgenticAsync(
            string message,
            long userId,
            long conversationId,
            string language
        )
        {
            var (reply, toolCall, source) = await _router.HandleAgenticAsync(
                message,
                userId,
                conversationId,
                language
            );

            if (toolCall is not null)
            {
                var tc = (Tibr.Application.Services.AiChatServices.ToolCall)toolCall;
                if (tc.FunctionName == "propose_order")
                {
                    var (action, asset, scope, amountGrams, amountEgp) =
                        Tibr.Application.Services.AiChatServices.Tools.OrderBuilderTool.ParseArgs(
                            tc.Arguments
                        );

                    var result = await _proposalService.BuildAsync(
                        userId,
                        conversationId,
                        action,
                        asset,
                        scope,
                        amountGrams,
                        amountEgp,
                        language
                    );

                    if (result.IsFailure)
                    {
                        return (
                            result.ErrorMessage ?? SystemMessages.AgenticProposalFailed(language),
                            "system"
                        );
                    }

                    return (result.Data.Reply, "system");
                }
            }

            return (reply, source);
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

            var dtos = conversations
                .Select(c =>
                {
                    var msgs = allMessages.GetValueOrDefault(c.Id, []);
                    return new ConversationSummaryDto
                    {
                        Id = c.Id,
                        Title = c.Title,
                        MessageCount = msgs.Count,
                        LastMessage =
                            msgs.Count > 0
                                ? msgs.OrderByDescending(m => m.CreatedAt).First().Content
                                : null,
                        UpdatedAt = msgs.Count > 0 ? msgs.Max(m => m.CreatedAt) : c.CreatedAt,
                    };
                })
                .OrderByDescending(d => d.UpdatedAt)
                .ToList();

            return Result<List<ConversationSummaryDto>>.Success(dtos);
        }

        public async Task<Result<ConversationDetailDto>> GetConversationAsync(
            long userId,
            long conversationId
        )
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
                    CreatedAt = m.CreatedAt,
                })
                .ToList();

            var dto = new ConversationDetailDto
            {
                Id = conversation.Id,
                Title = conversation.Title,
                Messages = messages,
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
