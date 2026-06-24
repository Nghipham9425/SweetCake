using System.Security.Claims;
using SweetCakeShop.Data;
using SweetCakeShop.Models;
using SweetCakeShop.Services.Chat;

namespace SweetCakeShop.Services
{
    public class CustomerBehaviorService : ICustomerBehaviorService
    {
        public const string ViewProduct = "ViewProduct";
        public const string AddToCart = "AddToCart";

        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _http;
        private readonly IChatIdentityService _chatIdentity;

        public CustomerBehaviorService(
            ApplicationDbContext db,
            IHttpContextAccessor http,
            IChatIdentityService chatIdentity)
        {
            _db = db;
            _http = http;
            _chatIdentity = chatIdentity;
        }

        public Task TrackProductViewAsync(int productId, string productName, string? pageUrl = null, CancellationToken ct = default) =>
            TrackAsync(ViewProduct, productId, productName, 1, pageUrl, ct);

        public Task TrackAddToCartAsync(int productId, string productName, int quantity, string? pageUrl = null, CancellationToken ct = default) =>
            TrackAsync(AddToCart, productId, productName, Math.Max(1, quantity), pageUrl, ct);

        private async Task TrackAsync(
            string eventType,
            int productId,
            string productName,
            int quantity,
            string? pageUrl,
            CancellationToken ct)
        {
            var ctx = _http.HttpContext;
            var userId = ctx?.User.Identity?.IsAuthenticated == true
                ? ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                : null;
            var email = ctx?.User.Identity?.IsAuthenticated == true
                ? ctx.User.FindFirst(ClaimTypes.Email)?.Value
                : null;

            string? chatToken = null;
            if (string.IsNullOrWhiteSpace(userId))
            {
                chatToken = _chatIdentity.EnsureChatTokenCookie();
            }

            var url = pageUrl;
            if (string.IsNullOrWhiteSpace(url) && ctx != null)
            {
                url = ctx.Request.Path + ctx.Request.QueryString;
            }

            _db.CustomerBehaviorEvents.Add(new CustomerBehaviorEvent
            {
                EventType = eventType,
                ProductId = productId,
                ProductName = productName,
                Quantity = quantity,
                UserId = userId,
                CustomerEmail = email,
                ChatToken = string.IsNullOrWhiteSpace(userId) ? chatToken : null,
                SessionId = ctx?.Session?.Id,
                PageUrl = url,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
        }
    }
}
