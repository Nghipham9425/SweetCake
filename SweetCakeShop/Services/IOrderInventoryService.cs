using SweetCakeShop.Models;
using SweetCakeShop.Models.ViewModels;

namespace SweetCakeShop.Services
{
    public interface IOrderInventoryService
    {
        Task<InventoryCheckResult> CheckCartAsync(CartViewModel cart);
        Task<InventoryCheckResult> CheckOrderAsync(Order order);
        Task<InventoryCheckResult> ConfirmAndDeductAsync(Order order);
        Task CancelAndRestockAsync(Order order);
    }

    public class InventoryCheckResult
    {
        public bool IsAvailable => Issues.Count == 0;
        public List<string> Issues { get; } = new();
        public string Message => IsAvailable ? "Đủ nguyên liệu" : string.Join(" ", Issues);

        public static InventoryCheckResult Success() => new();

        public static InventoryCheckResult Failed(params string[] issues)
        {
            var result = new InventoryCheckResult();
            result.Issues.AddRange(issues.Where(i => !string.IsNullOrWhiteSpace(i)));
            return result;
        }
    }
}
