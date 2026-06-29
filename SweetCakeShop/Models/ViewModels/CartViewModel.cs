namespace SweetCakeShop.Models.ViewModels
{
    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();
        public List<ProductRecommendationViewModel> Recommendations { get; set; } = new List<ProductRecommendationViewModel>();
        public decimal TotalAmount => Items.Sum(i => i.Subtotal);
    }
}
