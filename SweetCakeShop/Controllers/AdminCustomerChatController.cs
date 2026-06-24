using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;
using SweetCakeShop.Models;
using SweetCakeShop.Models.ViewModels;

namespace SweetCakeShop.Controllers
{
    [Authorize(Roles = nameof(Roles.Admin))]
    public class AdminCustomerChatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminCustomerChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? threadKey)
        {
            var allMessages = await _context.CustomerChatMessages
                .AsNoTracking()
                .Where(m => m.Sender != "system")
                .OrderByDescending(m => m.CreatedAt)
                .Take(500)
                .ToListAsync();

            var userIds = allMessages
                .Select(m => m.UserId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            var emailsByUserId = await _context.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Email);

            var threads = allMessages
                .GroupBy(GetThreadKey)
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .Select(g =>
                {
                    var ordered = g.OrderBy(m => m.CreatedAt).ToList();
                    var last = ordered.Last();
                    return new CustomerChatThreadViewModel
                    {
                        ThreadKey = g.Key,
                        CustomerEmail = !string.IsNullOrWhiteSpace(last.UserId) && emailsByUserId.TryGetValue(last.UserId, out var email)
                            ? email
                            : null,
                        CustomerLabel = BuildCustomerLabel(last, emailsByUserId),
                        LastMessage = last.Content,
                        LastMessageAt = last.CreatedAt,
                        MessageCount = ordered.Count,
                        NeedsSupport = ordered.Any(m => IsSupportRequest(m.Content))
                    };
                })
                .OrderByDescending(t => t.NeedsSupport)
                .ThenByDescending(t => t.LastMessageAt)
                .ToList();

            var selected = !string.IsNullOrWhiteSpace(threadKey)
                ? threads.FirstOrDefault(t => t.ThreadKey == threadKey)
                : threads.FirstOrDefault();

            var messages = new List<CustomerChatMessage>();
            if (selected != null)
            {
                messages = await QueryThread(selected.ThreadKey)
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();
            }

            return View(new AdminCustomerChatIndexViewModel
            {
                Threads = threads,
                SelectedThread = selected,
                Messages = messages
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(string threadKey, string message)
        {
            if (string.IsNullOrWhiteSpace(threadKey))
                return RedirectToAction(nameof(Index));

            if (string.IsNullOrWhiteSpace(message))
                return RedirectToAction(nameof(Index), new { threadKey });

            var sample = await QueryThread(threadKey).FirstOrDefaultAsync();
            if (sample == null)
                return RedirectToAction(nameof(Index));

            _context.CustomerChatMessages.Add(new CustomerChatMessage
            {
                UserId = sample.UserId,
                ChatToken = sample.UserId == null ? sample.ChatToken : null,
                Sender = "admin",
                Content = message.Trim(),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã gửi phản hồi CSKH.";

            return RedirectToAction(nameof(Index), new { threadKey });
        }

        private IQueryable<CustomerChatMessage> QueryThread(string threadKey)
        {
            if (threadKey.StartsWith("user:", StringComparison.OrdinalIgnoreCase))
            {
                var userId = threadKey["user:".Length..];
                return _context.CustomerChatMessages.Where(m => m.UserId == userId && m.Sender != "system");
            }

            if (threadKey.StartsWith("token:", StringComparison.OrdinalIgnoreCase))
            {
                var token = threadKey["token:".Length..];
                return _context.CustomerChatMessages.Where(m => m.ChatToken == token && m.Sender != "system");
            }

            return _context.CustomerChatMessages.Where(m => false);
        }

        private static string GetThreadKey(CustomerChatMessage message)
        {
            if (!string.IsNullOrWhiteSpace(message.UserId))
                return "user:" + message.UserId;
            if (!string.IsNullOrWhiteSpace(message.ChatToken))
                return "token:" + message.ChatToken;
            return string.Empty;
        }

        private static string BuildCustomerLabel(CustomerChatMessage message, Dictionary<string, string?> emailsByUserId)
        {
            if (!string.IsNullOrWhiteSpace(message.UserId))
            {
                return emailsByUserId.TryGetValue(message.UserId, out var email) && !string.IsNullOrWhiteSpace(email)
                    ? email
                    : "User " + Shorten(message.UserId);
            }

            if (!string.IsNullOrWhiteSpace(message.ChatToken))
                return "Guest " + Shorten(message.ChatToken);
            return "Anonymous";
        }

        private static string Shorten(string value) =>
            value.Length <= 10 ? value : value[..10] + "...";

        private static bool IsSupportRequest(string content) =>
            content.Contains("cskh", StringComparison.OrdinalIgnoreCase)
            || content.Contains("chăm sóc", StringComparison.OrdinalIgnoreCase)
            || content.Contains("nhân viên", StringComparison.OrdinalIgnoreCase)
            || content.Contains("support", StringComparison.OrdinalIgnoreCase);
    }
}
