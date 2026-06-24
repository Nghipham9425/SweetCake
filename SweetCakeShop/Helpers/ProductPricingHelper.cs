using SweetCakeShop.Models;

namespace SweetCakeShop.Helpers
{
    public static class ProductPricingHelper
    {
        public static bool HasActiveDiscount(Product product, DateTime? now = null)
        {
            if (!product.DiscountPrice.HasValue || product.DiscountPrice.Value <= 0)
                return false;

            if (product.DiscountPrice.Value >= product.Price)
                return false;

            var current = now ?? DateTime.UtcNow;
            if (product.DiscountStartAt.HasValue && product.DiscountStartAt.Value > current)
                return false;

            if (product.DiscountEndAt.HasValue && product.DiscountEndAt.Value < current)
                return false;

            return true;
        }

        public static decimal GetEffectivePrice(Product product, DateTime? now = null) =>
            HasActiveDiscount(product, now) ? product.DiscountPrice!.Value : product.Price;
    }
}
