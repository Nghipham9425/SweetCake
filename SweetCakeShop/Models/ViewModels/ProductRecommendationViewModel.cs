namespace SweetCakeShop.Models.ViewModels
{
    public class ProductRecommendationViewModel
    {
        public Product Product { get; set; } = null!;
        public double Support { get; set; }
        public double Confidence { get; set; }
        public double Lift { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
