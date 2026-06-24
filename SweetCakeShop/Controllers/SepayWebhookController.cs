using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;
using SweetCakeShop.Helpers;
using SweetCakeShop.Models.Api;
using SweetCakeShop.Services;

namespace SweetCakeShop.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/sepay")]
    public class SepayWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrderInventoryService _inventoryService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SepayWebhookController> _logger;

        public SepayWebhookController(
            ApplicationDbContext context,
            IOrderInventoryService inventoryService,
            IConfiguration configuration,
            ILogger<SepayWebhookController> logger)
        {
            _context = context;
            _inventoryService = inventoryService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Receive([FromBody] SepayWebhookRequest payload)
        {
            _logger.LogInformation(
                "Received SePay webhook {TransactionId}. Type={TransferType}, Code={Code}, Amount={Amount}, Content={Content}, Description={Description}",
                payload.Id,
                payload.TransferType,
                payload.Code,
                payload.TransferAmount,
                payload.Content,
                payload.Description);

            if (!SepayOptions.IsValidWebhookAuthorization(_configuration, Request.Headers.Authorization.ToString()))
            {
                _logger.LogWarning("Rejected SePay webhook {TransactionId}: invalid Authorization header", payload.Id);
                return Unauthorized(new { success = false });
            }

            if (!string.Equals(payload.TransferType, "in", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new { success = true });
            }

            var order = await FindOrderAsync(payload);
            if (order == null)
            {
                _logger.LogWarning(
                    "SePay webhook {TransactionId} did not match an order. Code={Code}, Content={Content}",
                    payload.Id,
                    payload.Code,
                    payload.Content);
                return Ok(new { success = true });
            }

            var expectedAmount = decimal.ToInt64(decimal.Round(order.TotalPrice, 0, MidpointRounding.AwayFromZero));
            if (payload.TransferAmount < expectedAmount)
            {
                _logger.LogWarning(
                    "SePay webhook {TransactionId} amount mismatch for order {OrderId}. Received={Received}, Expected={Expected}",
                    payload.Id,
                    order.OrderId,
                    payload.TransferAmount,
                    expectedAmount);
                return Ok(new { success = true });
            }

            if (!OrderStatuses.CountsForRevenue(order.Status))
            {
                var stockCheck = await _inventoryService.ConfirmAndDeductAsync(order);
                if (!stockCheck.IsAvailable)
                {
                    _logger.LogWarning(
                        "SePay webhook {TransactionId} matched order {OrderId} but inventory was insufficient: {Message}",
                        payload.Id,
                        order.OrderId,
                        stockCheck.Message);
                }
            }

            return Ok(new { success = true });
        }

        private async Task<Models.Order?> FindOrderAsync(SepayWebhookRequest payload)
        {
            var prefix = (_configuration["SePay:PaymentCodePrefix"] ?? "SCS").Trim().ToUpperInvariant();
            var code = payload.Code;

            if (string.IsNullOrWhiteSpace(code))
                code = ExtractCodeFromContent(payload.Content, prefix);

            if (string.IsNullOrWhiteSpace(code))
                code = ExtractCodeFromContent(payload.Description, prefix);

            if (string.IsNullOrWhiteSpace(code))
                return null;

            if (!code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return null;

            var suffix = code[prefix.Length..];
            if (!int.TryParse(suffix, out var orderId))
                return null;

            return await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        private static string? ExtractCodeFromContent(string content, string prefix)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            var match = Regex.Match(
                content,
                $@"{Regex.Escape(prefix)}\s*([0-9]+)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            return match.Success
                ? $"{prefix}{match.Groups[1].Value}".ToUpperInvariant()
                : null;
        }
    }
}
