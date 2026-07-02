using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Tibr.Application.Dtos;
using Tibr.Application.Dtos.ChatDtos;
using Tibr.Application.Services.AssetPriceServices;
using Tibr.Application.Services.InvestmentOrderServices;
using Tibr.Application.Services.WalletServices;
using Tibr.Application.Services.AiChatServices.Tools;
using Tibr.Application.Services.PlanServices;
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
        private readonly IPlanService _planService;
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
            IPlanService planService,
            ChatRoutingOptions routingOptions,
            ILogger<ChatService> logger,
            ILoggerFactory loggerFactory
        )
        {
            _planService = planService;
            _classifier = new IntentClassifier(aiProvider, loggerFactory.CreateLogger<IntentClassifier>());
            _router = new ChatRouter(
                aiProvider,
                vectorStore,
                walletService,
                priceService,
                tradeRepo,
                proposalService,
                investmentOrderService,
                new GoalParser(aiProvider),
                _planService
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
            _logger.LogInformation(
                "SendMessage: userId={UserId}, message={Message}, conversationId={ConvId}, intent={Intent}, language={Lang}",
                userId, request.Message, request.ConversationId, request.Intent, request.Language);
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

            // Load prior messages for AI conversation context
            var priorMessages = (await _messageRepo.GetAllAsync())
                .Where(m => m.ConversationId == conversation.Id && m.Id != userMessage.Id)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new Message(
                    m.Role == ChatRole.User ? "user" : "assistant",
                    m.Content
                ))
                .ToList();

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
                            var sanitizedReply = SanitizeReply(confirmResult.Data!.Reply);
                            var confirmMsg = new ChatMessage
                            {
                                ConversationId = conversation.Id,
                                Role = ChatRole.Assistant,
                                Content = sanitizedReply,
                            };
                            await _messageRepo.AddAsync(confirmMsg);
                            await _messageRepo.SaveChangesAsync();
                            confirmResult.Data.Reply = sanitizedReply;
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
                            var (unrelatedReply, _, _, _, _) = await ClassifyAndRouteAsync(
                                request.Message,
                                userId,
                                conversation.Id,
                                priorMessages,
                                requestLanguage: language
                            );
                            var combined = SanitizeReply(reminder + unrelatedReply);
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

            string reply, intent, source, lang;
            bool clarificationNeeded;

            // Bypass classifier when awaiting clarification from a previous turn
            if (conversation.PendingClarification is not null)
            {
                var pendingIntent = conversation.PendingClarification;
                conversation.PendingClarification = null;
                lang = request.Language ?? "en";

                if (pendingIntent == "planner")
                {
                    (reply, source, clarificationNeeded) = await _router.HandlePlannerAsync(
                        request.Message, userId, lang, priorMessages);
                    intent = "Planner";
                    if (clarificationNeeded)
                        conversation.PendingClarification = "planner";
                }
                else if (pendingIntent == "plan_update")
                {
                    (reply, source, clarificationNeeded) = await _router.HandlePlanUpdateAsync(
                        userId, lang);
                    intent = "PlanUpdate";
                }
                else
                {
                    (reply, intent, source, lang, clarificationNeeded) = await ClassifyAndRouteAsync(
                        request.Message, userId, conversation.Id, priorMessages,
                        request.Intent, request.Language);
                }
            }
            else
            {
                (reply, intent, source, lang, clarificationNeeded) = await ClassifyAndRouteAsync(
                    request.Message,
                    userId,
                    conversation.Id,
                    priorMessages,
                    request.Intent,
                    request.Language
                );

                if (clarificationNeeded && intent == "Planner")
                    conversation.PendingClarification = "planner";
            }

            reply = SanitizeReply(reply);

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
            string Language,
            bool ClarificationNeeded
        )> ClassifyAndRouteAsync(
            string message,
            long userId,
            long conversationId,
            List<Message> history,
            string? overrideIntent = null,
            string? requestLanguage = null
        )
        {
            if (!string.IsNullOrWhiteSpace(overrideIntent))
            {
                var lang = requestLanguage ?? "en";
                if (overrideIntent.ToLowerInvariant() == "planner")
                {
                    var (pReply, pSource, pClarification) = await _router.HandlePlannerAsync(
                        message, userId, lang, history);
                    return (pReply, overrideIntent, pSource, lang, pClarification);
                }

                if (overrideIntent.ToLowerInvariant() == "plan_update")
                {
                    var (uReply, uSource, uClarification) = await _router.HandlePlanUpdateAsync(
                        userId, lang);
                    return (uReply, overrideIntent, uSource, lang, uClarification);
                }

                var (reply, source) = overrideIntent.ToLowerInvariant() switch
                {
                    "faq" => await _router.HandleFaqAsync(message, lang, history),
                    "facts" => await _router.HandleFactsAsync(message, lang, history),
                    "price" => await _router.HandlePriceAsync(message, lang, history),
                    "portfolio_read" => await _router.HandlePortfolioReadAsync(
                        message, userId, lang, history),
                    "agentic" => await HandleAgenticAsync(message, userId, conversationId, lang, history),
                    "conditional_order" => await HandleConditionalOrderAsync(
                        message, userId, conversationId, lang, history),
                    _ => _router.HandleOutOfScope(lang),
                };
                return (reply, overrideIntent, source, lang, false);
            }

            var classification = await _classifier.ClassifyAsync(message);
            _logger.LogInformation(
                "Classification: intent={Intent}, confidence={Confidence:P}, reason={Reason}, language={Language}",
                classification.Intent, classification.Confidence, classification.Reason, classification.Language);

            if (classification.Reason == "AI_SERVICE_UNAVAILABLE")
            {
                return (SystemMessages.AiUnavailable(requestLanguage ?? "en"), "Error", "system", requestLanguage ?? "en", false);
            }

            var classifiedLang = classification.Language ?? requestLanguage ?? "en";

            var (classifiedReply, classifiedSource, clarificationNeeded) = await RouteByConfidenceAsync(
                classification, message, userId, conversationId, classifiedLang, history);

            return (
                classifiedReply,
                classification.Intent.ToString(),
                classifiedSource,
                classifiedLang,
                clarificationNeeded
            );
        }

        private async Task<(string Reply, string Source, bool ClarificationNeeded)> RouteByConfidenceAsync(
            ClassificationResult classification,
            string message,
            long userId,
            long conversationId,
            string language,
            List<Message> history)
        {
            var intent = classification.Intent;
            var confidence = classification.Confidence;

            if (intent is Intent.Faq or Intent.Facts
                && confidence < _routingOptions.DirectHitConfidenceThreshold)
            {
                _logger.LogInformation(
                    "Dual-RAG triggered: intent={Intent}, confidence={Confidence:P}, threshold={Threshold:P}",
                    intent, confidence, _routingOptions.DirectHitConfidenceThreshold);

                var (dualReply, dualSource) = await _router.HandleDualRagAsync(
                    message, language, _routingOptions.DualRagTopK, history);
                return (dualReply, dualSource, false);
            }

            return intent switch
            {
                Intent.Faq => To3(await _router.HandleFaqAsync(message, language, history)),
                Intent.Facts => To3(await _router.HandleFactsAsync(message, language, history)),
                Intent.Price => To3(await _router.HandlePriceAsync(message, language, history)),
                Intent.PortfolioRead => To3(await _router.HandlePortfolioReadAsync(
                    message, userId, language, history)),
                Intent.Agentic => To3(await HandleAgenticAsync(
                    message, userId, conversationId, language, history)),
                Intent.ConditionalOrder => To3(await HandleConditionalOrderAsync(
                    message, userId, conversationId, language, history)),
                Intent.Planner => await _router.HandlePlannerAsync(message, userId, language, history),
                Intent.PlanUpdate => await _router.HandlePlanUpdateAsync(userId, language),
                _ => To3(_router.HandleOutOfScope(language)),
            };
        }

        private static (string, string, bool) To3((string Reply, string Source) x)
            => (x.Reply, x.Source, false);

        private async Task<(string Reply, string Source)> HandleConditionalOrderAsync(
            string message,
            long userId,
            long conversationId,
            string language,
            List<Message> history
        )
        {
            var (reply, toolCall, source) = await _router.HandleConditionalOrderAsync(
                message,
                userId,
                language,
                history
            );

            if (toolCall is not null)
            {
                var tc = (ToolCall)toolCall;
                if (tc.FunctionName == "create_strategy_order")
                {
                    var strategyArgs = OrderBuilderTool.ParseStrategyArgs(tc.Arguments);

                    var assetType = strategyArgs.Asset == "silver" ? AssetType.Silver : AssetType.Gold;
                    var orderType = strategyArgs.Side == "buy" ? OrderType.Buy : OrderType.Sell;
                    var conditionOp =
                        strategyArgs.Operator == "greater_than"
                            ? ConditionOperator.GreaterThan
                            : ConditionOperator.LessThan;
                    var executionType = strategyArgs.ExecutionType switch
                    {
                        "auto_execute" => ExecutionType.AutoExecute,
                        "alert_and_execute" => ExecutionType.AlertAndExecute,
                        _ => ExecutionType.AlertOnly,
                    };

                    var quantity = strategyArgs.QuantityGrams ?? 0;
                    var maxAmountEgp = orderType == OrderType.Buy ? strategyArgs.MaxAmountEgp : null;

                    var dto = new CreateStrategyOrderDto
                    {
                        AssetType = assetType,
                        OrderType = orderType,
                        ExecutionType = executionType,
                        Quantity = quantity,
                        MaxAmountEgp = maxAmountEgp,
                        ExpiryDate = DateTime.UtcNow.AddDays(strategyArgs.ExpiresInDays),
                        Conditions =
                        [
                            new OrderConditionDto
                            {
                                ConditionType = ConditionType.PriceTarget,
                                Operator = conditionOp,
                                TargetValue = strategyArgs.TargetPrice,
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

                    var opLabel = strategyArgs.Operator == "greater_than" ? "rises above" : "drops below";
                    var execLabel = strategyArgs.ExecutionType switch
                    {
                        "auto_execute" => "automatically executed",
                        "alert_and_execute" => "alerted and automatically executed",
                        _ => "you'll be alerted",
                    };

                    return (
                        SystemMessages.StrategyCreated(
                            language,
                            strategyArgs.Side,
                            quantity,
                            strategyArgs.Asset,
                            opLabel,
                            strategyArgs.TargetPrice,
                            strategyArgs.ExpiresInDays,
                            execLabel,
                            strategyArgs.MaxAmountEgp
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
            string language,
            List<Message> history
        )
        {
            var (reply, toolCall, source) = await _router.HandleAgenticAsync(
                message,
                userId,
                conversationId,
                language,
                history
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

        private static string SanitizeReply(string text)
        {
            // Strip CJK characters that sometimes appear in AI provider responses
            return Regex.Replace(text, @"[\u4E00-\u9FFF\u3400-\u4DBF\uF900-\uFAFF\u3000-\u303F\uFF00-\uFFEF]", "");
        }
    }
}