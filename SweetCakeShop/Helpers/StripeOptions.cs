namespace SweetCakeShop.Helpers
{
    public static class StripeOptions
    {
        public static string? GetSecretKey(IConfiguration configuration) =>
            configuration["Stripe:SecretKey"]
            ?? Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");

        public static bool IsConfigured(IConfiguration configuration)
        {
            var key = GetSecretKey(configuration);
            if (string.IsNullOrWhiteSpace(key))
                return false;

            // Bỏ qua placeholder trong appsettings (vd: xxxxxxxxxxxx)
            if (key.Contains('x', StringComparison.OrdinalIgnoreCase)
                && key.Replace("_", "").All(c => c == 'x' || c == 'X'))
                return false;

            return key.StartsWith("sk_test_", StringComparison.Ordinal)
                   || key.StartsWith("sk_live_", StringComparison.Ordinal);
        }

        public static string GenerateBankTransferCode(int orderId) =>
            $"BANK-{orderId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }
}
