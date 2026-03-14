using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models;
using X.PagedList;
using X.PagedList.Extensions;

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
        public IActionResult Index(string? sortOrder, string? searchTerm, int? page)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentSearch"] = searchTerm;

            var products = from p in _context.Products.Include(p => p.Category)
                           select p;

            //tìm kiếm theo tên sản phẩm
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var keyword = searchTerm.Trim();
                products = products.Where(p => p.ProductName.Contains(keyword));
            }

            // Sắp xếp theo giá
            products = sortOrder switch
            {
                "price_asc" => products.OrderBy(p => p.Price),
                "price_desc" => products.OrderByDescending(p => p.Price),
                _ => products.OrderBy(p => p.ProductId) // Mặc định
            };

            int pageSize = 12;
            int pageNumber = page ?? 1;
            var pagedProducts = products.ToPagedList(pageNumber, pageSize);


            return View("IndexPro", pagedProducts);
        }

        [HttpGet]
        public IActionResult IndexPro(string? sortOrder, string? searchTerm, int? page)
        {
            return Index(sortOrder, searchTerm, page);
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
