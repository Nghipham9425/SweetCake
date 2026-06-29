using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;
using SweetCakeShop.Models;
using SweetCakeShop.Models.ViewModels;

namespace SweetCakeShop.Services
{
    public class ProductRecommendationService : IProductRecommendationService
    {
        private const double MinConfidence = 0.10;

        private readonly ApplicationDbContext _db;

        public ProductRecommendationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<ProductRecommendationViewModel>> GetForProductAsync(int productId, int take = 4)
        {
            var transactions = await LoadTransactionsAsync();
            var rules = BuildPairRules(transactions)
                .Where(r => r.SourceProductId == productId)
                .OrderByDescending(r => r.Confidence)
                .ThenByDescending(r => r.Lift)
                .ThenByDescending(r => r.Support)
                .Take(take)
                .ToList();

            return await MapRulesAsync(rules, [productId], take);
        }

        public async Task<List<ProductRecommendationViewModel>> GetForCartAsync(IEnumerable<int> productIds, int take = 4)
        {
            var cartIds = productIds.Distinct().ToHashSet();
            if (cartIds.Count == 0)
                return [];

            var transactions = await LoadTransactionsAsync();
            var rules = BuildPairRules(transactions)
                .Where(r => cartIds.Contains(r.SourceProductId) && !cartIds.Contains(r.TargetProductId))
                .GroupBy(r => r.TargetProductId)
                .Select(g => g
                    .OrderByDescending(r => r.Confidence)
                    .ThenByDescending(r => r.Lift)
                    .First())
                .OrderByDescending(r => r.Confidence)
                .ThenByDescending(r => r.Lift)
                .ThenByDescending(r => r.Support)
                .Take(take)
                .ToList();

            return await MapRulesAsync(rules, cartIds, take);
        }

        private async Task<List<HashSet<int>>> LoadTransactionsAsync()
        {
            var orders = await _db.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails)
                .Where(o => OrderStatuses.RevenueEligibleStatuses.Contains(o.Status))
                .ToListAsync();

            return orders
                .Select(o => o.OrderDetails.Select(d => d.ProductId).Distinct().ToHashSet())
                .Where(items => items.Count >= 2)
                .ToList();
        }

        private static List<RecommendationRule> BuildPairRules(List<HashSet<int>> transactions)
        {
            if (transactions.Count == 0)
                return [];

            var totalTransactions = transactions.Count;
            var itemCounts = new Dictionary<int, int>();
            var pairCounts = new Dictionary<(int Source, int Target), int>();

            foreach (var transaction in transactions)
            {
                foreach (var productId in transaction)
                {
                    itemCounts[productId] = itemCounts.GetValueOrDefault(productId) + 1;
                }

                foreach (var source in transaction)
                {
                    foreach (var target in transaction)
                    {
                        if (source == target)
                            continue;

                        var key = (source, target);
                        pairCounts[key] = pairCounts.GetValueOrDefault(key) + 1;
                    }
                }
            }

            return pairCounts
                .Select(pair =>
                {
                    var sourceCount = itemCounts[pair.Key.Source];
                    var targetCount = itemCounts[pair.Key.Target];
                    var pairCount = pair.Value;
                    var support = (double)pairCount / totalTransactions;
                    var confidence = (double)pairCount / sourceCount;
                    var targetSupport = (double)targetCount / totalTransactions;
                    var lift = targetSupport > 0 ? confidence / targetSupport : 0;

                    return new RecommendationRule(
                        pair.Key.Source,
                        pair.Key.Target,
                        support,
                        confidence,
                        lift);
                })
                .Where(r => r.Confidence >= MinConfidence)
                .ToList();
        }

        private async Task<List<ProductRecommendationViewModel>> MapRulesAsync(
            List<RecommendationRule> rules,
            IEnumerable<int> excludeProductIds,
            int take)
        {
            var exclude = excludeProductIds.ToHashSet();
            var productIds = rules.Select(r => r.TargetProductId).ToList();
            if (productIds.Count == 0)
                return await GetBestSellingFallbackAsync(exclude, take);

            var products = await _db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => productIds.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId);

            var result = rules
                .Where(r => products.ContainsKey(r.TargetProductId))
                .Where(r => !exclude.Contains(r.TargetProductId))
                .Select(r => new ProductRecommendationViewModel
                {
                    Product = products[r.TargetProductId],
                    Support = r.Support,
                    Confidence = r.Confidence,
                    Lift = r.Lift,
                    Reason = "Khách thường mua kèm sản phẩm này"
                })
                .Take(take)
                .ToList();

            if (result.Count >= take)
                return result;

            foreach (var productId in result.Select(r => r.Product.ProductId))
            {
                exclude.Add(productId);
            }

            var fallback = await GetBestSellingFallbackAsync(exclude, take - result.Count);
            result.AddRange(fallback);
            return result;
        }

        private async Task<List<ProductRecommendationViewModel>> GetBestSellingFallbackAsync(IEnumerable<int> excludeProductIds, int take)
        {
            var exclude = excludeProductIds.ToHashSet();
            if (take <= 0)
                return [];

            var sold = await _db.OrderDetails
                .AsNoTracking()
                .Where(d => d.Order != null && OrderStatuses.RevenueEligibleStatuses.Contains(d.Order.Status))
                .GroupBy(d => d.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(d => d.Quantity) })
                .OrderByDescending(x => x.Quantity)
                .Take(20)
                .ToListAsync();

            var soldIds = sold.Select(x => x.ProductId).Where(id => !exclude.Contains(id)).Take(take).ToList();
            var products = await _db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => soldIds.Contains(p.ProductId))
                .ToListAsync();

            return products
                .Select(p => new ProductRecommendationViewModel
                {
                    Product = p,
                    Reason = "Sản phẩm đang được nhiều khách chọn"
                })
                .Take(take)
                .ToList();
        }

        private sealed record RecommendationRule(
            int SourceProductId,
            int TargetProductId,
            double Support,
            double Confidence,
            double Lift);
    }
}
