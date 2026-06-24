namespace SweetCakeShop.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public DateTime? DiscountStartAt { get; set; }
        public DateTime? DiscountEndAt { get; set; }
        public string? Description { get; set; }
        public string? Image { get; set; }          // lưu đường dẫn ảnh, ví dụ: /images/cake1.jpg
        public int CategoryId { get; set; }

        public Category? Category { get; set; }
        public ICollection<ProductPriceHistory> PriceHistories { get; set; } = new List<ProductPriceHistory>();
    }
}
