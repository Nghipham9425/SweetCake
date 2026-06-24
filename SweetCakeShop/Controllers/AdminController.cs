using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;
using SweetCakeShop.Models;
using SweetCakeShop.Services;
namespace SweetCakeShop.Controllers
{
    [Authorize(Roles = nameof(Roles.Admin))]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IOrderInventoryService _inventoryService;

        public AdminController(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            IOrderInventoryService inventoryService)
        {
            _context = context;
            _env = env;
            _inventoryService = inventoryService;
        }

        public IActionResult Dashboard() => RedirectToAction("Index", "AdminDashboard");

        [HttpGet]
        public async Task<IActionResult> TopSellingProducts()
        {
            var ranking = await
                (from od in _context.OrderDetails.AsNoTracking()
                 join o in _context.Orders.AsNoTracking() on od.OrderId equals o.OrderId
                 join p in _context.Products.AsNoTracking() on od.ProductId equals p.ProductId
                 where o.ConfirmedAt != null && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status)
                 group new { od, o, p } by new { od.ProductId, p.ProductName } into g
                 orderby g.Sum(x => x.od.Quantity) descending, g.Key.ProductName
                 select new AdminTopSellingProductViewModel
                 {
                     ProductId = g.Key.ProductId,
                     ProductName = g.Key.ProductName,
                     SoldQuantity = g.Sum(x => x.od.Quantity),
                     TotalRevenue = g.Sum(x => x.od.Quantity * x.od.Price),
                     ConfirmedOrderCount = g.Select(x => x.o.OrderId).Distinct().Count()
                 })
                .ToListAsync();

            for (var i = 0; i < ranking.Count; i++)
            {
                ranking[i].Rank = i + 1;
            }

