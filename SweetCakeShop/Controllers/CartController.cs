using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;
using SweetCakeShop.Helpers;
using SweetCakeShop.Models;
using SweetCakeShop.Models.ViewModels;
using SweetCakeShop.Services;

namespace SweetCakeShop.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CartService _cartService;
        private readonly OrderService _orderService;
        private readonly IOrderInventoryService _inventoryService;
        private readonly ICustomerBehaviorService _behaviorService;
        private readonly IProductRecommendationService _recommendationService;
        private readonly IConfiguration _configuration;

        public CartController(
            ApplicationDbContext context,
            CartService cartService,
            OrderService orderService,
            IOrderInventoryService inventoryService,
            ICustomerBehaviorService behaviorService,
            IProductRecommendationService recommendationService,
            IConfiguration configuration)
        {
            _context = context;
            _cartService = cartService;
            _orderService = orderService;
            _inventoryService = inventoryService;
            _behaviorService = behaviorService;
            _recommendationService = recommendationService;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var cart = _cartService.GetCart();
            cart.Recommendations = await _recommendationService.GetForCartAsync(cart.Items.Select(i => i.ProductId));
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int id, int quantity = 1)
        {
            var product = _context.Products.Find(id);
            if (product == null)
                return NotFound();

            _cartService.AddToCart(product, quantity);
            await _behaviorService.TrackAddToCartAsync(
                product.ProductId,
                product.ProductName,
                quantity,
                Request.Headers.Referer.ToString());

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

        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = _cartService.GetCart();
            if (!cart.Items.Any())
                return RedirectToAction("Index");

            var model = new CheckoutViewModel
            {
                CustomerEmail = User.Identity?.IsAuthenticated == true
                    ? User.FindFirstValue(ClaimTypes.Email) ?? string.Empty
                    : string.Empty,
                CustomerName = User.Identity?.IsAuthenticated == true
                    ? User.Identity?.Name ?? string.Empty
                    : string.Empty
            };

            ViewData["Cart"] = cart;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutConfirm(CheckoutViewModel checkout)
        {
            var cart = _cartService.GetCart();
            if (!cart.Items.Any())
                return RedirectToAction("Index");

            var userId = User.Identity?.IsAuthenticated == true
                ? User.FindFirstValue(ClaimTypes.NameIdentifier)
                : null;

            Order order;
            try
            {
                order = await _orderService.CreateOrderAsync(cart, checkout, userId);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewData["Cart"] = cart;
                return View("Checkout", checkout);
            }

            _cartService.ClearCart();

            return RedirectToAction("Payment", new { orderId = order.OrderId });
        }

        [HttpGet]
        public async Task<IActionResult> Payment(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound();

            return View(new PaymentViewModel { Order = order });
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
                var stockCheck = await _inventoryService.ConfirmAndDeductAsync(order);
                if (!stockCheck.IsAvailable)
                {
                    TempData["Error"] = stockCheck.Message;
                    return RedirectToAction("Payment", new { orderId = order.OrderId });
                }

                return RedirectToAction("Success", new { orderId = order.OrderId });
            }

            if (method == "Online")
            {
                return await RedirectToSepayAsync(order);
            }

            TempData["Error"] = "Phương thức thanh toán không hợp lệ.";
            return RedirectToAction("Payment", new { orderId = order.OrderId });
        }

        [HttpGet]
        public async Task<IActionResult> OnlinePayment(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound();

            ViewBag.PaymentCode = SepayOptions.GetPaymentCode(order, _configuration);
            ViewBag.PaymentContent = SepayOptions.GetPaymentContent(order, _configuration);
            ViewBag.QrUrl = SepayOptions.BuildQrUrl(order, _configuration);
            ViewBag.PaymentNotice = TempData["PaymentNotice"]?.ToString();
            ViewBag.BankName = _configuration["SePay:BankCode"] ?? _configuration["BankTransfer:BankName"] ?? "Vietcombank";
            ViewBag.AccountNumber = _configuration["SePay:AccountNumber"] ?? _configuration["BankTransfer:AccountNumber"] ?? "0123456789";
            ViewBag.AccountName = _configuration["SePay:AccountName"] ?? _configuration["BankTransfer:AccountName"] ?? "SWEET CAKE SHOP";
            ViewBag.BankBranch = _configuration["SePay:Branch"] ?? _configuration["BankTransfer:Branch"] ?? "Chi nhánh TP.HCM";

            return View(new PaymentViewModel { Order = order });
        }

        [HttpGet]
        public async Task<IActionResult> PaymentStatus(int orderId)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound();

            var isPaid = OrderStatuses.CountsForRevenue(order.Status);
            return Json(new
            {
                orderId = order.OrderId,
                status = order.Status,
                isPaid,
                redirectUrl = isPaid ? Url.Action("Success", "Cart", new { orderId = order.OrderId }) : null
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOnlinePayment(int orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
                return NotFound();

            order.Status = "AwaitingConfirmation";
            await _context.SaveChangesAsync();

            return RedirectToAction("Success", new { orderId = order.OrderId });
        }

        [HttpGet]
        public async Task<IActionResult> Success(int orderId, string? session_id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
                return NotFound();

            if (!string.IsNullOrEmpty(session_id))
            {
                try
                {
                    var sessionService = new SessionService();
                    var session = await sessionService.GetAsync(session_id);

                    if (session != null && session.PaymentStatus == "paid")
                    {
                        var stockCheck = await _inventoryService.ConfirmAndDeductAsync(order);
                        if (!stockCheck.IsAvailable)
                        {
                            TempData["Error"] = stockCheck.Message;
                        }
                    }
                    else
                    {
                        order.Status = "PaymentFailed";
                        await _context.SaveChangesAsync();
                    }
                }
                catch
                {
                    // Keep current order status when Stripe verification is unavailable.
                }
            }

            ViewData["OrderId"] = orderId;
            return View();
        }

        private async Task<IActionResult> RedirectToSepayAsync(Order order)
        {
            order.Status = "AwaitingPayment";
            await _context.SaveChangesAsync();

            TempData["PaymentNotice"] =
                "Quét mã VietQR để thanh toán. SePay sẽ tự động xác nhận khi giao dịch khớp số tiền và mã đơn.";

            return RedirectToAction("OnlinePayment", new { orderId = order.OrderId });
        }
    }
}
