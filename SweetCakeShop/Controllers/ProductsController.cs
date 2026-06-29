using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;
using SweetCakeShop.Helpers;
using SweetCakeShop.Models.ViewModels;
using SweetCakeShop.Services;
using X.PagedList;
using X.PagedList.Extensions;

namespace SweetCakeShop.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICustomerBehaviorService _behaviorService;
        private readonly IProductRecommendationService _recommendationService;

        public ProductsController(
            ApplicationDbContext context,
            ICustomerBehaviorService behaviorService,
            IProductRecommendationService recommendationService)
        {
            _context = context;
            _behaviorService = behaviorService;
            _recommendationService = recommendationService;
        }

        // GET: Products
        public async Task<IActionResult> Index(
            string? sortOrder,
            string? searchTerm,
            int? page,
            int? categoryId,
            string? categoryName,
            string? salesFilter)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentSearch"] = searchTerm;
            ViewData["CurrentCategoryId"] = categoryId;
            ViewData["CurrentCategoryName"] = categoryName;
            ViewData["CurrentSalesFilter"] = salesFilter;

            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
            ViewData["Categories"] = categories;

            var soldCounts = await _context.OrderDetails
                .AsNoTracking()
                .Where(detail => detail.Order != null
                    && OrderStatuses.RevenueEligibleStatuses.Contains(detail.Order.Status))
                .GroupBy(detail => detail.ProductId)
                .Select(group => new
                {
                    ProductId = group.Key,
                    SoldQuantity = group.Sum(detail => detail.Quantity)
                })
                .ToDictionaryAsync(item => item.ProductId, item => item.SoldQuantity);

            var products = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value);
            }

            var productResults = products.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                var categoryKeyword = categoryName.Trim();
                productResults = productResults.Where(p =>
                    p.Category != null && TextSearchHelper.ContainsNormalized(p.Category.CategoryName, categoryKeyword));
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var keyword = searchTerm.Trim();
                productResults = productResults.Where(p =>
                    TextSearchHelper.ContainsNormalized(p.ProductName, keyword)
                    || TextSearchHelper.ContainsNormalized(p.Description, keyword));
            }

            productResults = salesFilter switch
            {
                "sold" => productResults.Where(p => soldCounts.GetValueOrDefault(p.ProductId) > 0),
                "not_sold" => productResults.Where(p => soldCounts.GetValueOrDefault(p.ProductId) == 0),
                _ => productResults
            };

            productResults = sortOrder switch
            {
                "price_asc" => productResults.OrderBy(p => p.Price),
                "price_desc" => productResults.OrderByDescending(p => p.Price),
                "category_asc" => productResults
                    .OrderBy(p => p.Category?.CategoryName)
                    .ThenBy(p => p.ProductName),
                "best_selling" => productResults
                    .OrderByDescending(p => soldCounts.GetValueOrDefault(p.ProductId))
                    .ThenBy(p => p.ProductName),
                "least_selling" => productResults
                    .OrderBy(p => soldCounts.GetValueOrDefault(p.ProductId))
                    .ThenBy(p => p.ProductName),
                _ => productResults.OrderBy(p => p.ProductId)
            };

            int pageSize = 12;
            int pageNumber = page ?? 1;
            var pagedProducts = productResults.ToPagedList(pageNumber, pageSize);

            ViewData["SoldCounts"] = soldCounts;
            return View("IndexPro", pagedProducts);
        }

        [HttpGet]
        public Task<IActionResult> IndexPro(
            string? sortOrder,
            string? searchTerm,
            int? page,
            int? categoryId,
            string? categoryName,
            string? salesFilter)
        {
            return Index(sortOrder, searchTerm, page, categoryId, categoryName, salesFilter);
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

            await _behaviorService.TrackProductViewAsync(
                product.ProductId,
                product.ProductName,
                Request.Path + Request.QueryString);

            var similarProducts = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != product.ProductId)
                .OrderBy(p => p.ProductId)
                .Take(6)
                .ToListAsync();

            var model = new ProductDetailsViewModel
            {
                Product = product,
                SimilarProducts = similarProducts,
                Recommendations = await _recommendationService.GetForProductAsync(product.ProductId)
            };

            return View(model);
        }
    }
}
