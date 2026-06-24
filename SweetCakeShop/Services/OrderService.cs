using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models;
using SweetCakeShop.Models.ViewModels;

namespace SweetCakeShop.Services
{
    public class OrderService
    {
        private readonly ApplicationDbContext _db;
        private readonly IOrderInventoryService _inventory;

        public OrderService(ApplicationDbContext db, IOrderInventoryService inventory)
        {
            _db = db;
            _inventory = inventory;
        }

        public async Task<Order> CreateOrderAsync(CartViewModel cart, CheckoutViewModel checkout, string? userId)
        {
            if (cart == null || !cart.Items.Any())
                throw new ArgumentException("Cart is empty", nameof(cart));

            var stockCheck = await _inventory.CheckCartAsync(cart);
            if (!stockCheck.IsAvailable)
                throw new InvalidOperationException(stockCheck.Message);

            var order = new Order
            {
                UserId = userId ?? string.Empty,
                CustomerName = checkout.CustomerName ?? string.Empty,
                CustomerEmail = checkout.CustomerEmail ?? string.Empty,
                CustomerPhone = checkout.CustomerPhone ?? string.Empty,
                ShippingAddress = checkout.ShippingAddress ?? string.Empty,
                IsGuest = string.IsNullOrEmpty(userId),
                OrderDate = DateTime.UtcNow,
                TotalPrice = cart.TotalAmount,
                Status = "Pending"
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync(); // get OrderId

            foreach (var item in cart.Items)
            {
                var detail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    CostPrice = item.CostPrice
                };
                _db.OrderDetails.Add(detail);
            }

            await _db.SaveChangesAsync();

            // load navigation if needed
            await _db.Entry(order).Collection(o => o.OrderDetails).LoadAsync();

            return order;
        }
    }
}
