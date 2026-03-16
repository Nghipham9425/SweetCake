using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Models;

namespace SweetCakeShop.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

            // Dùng migration thay vì EnsureCreated
            context.Database.Migrate();

            Console.WriteLine("Bắt đầu seed dữ liệu mới...");

            // KHÔNG xóa dữ liệu cũ nữa
            // if (context.Products.Any()) { ... remove ... }

            if (!context.Categories.Any())
            {
                var categories = new Category[]
                {
                    new Category { CategoryName = "Bánh kem" },
                    new Category { CategoryName = "Bánh bông lan" },
                    new Category { CategoryName = "Bánh quy" },
                    new Category { CategoryName = "Bánh mì" },
                    new Category { CategoryName = "Bánh mousse" },
                    new Category { CategoryName = "Tiramisu" }
                };

                context.Categories.AddRange(categories);
                context.SaveChanges();
            }

            if (!context.Products.Any())
            {
                // seed products như hiện tại của bạn
                // context.Products.AddRange(products);
                // context.SaveChanges();
            }

            if (!context.Ingredients.Any())
            {
                // seed ingredients như hiện tại của bạn
                // context.Ingredients.AddRange(ingredients);
                // context.SaveChanges();
            }

            if (!context.Recipes.Any())
            {
                // seed recipes như hiện tại của bạn
                // context.Recipes.AddRange(recipes);
                // context.SaveChanges();
            }

            Console.WriteLine("Seed dữ liệu hoàn tất!");
        }
    }
}
