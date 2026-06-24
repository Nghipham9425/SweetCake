using SweetCakeShop.Models;

namespace SweetCakeShop.Helpers
{
    public static class SepayOptions
    {
        public static string GetPaymentCode(Order order, IConfiguration configuration)
        {
            var prefix = configuration["SePay:PaymentCodePrefix"];
            if (string.IsNullOrWhiteSpace(prefix))
                prefix = "DH";

            var minDigits = 6;
            if (int.TryParse(configuration["SePay:PaymentCodeMinDigits"], out var configuredDigits)
                && configuredDigits > 0)
            {
                minDigits = configuredDigits;
            }

            return $"{prefix.Trim().ToUpperInvariant()}{order.OrderId.ToString().PadLeft(minDigits, '0')}";
        }

        public static string GetPaymentContent(Order order, IConfiguration configuration)
        {
            return $"{GetPaymentCode(order, configuration)} thanh toan don hang";
        }

        public static string BuildQrUrl(Order order, IConfiguration configuration)
        {
            var bank = configuration["SePay:BankCode"]
                ?? configuration["BankTransfer:BankName"]
                ?? "Vietcombank";
            var accountNumber = configuration["SePay:AccountNumber"]
                ?? configuration["BankTransfer:AccountNumber"]
                ?? string.Empty;
            var accountName = configuration["SePay:AccountName"]
                ?? configuration["BankTransfer:AccountName"]
                ?? "SWEET CAKE SHOP";
            var storeName = configuration["SePay:StoreName"] ?? "Sweet Cake Shop";
            var template = configuration["SePay:QrTemplate"] ?? "compact";
            var amount = decimal.ToInt64(decimal.Round(order.TotalPrice, 0, MidpointRounding.AwayFromZero));
            var description = GetPaymentContent(order, configuration);

            var query = new Dictionary<string, string?>
            {
                ["acc"] = accountNumber,
                ["bank"] = bank,
                ["amount"] = amount.ToString(),
                ["des"] = description,
                ["template"] = template,
                ["showinfo"] = "true",
                ["holder"] = accountName,
                ["store"] = storeName
            };

            var queryString = string.Join("&", query
                .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                .Select(item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value!)}"));

            return $"https://qr.sepay.vn/img?{queryString}";
        }

        public static bool IsValidWebhookAuthorization(IConfiguration configuration, string? authorizationHeader)
        {
            var apiKey = configuration["SePay:WebhookApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return true;

            return string.Equals(
                authorizationHeader?.Trim(),
                $"Apikey {apiKey.Trim()}",
                StringComparison.Ordinal);
        }
    }
}
