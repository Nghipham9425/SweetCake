using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models;
using SweetCakeShop.Models.ViewModels;
using System.Diagnostics;

namespace SweetCakeShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var featuredProducts = await _context.Products
                .OrderBy(p => p.ProductId)
                .Take(5)
                .ToListAsync();

            var model = new HomeViewModel
            {
                FeaturedProducts = featuredProducts
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