            return View(ranking);
        }

        #region Order Management
        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetails(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng";
                return RedirectToAction(nameof(Orders));
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransitionOrderStatus(int orderId, string toStatus)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng";
                return RedirectToAction(nameof(Orders));
            }

            if (!OrderStatuses.CanAdminTransition(order.Status, toStatus))
            {
                TempData["Error"] = $"Không thể chuyển đơn #{order.OrderId} từ {OrderStatuses.GetDisplayName(order.Status)} sang {OrderStatuses.GetDisplayName(toStatus)}";
                return RedirectToAction(nameof(Orders));
            }

            if (string.Equals(toStatus, OrderStatuses.Confirmed, StringComparison.OrdinalIgnoreCase))
            {
                var stockCheck = await _inventoryService.ConfirmAndDeductAsync(order);
                if (!stockCheck.IsAvailable)
                {
                    TempData["Error"] = stockCheck.Message;
                    return RedirectToAction(nameof(Orders));
                }
            }
            else if (string.Equals(toStatus, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            {
                await _inventoryService.CancelAndRestockAsync(order);
            }
            else
            {
                OrderStatuses.ApplyStatus(order, toStatus);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"Đã chuyển đơn #{order.OrderId} sang {OrderStatuses.GetDisplayName(order.Status)}";
            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng";
                return RedirectToAction(nameof(Orders));
            }

            if (order.OrderDetails.Any())
            {
                _context.OrderDetails.RemoveRange(order.OrderDetails);
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa đơn hàng #{orderId}";
            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeCake(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng";
                return RedirectToAction(nameof(Orders));
            }

            var stockCheck = await _inventoryService.ConfirmAndDeductAsync(order);
            if (!stockCheck.IsAvailable)
            {
                TempData["Warning"] = stockCheck.Message;
                return RedirectToAction(nameof(OrderDetails), new { orderId });
            }

            TempData["Success"] = $"Đã làm bánh cho đơn #{order.OrderId} và cập nhật trạng thái Confirmed";
            return RedirectToAction(nameof(OrderDetails), new { orderId });
        }
        #endregion

        #region Category Management
        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.CategoryId)
                .ToListAsync();

            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["Error"] = "Tên danh mục không được để trống";
                return RedirectToAction(nameof(Categories));
            }

            var exists = await _context.Categories
                .AnyAsync(c => c.CategoryName == categoryName.Trim());

            if (exists)
            {
                TempData["Error"] = "Danh mục đã tồn tại";
                return RedirectToAction(nameof(Categories));
            }

            _context.Categories.Add(new Category { CategoryName = categoryName.Trim() });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm danh mục thành công";
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCategory(int categoryId, string categoryName)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục";
                return RedirectToAction(nameof(Categories));
            }

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["Error"] = "Tên danh mục không được để trống";
                return RedirectToAction(nameof(Categories));
            }

            category.CategoryName = categoryName.Trim();
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật danh mục thành công";
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int categoryId)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

            if (category == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục";
                return RedirectToAction(nameof(Categories));
            }

            if (category.Products.Any())
            {
                TempData["Error"] = "Không thể xóa danh mục đang có sản phẩm";
                return RedirectToAction(nameof(Categories));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa danh mục thành công";
            return RedirectToAction(nameof(Categories));
        }
        #endregion

        #region Ingredients Management
        [HttpGet]
        public async Task<IActionResult> Ingredients()
        {
            var ingredients = await _context.Ingredients
                .OrderBy(i => i.Name)
                .ToListAsync();

            return View(ingredients);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateIngredient(string name, decimal quantity, string measurement)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Tên nguyên liệu không được để trống";
                return RedirectToAction(nameof(Ingredients));
            }

            if (quantity < 0)
            {
                TempData["Error"] = "Số lượng phải lớn hơn hoặc bằng 0";
                return RedirectToAction(nameof(Ingredients));
            }

            if (string.IsNullOrWhiteSpace(measurement))
            {
                TempData["Error"] = "Đơn vị đo không được để trống";
                return RedirectToAction(nameof(Ingredients));
            }

            var exists = await _context.Ingredients
                .AnyAsync(i => i.Name == name.Trim());

            if (exists)
            {
                TempData["Error"] = "Nguyên liệu đã tồn tại";
                return RedirectToAction(nameof(Ingredients));
            }

            _context.Ingredients.Add(new Ingredient
            {
                Name = name.Trim(),
                Quantity = quantity,
                Measurement = measurement.Trim()
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Thêm nguyên liệu thành công";
            return RedirectToAction(nameof(Ingredients));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateIngredient(
            int ingredientId,
            string name,
            string measurement,
            decimal addAmount = 0,
            decimal subtractAmount = 0)
        {
            var ingredient = await _context.Ingredients.FindAsync(ingredientId);
            if (ingredient == null)
            {
                TempData["Error"] = "Không tìm thấy nguyên liệu";
                return RedirectToAction(nameof(Ingredients));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Tên nguyên liệu không được để trống";
                return RedirectToAction(nameof(Ingredients));
            }

            if (string.IsNullOrWhiteSpace(measurement))
            {
                TempData["Error"] = "Đơn vị đo không được để trống";
                return RedirectToAction(nameof(Ingredients));
            }

            if (addAmount < 0 || subtractAmount < 0)
            {
                TempData["Error"] = "Giá trị Thêm/Trừ phải >= 0";
                return RedirectToAction(nameof(Ingredients));
            }

            addAmount = Math.Round(addAmount, 2, MidpointRounding.AwayFromZero);
            subtractAmount = Math.Round(subtractAmount, 2, MidpointRounding.AwayFromZero);

            var newQuantity = ingredient.Quantity + addAmount - subtractAmount;
            if (newQuantity < 0)
            {
                TempData["Error"] = "Số lượng không thể nhỏ hơn 0";
                return RedirectToAction(nameof(Ingredients));
            }

            ingredient.Name = name.Trim();
            ingredient.Measurement = measurement.Trim();
            ingredient.Quantity = Math.Round(newQuantity, 2, MidpointRounding.AwayFromZero);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật nguyên liệu thành công";
            return RedirectToAction(nameof(Ingredients));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteIngredient(int ingredientId)
        {
            var ingredient = await _context.Ingredients.FindAsync(ingredientId);
            if (ingredient == null)
            {
                TempData["Error"] = "Không tìm thấy nguyên liệu";
                return RedirectToAction(nameof(Ingredients));
            }

            var isUsedInRecipe = await _context.Recipes
                .AnyAsync(r => r.IngredientsID == ingredientId);

            if (isUsedInRecipe)
            {
                TempData["Error"] = "Không thể xóa nguyên liệu vì vẫn đang được dùng trong công thức bánh";
                return RedirectToAction(nameof(Ingredients));
            }

            _context.Ingredients.Remove(ingredient);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa nguyên liệu thành công";
            return RedirectToAction(nameof(Ingredients));
        }
        #endregion

        #region Product Management
        [HttpGet]
        public async Task<IActionResult> Products()
        {
            var products = await _context.Products
                .AsNoTracking()
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            ViewBag.Categories = categories;

            var recipes = await _context.Recipes
                .AsNoTracking()
                .Include(r => r.Ingredient)
                .ToListAsync();

            var recipeByProduct = recipes
                .GroupBy(r => r.ProductID)
                .ToDictionary(g => g.Key, g => g.ToList());

            var model = new List<AdminProductStockViewModel>();

            foreach (var product in products)
            {
                var row = new AdminProductStockViewModel
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName
                };

                if (recipeByProduct.TryGetValue(product.ProductId, out var productRecipes))
                {
                    row.Ingredients = productRecipes
                        .Where(r => r.Ingredient != null && r.Quantity > 0)
                        .Select(r => new AdminRecipeIngredientViewModel
                        {
                            IngredientId = r.IngredientsID,
                            IngredientName = r.Ingredient!.Name,
                            InStock = r.Ingredient!.Quantity,
                            RequiredPerCake = r.Quantity,
                            Measurement = r.Ingredient!.Measurement
                        })
                        .ToList();
                }

                if (row.Ingredients.Count == 0)
                {
                    row.HasEnoughIngredients = false;
                    row.CanMakeCount = 0;
                }
                else
                {
                    row.HasEnoughIngredients = row.Ingredients.All(i => i.InStock >= i.RequiredPerCake);
                    row.CanMakeCount = (int)Math.Floor(row.Ingredients.Min(i => i.InStock / i.RequiredPerCake));
                }

                model.Add(row);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditProductRecipe(int productId)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy bánh";
                return RedirectToAction(nameof(Products));
            }

            var recipeItems = await _context.Recipes
                .AsNoTracking()
                .Include(r => r.Ingredient)
                .Where(r => r.ProductID == productId)
                .OrderBy(r => r.Ingredient!.Name)
                .Select(r => new AdminProductRecipeItemViewModel
                {
                    RecipeId = r.RecipeID,
                    IngredientId = r.IngredientsID,
                    IngredientName = r.Ingredient!.Name,
                    Measurement = r.Ingredient!.Measurement,
                    Quantity = r.Quantity
                })
                .ToListAsync();

            var ingredientOptions = await _context.Ingredients
                .AsNoTracking()
                .OrderBy(i => i.Name)
                .Select(i => new IngredientOptionViewModel
                {
                    IngredientId = i.IngredientID,
                    Name = i.Name,
                    Measurement = i.Measurement
                })
                .ToListAsync();

            var model = new AdminEditProductRecipeViewModel
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                RecipeItems = recipeItems,
                IngredientOptions = ingredientOptions
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddIngredientToProduct(int productId, int ingredientId, decimal quantity)
        {
            if (quantity <= 0)
            {
                TempData["Error"] = "Số lượng phải lớn hơn 0";
                return RedirectToAction(nameof(EditProductRecipe), new { productId });
            }

            quantity = Math.Round(quantity, 2, MidpointRounding.AwayFromZero);

            var productExists = await _context.Products.AnyAsync(p => p.ProductId == productId);
            var ingredientExists = await _context.Ingredients.AnyAsync(i => i.IngredientID == ingredientId);

            if (!productExists || !ingredientExists)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ";
                return RedirectToAction(nameof(EditProductRecipe), new { productId });
            }

            var existing = await _context.Recipes
                .FirstOrDefaultAsync(r => r.ProductID == productId && r.IngredientsID == ingredientId);

            if (existing != null)
            {
                existing.Quantity = quantity; // nếu đã có thì cập nhật luôn
            }
            else
            {
                _context.Recipes.Add(new Recipe
                {
                    ProductID = productId,
                    IngredientsID = ingredientId,
                    Quantity = quantity
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã thêm/cập nhật nguyên liệu cho bánh";
            return RedirectToAction(nameof(EditProductRecipe), new { productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRecipeQuantity(int recipeId, int productId, decimal quantity)
        {
            var recipe = await _context.Recipes.FirstOrDefaultAsync(r => r.RecipeID == recipeId);
            if (recipe == null)
            {
                TempData["Error"] = "Không tìm thấy công thức";
                return RedirectToAction(nameof(EditProductRecipe), new { productId });
            }

            if (quantity <= 0)
            {
                TempData["Error"] = "Số lượng phải lớn hơn 0";
                return RedirectToAction(nameof(EditProductRecipe), new { productId });
            }

            recipe.Quantity = Math.Round(quantity, 2, MidpointRounding.AwayFromZero);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật định lượng thành công";
            return RedirectToAction(nameof(EditProductRecipe), new { productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveIngredientFromProduct(int recipeId, int productId)
        {
            var recipe = await _context.Recipes.FirstOrDefaultAsync(r => r.RecipeID == recipeId);
            if (recipe == null)
            {
                TempData["Error"] = "Không tìm thấy công thức";
                return RedirectToAction(nameof(EditProductRecipe), new { productId });
            }

            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa nguyên liệu khỏi công thức bánh";
            return RedirectToAction(nameof(EditProductRecipe), new { productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(
            string productName,
            decimal price,
            decimal costPrice,
            decimal? discountPrice,
            DateTime? discountStartAt,
            DateTime? discountEndAt,
            int categoryId,
            string? description,
            IFormFile? imageFile)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                TempData["Error"] = "Tên bánh không được để trống";
                return RedirectToAction(nameof(Products));
            }

            if (price < 0)
            {
                TempData["Error"] = "Giá phải >= 0";
                return RedirectToAction(nameof(Products));
            }

            if (costPrice < 0)
            {
                TempData["Error"] = "Giá vốn phải >= 0";
                return RedirectToAction(nameof(Products));
            }

            if (discountPrice.HasValue && (discountPrice.Value <= 0 || discountPrice.Value >= price))
            {
                TempData["Error"] = "Giá giảm phải lớn hơn 0 và nhỏ hơn giá bán";
                return RedirectToAction(nameof(Products));
            }

            if (discountStartAt.HasValue && discountEndAt.HasValue && discountEndAt.Value < discountStartAt.Value)
            {
                TempData["Error"] = "Ngày kết thúc giảm giá phải sau ngày bắt đầu";
                return RedirectToAction(nameof(Products));
            }

            var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryId == categoryId);
            if (!categoryExists)
            {
                TempData["Error"] = "Danh mục không hợp lệ";
                return RedirectToAction(nameof(Products));
            }

            var imagePath = await SaveProductImageAsync(imageFile);

            _context.Products.Add(new Product
            {
                ProductName = productName.Trim(),
                Price = price,
                CostPrice = costPrice,
                DiscountPrice = discountPrice,
                DiscountStartAt = discountStartAt,
                DiscountEndAt = discountEndAt,
                CategoryId = categoryId,
                Description = description?.Trim(),
                Image = imagePath
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Thêm bánh thành công";
            return RedirectToAction(nameof(Products));
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(int productId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy bánh";
                return RedirectToAction(nameof(Products));
            }

            ViewBag.Categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(
            int productId,
            string productName,
            decimal price,
            decimal costPrice,
            decimal? discountPrice,
            DateTime? discountStartAt,
            DateTime? discountEndAt,
            int categoryId,
            string? description,
            IFormFile? imageFile)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy bánh";
                return RedirectToAction(nameof(Products));
            }

            if (string.IsNullOrWhiteSpace(productName))
            {
                TempData["Error"] = "Tên bánh không được để trống";
                return RedirectToAction(nameof(EditProduct), new { productId });
            }

            if (price < 0)
            {
                TempData["Error"] = "Giá phải >= 0";
                return RedirectToAction(nameof(EditProduct), new { productId });
            }

            if (costPrice < 0)
            {
                TempData["Error"] = "Giá vốn phải >= 0";
                return RedirectToAction(nameof(EditProduct), new { productId });
            }

            if (discountPrice.HasValue && (discountPrice.Value <= 0 || discountPrice.Value >= price))
            {
                TempData["Error"] = "Giá giảm phải lớn hơn 0 và nhỏ hơn giá bán";
                return RedirectToAction(nameof(EditProduct), new { productId });
            }

            if (discountStartAt.HasValue && discountEndAt.HasValue && discountEndAt.Value < discountStartAt.Value)
            {
                TempData["Error"] = "Ngày kết thúc giảm giá phải sau ngày bắt đầu";
                return RedirectToAction(nameof(EditProduct), new { productId });
            }

            var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryId == categoryId);
            if (!categoryExists)
            {
                TempData["Error"] = "Danh mục không hợp lệ";
                return RedirectToAction(nameof(EditProduct), new { productId });
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                product.Image = await SaveProductImageAsync(imageFile);
            }

            var priceChanged = product.Price != price
                || product.CostPrice != costPrice
                || product.DiscountPrice != discountPrice
                || product.DiscountStartAt != discountStartAt
                || product.DiscountEndAt != discountEndAt;

            if (priceChanged)
            {
                _context.ProductPriceHistories.Add(new ProductPriceHistory
                {
                    ProductId = product.ProductId,
                    OldPrice = product.Price,
                    NewPrice = price,
                    OldCostPrice = product.CostPrice,
                    NewCostPrice = costPrice,
                    OldDiscountPrice = product.DiscountPrice,
                    NewDiscountPrice = discountPrice,
                    OldDiscountStartAt = product.DiscountStartAt,
                    NewDiscountStartAt = discountStartAt,
                    OldDiscountEndAt = product.DiscountEndAt,
                    NewDiscountEndAt = discountEndAt,
                    ChangedAt = DateTime.UtcNow,
                    ChangedBy = User.Identity?.Name
                });
            }

            product.ProductName = productName.Trim();
            product.Price = price;
            product.CostPrice = costPrice;
            product.DiscountPrice = discountPrice;
            product.DiscountStartAt = discountStartAt;
            product.DiscountEndAt = discountEndAt;
            product.CategoryId = categoryId;
            product.Description = description?.Trim();

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật bánh thành công";
            return RedirectToAction(nameof(Products));
        }

        [HttpGet]
        public async Task<IActionResult> ProductPriceHistory(int productId)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy bánh";
                return RedirectToAction(nameof(Products));
            }

            var history = await _context.ProductPriceHistories
                .AsNoTracking()
                .Where(h => h.ProductId == productId)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();

            ViewBag.Product = product;
            return View(history);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int productId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy bánh";
                return RedirectToAction(nameof(Products));
            }

            // chặn xóa nếu đã có trong đơn hàng
            var usedInOrders = await _context.OrderDetails.AnyAsync(od => od.ProductId == productId);
            if (usedInOrders)
            {
                TempData["Error"] = "Không thể xóa bánh vì đã tồn tại trong đơn hàng";
                return RedirectToAction(nameof(Products));
            }

            // xóa công thức trước
            var recipes = await _context.Recipes.Where(r => r.ProductID == productId).ToListAsync();
            if (recipes.Count > 0)
            {
                _context.Recipes.RemoveRange(recipes);
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa bánh thành công";
            return RedirectToAction(nameof(Products));
        }

        private async Task<string?> SaveProductImageAsync(IFormFile? imageFile)
        {
            if (imageFile == null || imageFile.Length == 0) return null;

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "products");
            Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(imageFile.FileName);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsFolder, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await imageFile.CopyToAsync(stream);

            return $"/uploads/products/{fileName}";
        }

        #endregion
    }
}
