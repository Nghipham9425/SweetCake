namespace SweetCakeShop.Services
{
    public interface ICustomerBehaviorService
    {
        Task TrackProductViewAsync(int productId, string productName, string? pageUrl = null, CancellationToken ct = default);
        Task TrackAddToCartAsync(int productId, string productName, int quantity, string? pageUrl = null, CancellationToken ct = default);
    }
}
