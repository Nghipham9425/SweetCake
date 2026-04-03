using System;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SweetCakeShop.Models;
using Stripe;
using Stripe.Checkout;
using System.Collections.Generic;

namespace SweetCakeShop.Services
{
    public class PaymentResult
    {
        public string PaymentCode { get; set; } = string.Empty;
        public string PaymentUrl { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool Success { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public string Name { get; set; }
    }

    public class PaymentService : IPaymentService
    {
        private readonly HttpClient? _http;
        private readonly ILogger<PaymentService> _logger;
        private readonly IConfiguration _configuration;

        public PaymentService(HttpClient? http, IConfiguration configuration, ILogger<PaymentService> logger)
        {
            _http = http;
            _configuration = configuration;
            _logger = logger;
        }

        // Create a Stripe Checkout Session and return the hosted checkout URL
        public async Task<PaymentResult> CreatePaymentAsync(Order order)
        {
            try
            {
                // Amount in smallest currency unit (VND uses whole integer)
                var amount = (long)order.TotalPrice;

                // Build the success and cancel URLs (redirect back to your app)
                var successUrl = _configuration["PaymentGateway:SuccessUrl"] ??
                                 $"https://{_configuration["ASPNETCORE_HOST"] ?? "localhost"}/Cart/PaymentCallback?orderId={order.OrderId}&status=success";
                var cancelUrl = _configuration["PaymentGateway:CancelUrl"] ??
                                $"https://{_configuration["ASPNETCORE_HOST"] ?? "localhost"}/Cart/Payment?orderId={order.OrderId}";

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    Mode = "payment",
                    LineItems = new List<SessionLineItemOptions>
                    {
                        // Use PriceData -> ProductData and UnitAmount/ Currency
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = "vnd",
                                UnitAmount = amount,
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = $"Đơn hàng #{order.OrderId}",
                                    Description = $"Thanh toán đơn hàng #{order.OrderId}"
                                }
                            },
                            Quantity = 1
                        }
                    },
                    CustomerEmail = string.IsNullOrWhiteSpace(order.CustomerEmail) ? null : order.CustomerEmail,
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                var result = new PaymentResult
                {
                    Success = true,
                    PaymentCode = session.Id ?? string.Empty,
                    PaymentUrl = session.Url ?? string.Empty,
                    Amount = order.TotalPrice,
                    Message = "Redirecting to Stripe Checkout"
                };

                _logger.LogInformation("Created Stripe session {SessionId} for order {OrderId}", session.Id, order.OrderId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Stripe Checkout Session for order {OrderId}", order.OrderId);

                // fallback: create local bank transfer instruction so UI stays usable
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var paymentCode = $"BANK-{order.OrderId}-{timestamp}";
                var message =
                    $"Không thể kết nối tới Stripe: {ex.Message}\n" +
                    $"Bạn có thể chuyển khoản thủ công. Mã: {paymentCode}";

                return new PaymentResult
                {
                    Success = false,
                    PaymentCode = paymentCode,
                    PaymentUrl = string.Empty,
                    Amount = order.TotalPrice,
                    Message = message
                };
            }
        }

        // Keep compatibility (not used here) – returns local bank instructions
        public Task<string> CreatePaymentRedirectUrlAsync(Order order, string returnUrl)
        {
            var providerBase = _configuration["PaymentGateway:ProviderUrl"] ?? "https://example-payment-provider.test";
            var url = $"{providerBase.TrimEnd('/')}/pay?orderId={order.OrderId}&amount={order.TotalPrice}&returnUrl={Uri.EscapeDataString(returnUrl)}";
            return Task.FromResult(url);
        }
    }
}
