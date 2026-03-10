using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Services;

namespace SweetCakeShop.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CartService _cartService;

        public CartController(ApplicationDbContext context, CartService cartService)
        {
            _context = context;
            _cartService = cartService;
        }

        // GET: /Cart
        public IActionResult Index()
        {
            var cart = _cartService.GetCart();
            return View(cart);
        }

        // POST: /Cart/Add/{id}
        [HttpPost]
        public IActionResult Add(int id, int quantity = 1)
        {
            var product = _context.Products.Find(id);
            if (product == null)
                return NotFound();

            _cartService.AddToCart(product, quantity);

            return Json(new { success = true, message = $"{product.ProductName} đã thêm vào giỏ hàng!" });
        }

        // POST: /Cart/Update
        [HttpPost]
        public IActionResult Update(int productId, int quantity)
        {
            _cartService.UpdateQuantity(productId, quantity);
            return RedirectToAction("Index");
        }

        // POST: /Cart/Remove
        [HttpPost]
        public IActionResult Remove(int productId)
        {
            _cartService.RemoveFromCart(productId);
            return RedirectToAction("Index");
        }
    }
}