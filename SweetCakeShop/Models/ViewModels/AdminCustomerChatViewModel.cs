using SweetCakeShop.Models;

namespace SweetCakeShop.Models.ViewModels
{
    public class AdminCustomerChatIndexViewModel
    {
        public List<CustomerChatThreadViewModel> Threads { get; set; } = new();
        public CustomerChatThreadViewModel? SelectedThread { get; set; }
        public List<CustomerChatMessage> Messages { get; set; } = new();
    }

    public class CustomerChatThreadViewModel
    {
        public string ThreadKey { get; set; } = string.Empty;
        public string CustomerLabel { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageAt { get; set; }
        public int MessageCount { get; set; }
        public bool NeedsSupport { get; set; }
    }
}
