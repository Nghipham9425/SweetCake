namespace SweetCakeShop.Models.ViewModels
{
    public class CustomerBehaviorDashboardViewModel
    {
        public List<CustomerBehaviorProductSummaryViewModel> ViewedProducts { get; set; } = new();
        public List<CustomerBehaviorProductSummaryViewModel> AddedToCartProducts { get; set; } = new();
        public List<CustomerBehaviorEventRowViewModel> RecentEvents { get; set; } = new();
    }

    public class CustomerBehaviorProductSummaryViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int EventCount { get; set; }
        public int TotalQuantity { get; set; }
        public DateTime LastSeenAt { get; set; }
    }

    public class CustomerBehaviorEventRowViewModel
    {
        public string EventType { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? CustomerEmail { get; set; }
        public string CustomerKey { get; set; } = string.Empty;
        public string? PageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
