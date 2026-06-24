using System.Data;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;
using SweetCakeShop.Models;
using SweetCakeShop.Models.ViewModels;

namespace SweetCakeShop.Services
{
    public class OrderInventoryService : IOrderInventoryService
    {
        private readonly ApplicationDbContext _db;

        public OrderInventoryService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<InventoryCheckResult> CheckCartAsync(CartViewModel cart)
        {
            var items = cart.Items
                .Where(i => i.Quantity > 0)
                .Select(i => new InventoryOrderItem(i.ProductId, i.ProductName, i.Quantity))
                .ToList();

            return await CheckItemsAsync(items);
        }

        public async Task<InventoryCheckResult> CheckOrderAsync(Order order)
        {
            await EnsureOrderDetailsLoadedAsync(order);

            var productNames = await _db.Products
                .AsNoTracking()
                .Where(p => order.OrderDetails.Select(d => d.ProductId).Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId, p => p.ProductName);

            var items = order.OrderDetails
                .Where(d => d.Quantity > 0)
                .Select(d => new InventoryOrderItem(
                    d.ProductId,
                    productNames.GetValueOrDefault(d.ProductId, $"SP #{d.ProductId}"),
                    d.Quantity))
                .ToList();

            return await CheckItemsAsync(items);
        }

        public async Task<InventoryCheckResult> ConfirmAndDeductAsync(Order order)
        {
            if (order.InventoryDeducted)
            {
                OrderStatuses.ApplyConfirmed(order);
                await _db.SaveChangesAsync();
                return InventoryCheckResult.Success();
            }

            await EnsureOrderDetailsLoadedAsync(order);

            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var check = await CheckOrderAsync(order);
            if (!check.IsAvailable)
            {
                await tx.RollbackAsync();
                return check;
            }

            var requiredByIngredient = await BuildRequiredByIngredientAsync(
                order.OrderDetails.Select(d => new InventoryOrderItem($"SP #{d.ProductId}", d.ProductId, d.Quantity)));

            var ingredientIds = requiredByIngredient.Keys.ToList();
            var ingredients = await _db.Ingredients
                .Where(i => ingredientIds.Contains(i.IngredientID))
                .ToListAsync();

            foreach (var ingredient in ingredients)
            {
                ingredient.Quantity = Math.Round(
                    ingredient.Quantity - requiredByIngredient[ingredient.IngredientID],
                    2,
                    MidpointRounding.AwayFromZero);
            }

            order.InventoryDeducted = true;
            OrderStatuses.ApplyConfirmed(order);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return InventoryCheckResult.Success();
        }

        public async Task CancelAndRestockAsync(Order order)
        {
            await EnsureOrderDetailsLoadedAsync(order);

            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            if (order.InventoryDeducted)
            {
                var requiredByIngredient = await BuildRequiredByIngredientAsync(
                    order.OrderDetails.Select(d => new InventoryOrderItem($"SP #{d.ProductId}", d.ProductId, d.Quantity)));

                var ingredientIds = requiredByIngredient.Keys.ToList();
                var ingredients = await _db.Ingredients
                    .Where(i => ingredientIds.Contains(i.IngredientID))
                    .ToListAsync();

                foreach (var ingredient in ingredients)
                {
                    ingredient.Quantity = Math.Round(
                        ingredient.Quantity + requiredByIngredient[ingredient.IngredientID],
                        2,
                        MidpointRounding.AwayFromZero);
                }

                order.InventoryDeducted = false;
            }

            OrderStatuses.ApplyStatus(order, OrderStatuses.Cancelled);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        private async Task<InventoryCheckResult> CheckItemsAsync(List<InventoryOrderItem> items)
        {
            if (items.Count == 0)
                return InventoryCheckResult.Failed("Giỏ hàng không có sản phẩm hợp lệ.");

            var requiredByIngredient = await BuildRequiredByIngredientAsync(items);
            if (_issues.Count > 0)
                return InventoryCheckResult.Failed(_issues.ToArray());

            var ingredientIds = requiredByIngredient.Keys.ToList();
            var ingredients = await _db.Ingredients
                .AsNoTracking()
                .Where(i => ingredientIds.Contains(i.IngredientID))
                .ToDictionaryAsync(i => i.IngredientID);

            var issues = new List<string>();

            foreach (var ingredientId in ingredientIds)
            {
                if (!ingredients.TryGetValue(ingredientId, out var ingredient))
                {
                    issues.Add($"Thiếu nguyên liệu #{ingredientId} trong kho.");
                    continue;
                }

                var required = requiredByIngredient[ingredientId];
                if (ingredient.Quantity < required)
                {
                    issues.Add($"Không đủ {ingredient.Name}: cần {required:0.##} {ingredient.Measurement}, còn {ingredient.Quantity:0.##} {ingredient.Measurement}.");
                }
            }

            return issues.Count == 0
                ? InventoryCheckResult.Success()
                : InventoryCheckResult.Failed(issues.ToArray());
        }

        private readonly List<string> _issues = new();

        private async Task<Dictionary<int, decimal>> BuildRequiredByIngredientAsync(IEnumerable<InventoryOrderItem> orderItems)
        {
            _issues.Clear();

            var items = orderItems
                .GroupBy(i => i.ProductId)
                .Select(g => new InventoryOrderItem(g.First().ProductName, g.Key, g.Sum(i => i.Quantity)))
                .ToList();

            var productIds = items.Select(i => i.ProductId).Distinct().ToList();
            var recipes = await _db.Recipes
                .AsNoTracking()
                .Where(r => productIds.Contains(r.ProductID))
                .ToListAsync();

            var recipesByProduct = recipes
                .Where(r => r.Quantity > 0)
                .GroupBy(r => r.ProductID)
                .ToDictionary(g => g.Key, g => g.ToList());

            var requiredByIngredient = new Dictionary<int, decimal>();

            foreach (var item in items)
            {
                if (!recipesByProduct.TryGetValue(item.ProductId, out var productRecipes) || productRecipes.Count == 0)
                {
                    _issues.Add($"{item.ProductName} chưa có công thức nguyên liệu.");
                    continue;
                }

                foreach (var recipe in productRecipes)
                {
                    var required = recipe.Quantity * item.Quantity;
                    requiredByIngredient[recipe.IngredientsID] =
                        requiredByIngredient.GetValueOrDefault(recipe.IngredientsID) + required;
                }
            }

            return requiredByIngredient;
        }

        private async Task EnsureOrderDetailsLoadedAsync(Order order)
        {
            if (order.OrderDetails.Count == 0)
            {
                await _db.Entry(order).Collection(o => o.OrderDetails).LoadAsync();
            }
        }

        private sealed record InventoryOrderItem(string ProductName, int ProductId, int Quantity)
        {
            public InventoryOrderItem(int productId, string productName, int quantity)
                : this(productName, productId, quantity)
            {
            }
        }
    }
}
