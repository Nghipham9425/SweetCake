using Microsoft.AspNetCore.Identity;
namespace SweetCakeShop.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public string UserId { get; set; } = string.Empty;   // liên kết với Identity User
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = "Pending";     // Pending, Confirmed, Shipped, Delivered, Cancelled

        public IdentityUser? User { get; set; }           // nếu dùng Identity
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
