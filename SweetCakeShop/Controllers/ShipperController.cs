using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;

namespace SweetCakeShop.Controllers
{
    [Authorize(Roles = "Shipper,Admin")]
    public class ShipperController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShipperController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.Status == OrderStatuses.Shipped
                    || o.Status == OrderStatuses.Delivered
                    || o.Status == OrderStatuses.DeliveryFailed)
                .OrderBy(o => o.Status == OrderStatuses.Shipped ? 0 : o.Status == OrderStatuses.DeliveryFailed ? 1 : 2)
                .ThenByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int orderId)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng";
                return RedirectToAction(nameof(Orders));
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDelivered(int orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng";
                return RedirectToAction(nameof(Orders));
            }

            if (!OrderStatuses.CanShipperTransition(order.Status, OrderStatuses.Delivered))
            {
                TempData["Error"] = $"Chỉ xác nhận đã giao cho đơn đang ở trạng thái {OrderStatuses.GetDisplayName(OrderStatuses.Shipped)}.";
                return RedirectToAction(nameof(Orders));
            }

            OrderStatuses.ApplyStatus(order, OrderStatuses.Delivered);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xác nhận giao thành công đơn #{order.OrderId}";
            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDeliveryFailed(int orderId, string? reason)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng";
                return RedirectToAction(nameof(Orders));
            }

            if (!OrderStatuses.CanShipperTransition(order.Status, OrderStatuses.DeliveryFailed))
            {
                TempData["Error"] = $"Chỉ báo không giao được cho đơn đang ở trạng thái {OrderStatuses.GetDisplayName(OrderStatuses.Shipped)}.";
                return RedirectToAction(nameof(Orders));
            }

            OrderStatuses.ApplyStatus(order, OrderStatuses.DeliveryFailed);
            await _context.SaveChangesAsync();

            var note = string.IsNullOrWhiteSpace(reason) ? string.Empty : $" Lý do: {reason.Trim()}";
            TempData["Warning"] = $"Đã ghi nhận không giao được đơn #{order.OrderId}.{note}";
            return RedirectToAction(nameof(Orders));
        }
    }
}
