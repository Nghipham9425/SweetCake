using SweetCakeShop.Models;

namespace SweetCakeShop.Services
{
    public interface IPaymentService
    {
        Task<PaymentResult> CreatePaymentAsync(Order order);
    }
}
