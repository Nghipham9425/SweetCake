namespace SweetCakeShop.Models
{
    public class CustomerBehaviorEvent
    {
        public long CustomerBehaviorEventId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public string? UserId { get; set; }
        public string? CustomerEmail { get; set; }
        public string? ChatToken { get; set; }
        public string? SessionId { get; set; }
        public string? PageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Product? Product { get; set; }
    }
}
