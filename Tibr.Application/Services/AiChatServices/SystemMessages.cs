namespace Tibr.Application.Services.AiChatServices;

public static class SystemMessages
{
    public static string OutOfScope(string lang) => lang == "ar"
        ? "يمكنني المساعدة فقط في موضوعات الاستثمار في الذهب والفضة على تبر. اسأل عن محفظتك أو أسعار الذهب أو كيفية عمل تبر."
        : "I can only help with gold and silver investment topics on Tibr. Feel free to ask about your portfolio, gold prices, or how Tibr works.";

    public static string FaqNoAnswers(string lang) => lang == "ar"
        ? "ليس لدي إجابة محددة لذلك. حاول إعادة الصياغة أو تواصل مع دعم تبر."
        : "I don't have a specific answer for that. Try rephrasing or contact Tibr support.";

    public static string FaqGenFailed(string lang) => lang == "ar"
        ? "عذراً، لم أتمكن من إنشاء إجابة."
        : "Sorry, I could not generate an answer.";

    public static string FactsNoFacts(string lang) => lang == "ar"
        ? "لم يتم العثور على معلومات سياسة محددة."
        : "No specific policy facts found.";

    public static string FactsPriceUnavailable(string lang) => lang == "ar"
        ? "بيانات الأسعار غير متوفرة مؤقتاً."
        : "Price data temporarily unavailable.";

    public static string FactsGenFailed(string lang) => lang == "ar"
        ? "عذراً، لم أتمكن من إنشاء إجابة."
        : "Sorry, I could not generate an answer.";

    public static string PriceUnavailable(string lang) => lang == "ar"
        ? "لا يمكنني جلب سعر الذهب الحالي الآن. حاول مرة أخرى لاحقاً."
        : "I'm unable to fetch the current gold price right now. Please try again later.";

    public static string PortfolioGenFailed(string lang) => lang == "ar"
        ? "عذراً، لم أتمكن من تحليل محفظتك."
        : "Sorry, I could not analyze your portfolio.";

    public static string PlannerClarify(string lang) => lang == "ar"
        ? "هل يمكنك تقديم المزيد من التفاصيل حول هدف الادخار الخاص بك؟"
        : "Could you provide more details about your savings goal?";

    public static string PlannerFallback(string lang) => lang == "ar"
        ? "هذه هي خطة الادخار الخاصة بك. تابع تقدمك!"
        : "Here's your savings plan. Check back as you make progress!";

    public static string AgenticFallback(string lang) => lang == "ar"
        ? "يمكنني مساعدتك في شراء أو بيع الذهب والفضة. ماذا تريد أن تفعل؟"
        : "I can help you buy or sell gold and silver. What would you like to do?";

    public static string AgenticProposalFailed(string lang) => lang == "ar"
        ? "تعذر إنشاء اقتراح الطلب."
        : "Could not create order proposal.";

    public static string ConditionalFallback(string lang) => lang == "ar"
        ? "يمكنني مساعدتك في إعداد أوامر شرطية للذهب والفضة. مثال: 'اشتر 10 جرام ذهب عندما ينخفض السعر عن 8000 جنيها للجرام'."
        : "I can help you set conditional orders for gold and silver. For example: 'buy 10g of gold when price drops below 8000 EGP/g'.";

    public static string ConditionalCreateFailed(string lang) => lang == "ar"
        ? "تعذر إنشاء أمر الاستراتيجية."
        : "Could not create strategy order.";

    public static string ProposalNoPrice(string lang) => lang == "ar"
        ? "تعذر جلب السعر الحالي."
        : "Unable to fetch current price.";

    public static string ProposalNoAmount(string lang) => lang == "ar"
        ? "لم يتم تحديد الكمية."
        : "No amount specified.";

    public static string ProposalNoBalance(string lang) => lang == "ar"
        ? "تعذر جلب أرصدة المحفظة."
        : "Unable to fetch wallet balances.";

    public static string ProposalInsufficientFunds(string lang) => lang == "ar"
        ? "رصيد غير كافٍ للشراء."
        : "Insufficient funds for purchase.";

    public static string ProposalNoHoldings(string lang) => lang == "ar"
        ? "لا توجد ممتلكات متاحة للبيع."
        : "No holdings available to sell.";

