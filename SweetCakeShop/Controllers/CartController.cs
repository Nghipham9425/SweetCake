using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly IPaymentService _paymentService;

        public CartController(ApplicationDbContext context, CartService cartService, OrderService orderService, IPaymentService paymentService)
        {
            _context = context;
            _cartService = cartService;
            _orderService = orderService;  
            _paymentService = paymentService;
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

            // Require login to proceed to checkout
            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["LoginMessage"] = "Bạn phải tiến hành đăng nhập để tiếp tục mua sản phẩm";
                var returnUrl = Url.Action("Checkout", "Cart");
                return Redirect($"/Identity/Account/Login?returnUrl={System.Net.WebUtility.UrlEncode(returnUrl ?? "/")}");
            }

            var model = new CheckoutViewModel();

            // Prefill when logged in
            model.CustomerEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            model.CustomerName = User.Identity?.Name ?? string.Empty;

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

            // After creating order, redirect to Payment selection page
            return RedirectToAction("Payment", new { orderId = order.OrderId });
        }

        // Payment selection & result page
        [HttpGet]
        public async Task<IActionResult> Payment(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound();

            var model = new PaymentViewModel
            {
                Order = order
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(int orderId, string method)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound();

            if (method == "COD")
            {
                order.Status = "Confirmed";
                await _context.SaveChangesAsync();

                return RedirectToAction("Success", new { orderId = order.OrderId });
            }
            else if (method == "Online")
            {
                // Create Stripe Checkout Session (or other provider) and redirect if we get a URL
                var payment = await _paymentService.CreatePaymentAsync(order);

                // Mark order awaiting online payment (or Redirected)
                order.Status = "AwaitingPayment";
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(payment.PaymentUrl))
                {
                    // redirect user to Stripe Checkout
                    return Redirect(payment.PaymentUrl);
                }

                // otherwise render Payment view with fallback info (bank transfer)
                var model = new PaymentViewModel
                {
                    Order = order,
                    PaymentResult = payment
                };

                return View("Payment", model);
            }

            TempData["Error"] = "Phương thức thanh toán không hợp lệ.";
            return RedirectToAction("Payment", new { orderId = order.OrderId });
        }

        // Internal page that displays your payment image/QR code in the middle
        [HttpGet]
        public async Task<IActionResult> OnlinePayment(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound();

            var model = new PaymentViewModel
            {
                Order = order
            };

            return View(model); // Views/Cart/OnlinePayment.cshtml
        }

        // User clicks "I have paid" on internal page to confirm manually
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOnlinePayment(int orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
                return NotFound();

            // mark as awaiting manual confirmation (you can change to Confirmed if you prefer)
            order.Status = "AwaitingConfirmation";
            await _context.SaveChangesAsync();

            return RedirectToAction("Success", new { orderId = order.OrderId });
        }

        public IActionResult Success(int orderId)
        {
            ViewData["OrderId"] = orderId;
            return View();
        }
    }
}