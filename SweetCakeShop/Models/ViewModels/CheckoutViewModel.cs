namespace SweetCakeShop.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public CartViewModel Cart { get; set; } = new();
        public string? FullName { get; set; }       // optional: thêm nếu muốn thu thập thông tin giao hàng
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Note { get; set; }
    }
}