    public static string ProposalNoPending(string lang) => lang == "ar"
        ? "لم يتم العثور على اقتراح معلق."
        : "No pending proposal found.";

    public static string ProposalExpired(string lang) => lang == "ar"
        ? "انتهت صلاحية الاقتراح. يرجى بدء طلب جديد."
        : "Proposal has expired. Please start a new order.";

    public static string CancelReply(string lang) => lang == "ar"
        ? "تم إلغاء الطلب. هل هناك شيء آخر يمكنني مساعدتك به؟"
        : "Order cancelled. Anything else I can help with?";

    public static string AiUnavailable(string lang) => lang == "ar"
        ? "عذراً، خدمة الذكاء الاصطناعي غير متوفرة حالياً. حاول مرة أخرى قريباً."
        : "I'm sorry, the AI service is temporarily unavailable. Please try again shortly.";

    private static string TranslateAction(string action, string lang)
    {
        if (lang != "ar") return action;
        return action.ToLowerInvariant() switch
        {
            "buy" => "شراء",
            "sell" => "بيع",
            _ => action
        };
    }

    private static string TranslateAsset(string asset, string lang)
    {
        if (lang != "ar") return asset;
        return asset.ToLowerInvariant() switch
        {
            "gold" => "الذهب",
            "silver" => "الفضة",
            _ => asset
        };
    }

    public static string UnrelatedReminder(string lang, string action, decimal grams, string asset)
    {
        if (lang == "ar")
        {
            var a = TranslateAction(action, lang);
            var as_ = TranslateAsset(asset, lang);
            return $"لديك طلب معلق لـ {a} {grams:F4}g {as_}. رد 'تأكيد' للمتابعة أو 'إلغاء' للتجاهل.\n\n";
        }
        return $"You have a pending order to {action} {grams:F4}g {asset}. Reply 'confirm' to proceed or 'cancel' to discard it.\n\n";
    }

    public static string OrderProposal(string lang, string action, decimal grams, string asset, decimal price, decimal total)
    {
        if (lang == "ar")
        {
            var a = TranslateAction(action, lang);
            var as_ = TranslateAsset(asset, lang);
            return $"اقتراح {a} {grams:F4}g {as_} بسعر {price:N2} جنيها للجرام (الإجمالي: {total:N2} جنيها). رد 'تأكيد' أو 'إلغاء'.";
        }
        return $"Proposed {action} of {grams:F4}g {asset} at {price:N2} EGP/g (total: {total:N2} EGP). Reply 'confirm' or 'cancel'.";
    }

    public static string BuyReceipt(string lang, decimal grams, string asset, decimal price) => lang == "ar"
        ? $"تم تنفيذ الطلب! تم شراء {grams:F4}g {TranslateAsset(asset, lang)} بسعر {price:N2} جنيها للجرام."
        : $"Order executed! Bought {grams:F4}g {asset} at {price:N2} EGP/g.";

    public static string SellReceipt(string lang, decimal grams, string asset, decimal price) => lang == "ar"
        ? $"تم تنفيذ الطلب! تم بيع {grams:F4}g {TranslateAsset(asset, lang)} بسعر {price:N2} جنيها للجرام."
        : $"Order executed! Sold {grams:F4}g {asset} at {price:N2} EGP/g.";

    public static string StrategyCreated(string lang, string side, decimal quantity, string asset, string opLabel, decimal targetPrice, int expiresInDays, string execLabel)
    {
        if (lang == "ar")
        {
            var sideAr = side == "buy" ? "شراء" : "بيع";
            var opAr = opLabel == "rises above" ? "يرتفع عن" : "ينخفض عن";
            var execAr = execLabel == "automatically executed" ? "سيتم تنفيذه تلقائياً" : "سيتم إعلامك";
            return $"✅ تم إنشاء الاستراتيجية! سأقوم بـ {sideAr} {quantity:F4}g {TranslateAsset(asset, lang)} عندما {opAr} {targetPrice:N2} جنيها للجرام. تنتهي في {expiresInDays} يوماً و{execAr}.";
        }

        return $"✅ Strategy created! I'll {side} {quantity:F4}g of {asset} when the price {opLabel} {targetPrice:N2} EGP/g. It expires in {expiresInDays} days and will be {execLabel}.";
    }
}
