namespace SweetCakeShop.Models
{
    public class ProductPriceHistory
    {
        public int ProductPriceHistoryId { get; set; }
        public int ProductId { get; set; }
        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
        public decimal OldCostPrice { get; set; }
        public decimal NewCostPrice { get; set; }
        public decimal? OldDiscountPrice { get; set; }
        public decimal? NewDiscountPrice { get; set; }
        public DateTime? OldDiscountStartAt { get; set; }
        public DateTime? NewDiscountStartAt { get; set; }
        public DateTime? OldDiscountEndAt { get; set; }
        public DateTime? NewDiscountEndAt { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public string? ChangedBy { get; set; }

        public Product? Product { get; set; }
    }
}
