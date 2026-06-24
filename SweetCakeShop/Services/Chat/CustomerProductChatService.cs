using System.Text.RegularExpressions;
using SweetCakeShop.Models.Api;
using SweetCakeShop.Services.AI;
using SweetCakeShop.Services.AI.Rag;
using SweetCakeShop.Services.Chat.Gemini;
using SweetCakeShop.Services.Chat.OpenAi;

namespace SweetCakeShop.Services.Chat
{
    public class CustomerProductChatService : ICustomerProductChatService
    {
        private const string SupportOpenMarker = "SUPPORT_OPEN";
        private const string SupportClosedMarker = "SUPPORT_CLOSED";
        private const string WelcomeMessage =
            "Chao anh/chi! Em la tro ly SweetCakeShop. Em co the tu van banh, gia, giao hang hoac chuyen anh/chi sang CSKH.";

        private readonly IChatIdentityService _identity;
        private readonly IChatHistoryService _history;
        private readonly IChatTokenMergeService _merge;
        private readonly IProductCatalogForAiService _catalog;
        private readonly IProductIntentResolver _resolver;
        private readonly IGeminiChatApiService _gemini;
        private readonly IOpenAiChatApiService _openAi;
        private readonly ITopicFilterService _topicFilter;
        private readonly IChatSecurityService _security;

        public CustomerProductChatService(
            IChatIdentityService identity,
            IChatHistoryService history,
            IChatTokenMergeService merge,
            IProductCatalogForAiService catalog,
            IProductIntentResolver resolver,
            IGeminiChatApiService gemini,
            IOpenAiChatApiService openAi,
            ITopicFilterService topicFilter,
            IChatSecurityService security)
        {
            _identity = identity;
            _history = history;
            _merge = merge;
            _catalog = catalog;
            _resolver = resolver;
            _gemini = gemini;
            _openAi = openAi;
            _topicFilter = topicFilter;
            _security = security;
        }

        public async Task<ChatHistoryResponse> GetChatHistoryAsync(CancellationToken ct = default)
        {
            await _merge.TryMergeOnAuthenticatedRequestAsync(ct);
            _identity.EnsureChatTokenCookie();

            var supportActive = await _history.IsHumanSupportActiveAsync(ct);
            var has = await _history.HasAnyMessagesAsync(ct);
            if (!has)
            {
                return new ChatHistoryResponse
                {
                    Messages =
                    [
                        new ChatMessageDto { Sender = "model", Content = WelcomeMessage, CreatedAt = DateTime.UtcNow }
                    ],
                    QuickReplies = GetQuickReplies(supportActive),
                    HumanSupportActive = supportActive
                };
            }

            return new ChatHistoryResponse
            {
                Messages = await _history.GetHistoryForUiAsync(ct),
                QuickReplies = GetQuickReplies(supportActive),
                HumanSupportActive = supportActive
            };
        }

        public async Task<SendChatMessageResponse> SendMessageAsync(
            SendChatMessageRequest request,
            CancellationToken ct = default)
        {
            var text = (request.UserMessage ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text) || text.Length > 2000)
                return new SendChatMessageResponse { Success = false, Reply = "Tin nhan khong hop le." };

            await _merge.TryMergeOnAuthenticatedRequestAsync(ct);
            _identity.EnsureChatTokenCookie();

            if (IsEndSupportRequest(text))
            {
                await _history.AddUserMessageAsync(text, request.ProductId, ct);
                await _history.AddSystemMessageAsync(SupportClosedMarker, ct);
                var endReply = "Da ket thuc chat voi CSKH. Tro ly AI da hoat dong lai, anh/chi co the hoi ve san pham, gia hoac giao hang.";
                await _history.AddModelMessageAsync(endReply, ct);
                return new SendChatMessageResponse
                {
                    Success = true,
                    Reply = endReply,
                    QuickReplies = GetQuickReplies(false),
                    HumanSupportActive = false
                };
            }

