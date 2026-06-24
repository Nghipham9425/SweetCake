namespace SweetCakeShop.Models
{
    public class OrderDetail
    {
        public int OrderDetailId { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }          // giá tại thời điểm mua
        public decimal CostPrice { get; set; }      // giá vốn tại thời điểm mua
        public decimal Profit => (Price - CostPrice) * Quantity;

        public Order? Order { get; set; }
        public Product? Product { get; set; }
    }
}
