using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models;

namespace SweetCakeShop.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Products
        public async Task<IActionResult> Index(string sortOrder)
        {
            ViewData["CurrentSort"] = sortOrder;

            var products = from p in _context.Products.Include(p => p.Category)
                           select p;

            // Sắp xếp theo giá
            products = sortOrder switch
            {
                "price_asc" => products.OrderBy(p => p.Price),
                "price_desc" => products.OrderByDescending(p => p.Price),
                _ => products.OrderBy(p => p.ProductId) // Mặc định
            };

            return View("IndexPro", await products.ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> IndexPro(string sortOrder)
        {
            return await Index(sortOrder);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}
