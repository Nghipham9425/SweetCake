using SweetCakeShop.Models;

namespace SweetCakeShop.Constants
{
    public static class OrderStatuses
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Shipped = "Shipped";
        public const string Delivered = "Delivered";
        public const string DeliveryFailed = "DeliveryFailed";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";

        /// <summary>EF-translatable statuses that count toward revenue.</summary>
        public static readonly string[] RevenueEligibleStatuses = [Confirmed, Completed];

        private static readonly HashSet<string> RevenueEligible = new(RevenueEligibleStatuses, StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string[]> AdminTransitions = new(StringComparer.OrdinalIgnoreCase)
        {
            [Pending] = [Confirmed, Cancelled],
            ["AwaitingPayment"] = [Cancelled],
            ["AwaitingConfirmation"] = [Confirmed, Cancelled],
            [Confirmed] = [Shipped, Cancelled],
            [DeliveryFailed] = [Shipped, Cancelled],
            [Delivered] = [Completed],
            [Completed] = [],
            [Cancelled] = []
        };

        private static readonly Dictionary<string, string[]> ShipperTransitions = new(StringComparer.OrdinalIgnoreCase)
        {
            [Shipped] = [Delivered, DeliveryFailed]
        };

        public static bool CountsForRevenue(string? status) =>
            !string.IsNullOrWhiteSpace(status) && RevenueEligible.Contains(status.Trim());

        public static IReadOnlyList<string> GetAdminNextStatuses(string? status) =>
            !string.IsNullOrWhiteSpace(status) && AdminTransitions.TryGetValue(status.Trim(), out var next)
                ? next
                : [];

        public static bool CanAdminTransition(string? fromStatus, string toStatus) =>
            GetAdminNextStatuses(fromStatus).Contains(toStatus, StringComparer.OrdinalIgnoreCase);

        public static bool CanShipperTransition(string? fromStatus, string toStatus) =>
            !string.IsNullOrWhiteSpace(fromStatus)
            && ShipperTransitions.TryGetValue(fromStatus.Trim(), out var next)
            && next.Contains(toStatus, StringComparer.OrdinalIgnoreCase);

        public static string GetDisplayName(string? status) => status switch
        {
            Pending => "Chờ xử lý",
            "AwaitingPayment" => "Chờ thanh toán",
            "AwaitingConfirmation" => "Chờ xác nhận",
            Confirmed => "Đã xác nhận",
            Shipped => "Đang giao",
            Delivered => "Đã giao",
            DeliveryFailed => "Không giao được",
            Completed => "Hoàn tất",
            Cancelled => "Đã hủy",
            _ => status ?? string.Empty
        };

        public static void ApplyStatus(Order order, string status)
        {
            if (string.Equals(status, Confirmed, StringComparison.OrdinalIgnoreCase))
            {
                ApplyConfirmed(order);
                return;
            }

            if (string.Equals(status, Delivered, StringComparison.OrdinalIgnoreCase))
            {
                ApplyDelivered(order);
                return;
            }

            if (string.Equals(status, DeliveryFailed, StringComparison.OrdinalIgnoreCase))
            {
                ApplyDeliveryFailed(order);
                return;
            }

            order.Status = status;
        }

        public static void ApplyConfirmed(Order order)
        {
            order.Status = Confirmed;
            order.ConfirmedAt = DateTime.Now;
        }

        public static void ApplyDelivered(Order order)
        {
            order.Status = Delivered;
        }

        public static void ApplyDeliveryFailed(Order order)
        {
            order.Status = DeliveryFailed;
        }
    }
}
