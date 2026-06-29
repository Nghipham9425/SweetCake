using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Helpers;
using SweetCakeShop.Models;

namespace SweetCakeShop.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());
            // Use migrations to update database schema. EnsureCreated() bypasses migrations
            // and will break migration-based workflows. Use Migrate() so pending
            // migrations are applied automatically at startup.
            context.Database.Migrate();

            Console.WriteLine("Bắt đầu seed dữ liệu mới...");

            var categories = new Category[]
            {   
                new Category { CategoryName = "Bánh kem" },
                new Category { CategoryName = "Bánh bông lan" },
                new Category { CategoryName = "Bánh quy" },
                new Category { CategoryName = "Bánh mì" },
                new Category { CategoryName = "Bánh mousse" },
                new Category { CategoryName = "Tiramisu" }
            };

            if (!context.Categories.Any())
            {
                context.Categories.AddRange(categories);
                context.SaveChanges();
            }
            else
            {
                categories = context.Categories.ToArray();
            }

            var products = new Product[]
{
                // Bánh kem
                new Product
                {
                    ProductName = "Bánh kem dâu tây",
                    Price = 350000,
                    Description = "Bánh kem tươi với dâu tây tươi ngon",
                    Image = "/images/sp2.jpg",
                    CategoryId = categories[0].CategoryId
                },
                new Product
                {
                    ProductName = "Bánh kem socola",
                    Price = 380000,
                    Description = "Bánh kem socola Bỉ cao cấp",
                    Image = "/images/sp1.jpg",
                    CategoryId = categories[0].CategoryId
                },
                new Product
                {
                    ProductName = "Bánh kem vanilla",
                    Price = 320000,
                    Description = "Bánh kem vanilla cổ điển",
                    Image = "/images/sp3.jpg",
                    CategoryId = categories[0].CategoryId
                },
                new Product
                {
                    ProductName = "Bánh kem việt quất",
                    Price = 400000,
                    Description = "Bánh kem việt quất đỉnh và ngon điên",
                    Image = "/images/sp15.jpg",
                    CategoryId = categories[0].CategoryId
                },
                new Product
                {
                    ProductName = "Bánh kem thần tiên",
                    Price = 300000,
                    Description = "Bánh kem ăn vô như lạc vào chốn bồng lai",
                    Image = "/images/sp16.jpg",
                    CategoryId = categories[0].CategoryId
                },

                // Bánh bông lan
                new Product
                {
                    ProductName = "Bánh bông lan trứng muối",
                    Price = 120000,
                    Description = "Bánh bông lan trứng muối thơm ngon",
                    Image = "/images/sp4.jpg",
                    CategoryId = categories[1].CategoryId
                },
                new Product
                {
                    ProductName = "Bánh bông lan phô mai",
                    Price = 150000,
                    Description = "Bánh bông lan phô mai Nhật Bản",
                    Image = "/images/sp5.jpg",
                    CategoryId = categories[1].CategoryId
                },
                new Product
                {
                    ProductName = "Bánh bông lan truyền thống",
                    Price = 90000,
                    Description = "Bánh bông lan theo công thức truyền thống",
                    Image = "/images/sp6.jpg",
                    CategoryId = categories[1].CategoryId
                },

                // Bánh quy
                new Product
                {
                    ProductName = "Bánh quy bơ",
                    Price = 80000,
                    Description = "Bánh quy bơ thơm ngon giòn tan",
                    Image = "/images/sp7.jpg",
                    CategoryId = categories[2].CategoryId
                },
                new Product
                {
                    ProductName = "Bánh quy socola chip",
                    Price = 95000,
                    Description = "Bánh quy với socola chip cao cấp",
                    Image = "/images/sp8.jpg",
                    CategoryId = categories[2].CategoryId
                },
                new Product
                {
                    ProductName = "Bánh quy socola bảy màu",
                    Price = 100000,
                    Description = "Bánh quy với socola với nhiều màu sắc cao cấp",
                    Image = "/images/sp18.jpg",
                    CategoryId = categories[2].CategoryId
                },new Product
                {
                    ProductName = "Bánh mì baguette",
                    Price = 25000,
                    Description = "Bánh mì baguette truyền thống Pháp",
                    Image = "/images/sp9.jpg",
                    CategoryId = categories[3].CategoryId
                },
                new Product
                {
                    ProductName = "Bánh mì sandwich",
                    Price = 30000,
                    Description = "Bánh mì sandwich mềm mịn",
                    Image = "/images/sp10.jpg",
                    CategoryId = categories[3].CategoryId
                },
                new Product
                {
                    ProductName = "Bánh mì bơ tỏi",
                    Price = 35000,
                    Description = "Bánh mì bơ tỏi thơm phức giòn rụm",
                    Image = "/images/sp20.jpg",
                    CategoryId = categories[3].CategoryId
                },new Product
                {
                    ProductName = "Mousse socola",
                    Price = 180000,
                    Description = "Mousse socola Bỉ thơm ngon",
                    Image = "/images/sp11.jpg",
                    CategoryId = categories[4].CategoryId
                },
                new Product
                {
                    ProductName = "Mousse dâu tây",
                    Price = 170000,
                    Description = "Mousse dâu tây tươi mát",
                    Image = "/images/sp12.jpg",
                    CategoryId = categories[4].CategoryId
                },
                new Product
                {
                    ProductName = "Mousse xoài",
                    Price = 200000,
                    Description = "Mousse xoài tươi mát chua chua ngọt ngọt phù hợp cho mùa hè",
                    Image = "/images/sp19.jpg",
                    CategoryId = categories[4].CategoryId
                },new Product
                {
                    ProductName = "Tiramisu truyền thống",
                    Price = 250000,
                    Description = "Tiramisu Ý truyền thống",
                    Image = "/images/sp13.jpg",
                    CategoryId = categories[5].CategoryId
                },
                new Product
                {
                    ProductName = "Tiramisu Mango",
                    Price = 260000,
                    Description = "Tiramisu xoài tạo nên mùi vị chua ngọt thoải mái",
                    Image = "/images/sp17.jpg",
                    CategoryId = categories[5].CategoryId
                },
                new Product
                {
                    ProductName = "Tiramisu matcha",
                    Price = 280000,
                    Description = "Tiramisu với matcha Nhật Bản",
                    Image = "/images/sp14.jpg",
                    CategoryId = categories[5].CategoryId
                }
            };

            if (!context.Products.Any())
            {
                context.Products.AddRange(products);
                context.SaveChanges();
            }
            else
            {
                products = context.Products.ToArray();
            }

            var ingredients = new Ingredient[]
            {
                new Ingredient { Name = "Bột mì", Quantity = 50000, Measurement = "gram" },
                new Ingredient { Name = "Trứng gà", Quantity = 1000, Measurement = "quả" },
                new Ingredient { Name = "Đường", Quantity = 30000, Measurement = "gram" },
                new Ingredient { Name = "Sữa tươi", Quantity = 20000, Measurement = "ml" },
                new Ingredient { Name = "Bơ lạt", Quantity = 15000, Measurement = "gram" },
                new Ingredient { Name = "Kem tươi", Quantity = 12000, Measurement = "ml" },
                new Ingredient { Name = "Socola", Quantity = 10000, Measurement = "gram" },
                new Ingredient { Name = "Dâu tây", Quantity = 8000, Measurement = "gram" },
                new Ingredient { Name = "Phô mai", Quantity = 7000, Measurement = "gram" },
                new Ingredient { Name = "Bột matcha", Quantity = 2000, Measurement = "gram" }
            };

            if (!context.Ingredients.Any())
            {
                context.Ingredients.AddRange(ingredients);
                context.SaveChanges();
            }
            else
            {
                ingredients = context.Ingredients.ToArray();
            }

            if (!context.Recipes.Any())
            {
                int ProductId(string name) => products.First(p => p.ProductName == name).ProductId;
                int IngredientId(string name) => ingredients.First(i => i.Name == name).IngredientID;

                var recipes = new Recipe[]
                {
                    // Bánh kem dâu tây
                    new Recipe { ProductID = ProductId("Bánh kem dâu tây"), IngredientsID = IngredientId("Bột mì"), Quantity = 300 },
                    new Recipe { ProductID = ProductId("Bánh kem dâu tây"), IngredientsID = IngredientId("Trứng gà"), Quantity = 6 },
                    new Recipe { ProductID = ProductId("Bánh kem dâu tây"), IngredientsID = IngredientId("Đường"), Quantity = 120 },
                    new Recipe { ProductID = ProductId("Bánh kem dâu tây"), IngredientsID = IngredientId("Kem tươi"), Quantity = 250 },
                    new Recipe { ProductID = ProductId("Bánh kem dâu tây"), IngredientsID = IngredientId("Dâu tây"), Quantity = 200 },

                    // Bánh kem socola
                    new Recipe { ProductID = ProductId("Bánh kem socola"), IngredientsID = IngredientId("Bột mì"), Quantity = 320 },
                    new Recipe { ProductID = ProductId("Bánh kem socola"), IngredientsID = IngredientId("Trứng gà"), Quantity = 6 },
                    new Recipe { ProductID = ProductId("Bánh kem socola"), IngredientsID = IngredientId("Đường"), Quantity = 130 },
                    new Recipe { ProductID = ProductId("Bánh kem socola"), IngredientsID = IngredientId("Socola"), Quantity = 180 },
                    new Recipe { ProductID = ProductId("Bánh kem socola"), IngredientsID = IngredientId("Kem tươi"), Quantity = 220 },

                    // Tiramisu matcha
                    new Recipe { ProductID = ProductId("Tiramisu matcha"), IngredientsID = IngredientId("Phô mai"), Quantity = 250 },
                    new Recipe { ProductID = ProductId("Tiramisu matcha"), IngredientsID = IngredientId("Kem tươi"), Quantity = 180 },
                    new Recipe { ProductID = ProductId("Tiramisu matcha"), IngredientsID = IngredientId("Đường"), Quantity = 90 },
                    new Recipe { ProductID = ProductId("Tiramisu matcha"), IngredientsID = IngredientId("Bột matcha"), Quantity = 25 }
                };

                context.Recipes.AddRange(recipes);
                context.SaveChanges();
            }

            var productIdsWithRecipes = context.Recipes
                .Select(r => r.ProductID)
                .Distinct()
                .ToHashSet();

            var fallbackIngredients = context.Ingredients
                .OrderBy(i => i.IngredientID)
                .Take(3)
                .ToList();

            if (fallbackIngredients.Count > 0)
            {
                var fallbackRecipes = new List<Recipe>();
                var productsWithoutRecipes = context.Products
                    .Where(p => !productIdsWithRecipes.Contains(p.ProductId))
                    .ToList();

                foreach (var product in productsWithoutRecipes)
                {
                    fallbackRecipes.Add(new Recipe
                    {
                        ProductID = product.ProductId,
                        IngredientsID = fallbackIngredients[0].IngredientID,
                        Quantity = 100
                    });

                    if (fallbackIngredients.Count > 1)
                    {
                        fallbackRecipes.Add(new Recipe
                        {
                            ProductID = product.ProductId,
                            IngredientsID = fallbackIngredients[1].IngredientID,
                            Quantity = 1
                        });
                    }

                    if (fallbackIngredients.Count > 2)
                    {
                        fallbackRecipes.Add(new Recipe
                        {
                            ProductID = product.ProductId,
                            IngredientsID = fallbackIngredients[2].IngredientID,
                            Quantity = 50
                        });
                    }
                }

                if (fallbackRecipes.Count > 0)
                {
                    context.Recipes.AddRange(fallbackRecipes);
                    context.SaveChanges();
                }
            }

            var ordersNeedingConfirmedAt = context.Orders
                .Where(o => o.ConfirmedAt == null && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status))
                .ToList();

            if (ordersNeedingConfirmedAt.Count > 0)
            {
                foreach (var order in ordersNeedingConfirmedAt)
                {
                    order.ConfirmedAt = order.OrderDate;
                }

                context.SaveChanges();
            }

            SeedRecommendationDemoOrders(context);

            Console.WriteLine("Seed dữ liệu hoàn tất!");
        }
        private static void SeedRecommendationDemoOrders(ApplicationDbContext context)
        {
            const string demoEmail = "recommend-demo@sweetcake.local";
            if (context.Orders.Any(o => o.CustomerEmail == demoEmail))
                return;

            var products = context.Products.ToDictionary(p => p.ProductName);
            var orderDate = DateTime.UtcNow.AddDays(-45);

            var baskets = new[]
            {
                new[] { "Bánh kem dâu tây", "Mousse dâu tây", "Bánh quy bơ" },
                new[] { "Bánh kem dâu tây", "Mousse dâu tây", "Bánh bông lan phô mai" },
                new[] { "Bánh kem dâu tây", "Bánh quy bơ", "Bánh quy socola chip" },
                new[] { "Bánh kem dâu tây", "Mousse dâu tây", "Tiramisu truyền thống" },
                new[] { "Bánh kem dâu tây", "Bánh bông lan trứng muối", "Bánh quy bơ" },
                new[] { "Bánh kem socola", "Mousse socola", "Bánh quy socola chip" },
                new[] { "Bánh kem socola", "Mousse socola", "Tiramisu matcha" },
                new[] { "Bánh kem socola", "Bánh quy socola chip", "Bánh quy socola bảy màu" },
                new[] { "Bánh kem socola", "Mousse socola", "Bánh bông lan phô mai" },
                new[] { "Bánh kem vanilla", "Bánh bông lan truyền thống", "Bánh quy bơ" },
                new[] { "Bánh kem vanilla", "Bánh bông lan phô mai", "Bánh mì sandwich" },
                new[] { "Bánh kem việt quất", "Mousse xoài", "Tiramisu Mango" },
                new[] { "Bánh kem việt quất", "Mousse dâu tây", "Bánh quy bơ" },
                new[] { "Bánh bông lan trứng muối", "Bánh mì bơ tỏi", "Bánh mì baguette" },
                new[] { "Bánh bông lan trứng muối", "Bánh quy bơ", "Bánh mì bơ tỏi" },
                new[] { "Bánh bông lan phô mai", "Tiramisu matcha", "Bánh quy socola chip" },
                new[] { "Bánh bông lan truyền thống", "Bánh mì sandwich", "Bánh quy bơ" },
                new[] { "Bánh quy bơ", "Bánh mì baguette", "Bánh mì bơ tỏi" },
                new[] { "Bánh quy socola chip", "Mousse socola", "Tiramisu matcha" },
                new[] { "Bánh quy socola bảy màu", "Bánh kem thần tiên", "Mousse xoài" },
                new[] { "Bánh mì baguette", "Bánh mì sandwich", "Bánh mì bơ tỏi" },
                new[] { "Bánh mì sandwich", "Bánh bông lan truyền thống", "Bánh quy bơ" },
                new[] { "Mousse socola", "Tiramisu matcha", "Bánh quy socola chip" },
                new[] { "Mousse dâu tây", "Bánh kem dâu tây", "Tiramisu truyền thống" },
                new[] { "Mousse xoài", "Tiramisu Mango", "Bánh kem việt quất" },
                new[] { "Tiramisu truyền thống", "Bánh kem dâu tây", "Bánh quy bơ" },
                new[] { "Tiramisu Mango", "Mousse xoài", "Bánh kem việt quất" },
                new[] { "Tiramisu matcha", "Bánh bông lan phô mai", "Mousse socola" },
                new[] { "Bánh kem thần tiên", "Bánh quy socola bảy màu", "Mousse xoài" },
                new[] { "Bánh kem thần tiên", "Bánh kem vanilla", "Bánh quy bơ" },
                new[] { "Bánh kem dâu tây", "Mousse dâu tây", "Bánh quy bơ", "Tiramisu truyền thống" },
                new[] { "Bánh kem socola", "Mousse socola", "Bánh quy socola chip", "Tiramisu matcha" },
                new[] { "Bánh mì baguette", "Bánh mì sandwich", "Bánh mì bơ tỏi", "Bánh bông lan truyền thống" },
                new[] { "Mousse xoài", "Tiramisu Mango", "Bánh kem việt quất", "Bánh quy bơ" },
                new[] { "Bánh bông lan trứng muối", "Bánh quy bơ", "Bánh mì bơ tỏi", "Mousse dâu tây" }
            };

            var orders = new List<Order>();

            for (var i = 0; i < baskets.Length; i++)
            {
                var order = new Order
                {
                    UserId = null,
                    CustomerName = $"Khach demo {i + 1:00}",
                    CustomerEmail = demoEmail,
                    CustomerPhone = $"090000{i + 1:0000}",
                    ShippingAddress = $"Dia chi demo {i + 1}, TP.HCM",
                    IsGuest = true,
                    OrderDate = orderDate.AddDays(i),
                    ConfirmedAt = orderDate.AddDays(i).AddMinutes(20),
                    Status = i % 5 == 0 ? OrderStatuses.Completed : OrderStatuses.Confirmed,
                    InventoryDeducted = true
                };

                foreach (var productName in baskets[i])
                {
                    if (!products.TryGetValue(productName, out var product))
                        continue;

                    var quantity = product.Price < 100000 ? 2 : 1;
                    var price = ProductPricingHelper.GetEffectivePrice(product);

                    order.OrderDetails.Add(new OrderDetail
                    {
                        ProductId = product.ProductId,
                        Quantity = quantity,
                        Price = price,
                        CostPrice = product.CostPrice
                    });
                }

                order.TotalPrice = order.OrderDetails.Sum(d => d.Price * d.Quantity);
                if (order.OrderDetails.Count > 0)
                    orders.Add(order);
            }

            context.Orders.AddRange(orders);
            context.SaveChanges();
        }
    }
}
