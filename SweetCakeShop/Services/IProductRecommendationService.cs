using SweetCakeShop.Models.ViewModels;

namespace SweetCakeShop.Services
{
    public interface IProductRecommendationService
    {
        Task<List<ProductRecommendationViewModel>> GetForProductAsync(int productId, int take = 4);
        Task<List<ProductRecommendationViewModel>> GetForCartAsync(IEnumerable<int> productIds, int take = 4);
    }
}
