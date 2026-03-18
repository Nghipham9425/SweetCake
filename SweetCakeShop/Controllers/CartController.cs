using Microsoft.AspNetCore.Mvc;
using SweetCakeShop.Data;
using SweetCakeShop.Services;
using SweetCakeShop.Models;
using SweetCakeShop.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SweetCakeShop.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CartService _cartService;
        private readonly OrderService _orderService;

        public CartController(ApplicationDbContext context, CartService cartService, OrderService orderService)
        {
            _context = context;
            _cartService = cartService;
            _orderService = orderService;
        }

        public IActionResult Index()
        {
            var cart = _cartService.GetCart();
            return View(cart);
        }

        [HttpPost]
        public IActionResult Add(int id, int quantity = 1)
        {
            var product = _context.Products.Find(id);
            if (product == null)
                return NotFound();

            _cartService.AddToCart(product, quantity);

            return Json(new { success = true, message = $"{product.ProductName} đã thêm vào giỏ hàng!" });
        }

        [HttpPost]
        public IActionResult Update(int productId, int quantity)
        {
            _cartService.UpdateQuantity(productId, quantity);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Remove(int productId)
        {
            _cartService.RemoveFromCart(productId);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Count()
        {
            var count = _cartService.GetCart().Items.Sum(i => i.Quantity);
            return Json(new { count });
        }

        // Show checkout with shipping form
        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = _cartService.GetCart();
            if (!cart.Items.Any())
                return RedirectToAction("Index");

            var model = new CheckoutViewModel();

            // If user is logged in, you could prefill name/email from claims/profile
            if (User.Identity?.IsAuthenticated == true)
            {
                model.CustomerEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
                model.CustomerName = User.Identity?.Name ?? string.Empty;
            }

            ViewData["Cart"] = cart;
            return View(model);
        }

        // Accept checkout from guests and authenticated users
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutConfirm(CheckoutViewModel checkout)
        {
            var cart = _cartService.GetCart();
            if (!cart.Items.Any())
                return RedirectToAction("Index");

            string? userId = null;
            if (User.Identity?.IsAuthenticated == true)
                userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = await _orderService.CreateOrderAsync(cart, checkout, userId);

            _cartService.ClearCart();

            return RedirectToAction("Success", new { orderId = order.OrderId });
        }

        public IActionResult Success(int orderId)
        {
            ViewData["OrderId"] = orderId;
            return View();
        }
    }
}