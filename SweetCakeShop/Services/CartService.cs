using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;  // hoặc System.Text.Json
using SweetCakeShop.Helpers;
using SweetCakeShop.Models;
using SweetCakeShop.Models.ViewModels;


namespace SweetCakeShop.Services
{
    public class CartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string CartSessionKey = "SweetCakeCart";

        public CartService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ISession Session => _httpContextAccessor.HttpContext!.Session;

        public CartViewModel GetCart()
        {
            var cartJson = Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(cartJson))
                return new CartViewModel();

            return JsonConvert.DeserializeObject<CartViewModel>(cartJson) ?? new CartViewModel();
        }

        public void SaveCart(CartViewModel cart)
        {
            var cartJson = JsonConvert.SerializeObject(cart);
            Session.SetString(CartSessionKey, cartJson);
        }

        public void AddToCart(Product product, int quantity = 1)
        {
            var cart = GetCart();
            var effectivePrice = ProductPricingHelper.GetEffectivePrice(product);

            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == product.ProductId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                existingItem.Price = effectivePrice;
                existingItem.OriginalPrice = product.Price;
                existingItem.CostPrice = product.CostPrice;
            }
            else
            {
                cart.Items.Add(new CartItemViewModel
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Price = effectivePrice,
                    OriginalPrice = product.Price,
                    CostPrice = product.CostPrice,
                    Image = product.Image,
                    Quantity = quantity
                });
            }

            SaveCart(cart);
        }

        public void UpdateQuantity(int productId, int quantity)
        {
            var cart = GetCart();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                if (quantity <= 0)
                    cart.Items.Remove(item);
                else
                    item.Quantity = quantity;

                SaveCart(cart);
            }
        }

        public void RemoveFromCart(int productId)
        {
            var cart = GetCart();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                cart.Items.Remove(item);
                SaveCart(cart);
            }
        }

        public void ClearCart()
        {
            Session.Remove(CartSessionKey);
        }
    }
}