            var supportActive = await _history.IsHumanSupportActiveAsync(ct);
            if (supportActive)
            {
                await _history.AddUserMessageAsync(text, request.ProductId, ct);
                return new SendChatMessageResponse
                {
                    Success = true,
                    Reply = "Da gui tin nhan cho CSKH. Anh/chi bam 'Ket thuc CSKH' neu muon quay lai tro ly AI.",
                    QuickReplies = GetQuickReplies(true),
                    HumanSupportActive = true
                };
            }

            if (IsHumanSupportRequest(text))
            {
                await _history.AddUserMessageAsync(text, request.ProductId, ct);
                await _history.AddSystemMessageAsync(SupportOpenMarker, ct);
                var handoffReply = "Da chuyen anh/chi sang bo phan CSKH. Tu luc nay tro ly AI se tam dung, nhan vien se tra loi trong khung chat nay. Bam 'Ket thuc CSKH' de quay lai AI.";
                await _history.AddModelMessageAsync(handoffReply, ct);
                return new SendChatMessageResponse
                {
                    Success = true,
                    Reply = handoffReply,
                    QuickReplies = GetQuickReplies(true),
                    HumanSupportActive = true
                };
            }

            if (_topicFilter.IsClearlyOffTopic(text))
                return new SendChatMessageResponse { Success = true, Reply = _topicFilter.GetRejectionMessage("vi") };

            if (_security.IsRestrictedRequest(text))
                return new SendChatMessageResponse { Success = true, Reply = _security.GetStaffRejectionMessage("vi", AiChatMode.Customer) };

            await _history.AddUserMessageAsync(text, request.ProductId, ct);
            var recent = await _history.GetRecentAsync(6, ct);

            var resolved = await _resolver.ResolveAsync(text, request.ProductId, recent, ct);
            string reply;
            var products = ChatProductCardMapper.ToCards(resolved.Products);

            if (resolved.UseDirectReply)
            {
                reply = TrimSentences(resolved.DirectReply, 3);
            }
            else
            {
                var catalog = await _catalog.BuildCatalogTextAsync(ct);
                var system = BuildSystemPrompt(catalog);
                reply = await _gemini.GenerateReplyAsync(system, recent, text, resolved.FactsBlock, ct)
                          ?? await _openAi.GenerateReplyAsync(system, recent, text, resolved.FactsBlock, ct)
                          ?? "Em chua ket noi duoc AI. Anh/chi co the bam 'Chat CSKH' de nhan vien ho tro.";
                reply = TrimSentences(reply, 3);
            }

            await _history.AddModelMessageAsync(reply, ct);

            return new SendChatMessageResponse
            {
                Success = true,
                Reply = reply,
                Products = products,
                QuickReplies = GetQuickReplies(false),
                HumanSupportActive = false
            };
        }

        private static string BuildSystemPrompt(string catalog) => $"""
            Ban la nhan vien tu van banh SweetCakeShop. Tra loi ngan gon toi da 2 cau.
            Chi tra loi dua tren danh muc va du lieu cau hoi ben duoi. Khong bia banh hoac gia.
            Neu khach can gap nguoi that/CSKH, huong dan bam nut Chat CSKH.

            {catalog}
            """;

        private static bool IsHumanSupportRequest(string text) =>
            Regex.IsMatch(
                text,
                @"cskh|chat cskh|cham soc khach|chăm sóc khách|nhan vien|nhân viên|nguoi that|người thật|tu van vien|tư vấn viên|support|hotline",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static bool IsEndSupportRequest(string text) =>
            Regex.IsMatch(
                text,
                @"ket thuc cskh|kết thúc cskh|thoat cskh|thoát cskh|end support|quay lai ai|quay lại ai",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static List<string> GetQuickReplies(bool supportActive) =>
            supportActive
                ? ["Ket thuc CSKH"]
                : ["Banh ban chay?", "Giao hang?", "Chat CSKH"];

        private static string TrimSentences(string text, int max)
        {
            var cleaned = text.Trim();
            var parts = Regex.Split(cleaned, @"(?<=[.!?…])\s+");
            return parts.Length <= max ? cleaned : string.Join(" ", parts.Take(max));
        }
    }
}
