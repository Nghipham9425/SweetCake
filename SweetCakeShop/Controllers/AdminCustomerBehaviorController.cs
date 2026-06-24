using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;
using SweetCakeShop.Models.ViewModels;
using SweetCakeShop.Services;

namespace SweetCakeShop.Controllers
{
    [Authorize(Roles = nameof(Roles.Admin))]
    public class AdminCustomerBehaviorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminCustomerBehaviorController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var viewed = await BuildSummaryAsync(CustomerBehaviorService.ViewProduct);
            var added = await BuildSummaryAsync(CustomerBehaviorService.AddToCart);

            var recent = await _context.CustomerBehaviorEvents
                .AsNoTracking()
                .OrderByDescending(e => e.CreatedAt)
                .Take(80)
                .Select(e => new CustomerBehaviorEventRowViewModel
                {
                    EventType = e.EventType,
                    ProductId = e.ProductId,
                    ProductName = e.ProductName,
                    Quantity = e.Quantity,
                    CustomerEmail = e.CustomerEmail,
                    CustomerKey = e.CustomerEmail ?? e.UserId ?? e.ChatToken ?? e.SessionId ?? "anonymous",
                    PageUrl = e.PageUrl,
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync();

            return View(new CustomerBehaviorDashboardViewModel
            {
                ViewedProducts = viewed,
                AddedToCartProducts = added,
                RecentEvents = recent
            });
        }

        private async Task<List<CustomerBehaviorProductSummaryViewModel>> BuildSummaryAsync(string eventType)
        {
            return await _context.CustomerBehaviorEvents
                .AsNoTracking()
                .Where(e => e.EventType == eventType)
                .GroupBy(e => new { e.ProductId, e.ProductName })
                .Select(g => new CustomerBehaviorProductSummaryViewModel
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    EventCount = g.Count(),
                    TotalQuantity = g.Sum(e => e.Quantity),
                    LastSeenAt = g.Max(e => e.CreatedAt)
                })
                .OrderByDescending(x => x.EventCount)
                .ThenByDescending(x => x.TotalQuantity)
                .Take(10)
                .ToListAsync();
        }
    }
}
