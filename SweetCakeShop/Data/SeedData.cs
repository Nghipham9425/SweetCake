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

            // Đảm bảo database đã được tạo
            context.Database.EnsureCreated();

            // XÓA DỮ LIỆU CŨ MỘT LẦN ĐỂ CẬP NHẬT ĐƯỜNG DẪN ẢNH
            if (context.Products.Any())
            {
                Console.WriteLine("Đang xóa dữ liệu cũ...");
                context.Products.RemoveRange(context.Products);
                context.Categories.RemoveRange(context.Categories);
                context.SaveChanges();
                Console.WriteLine("Đã xóa dữ liệu cũ.");
            }

            Console.WriteLine("Bắt đầu seed dữ liệu mới...");

            // Seed Categories
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

            Console.WriteLine($"Đã thêm {categories.Length} categories.");

            // Seed Products
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

                // Bánh mì
                new Product
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

                // Bánh mousse
                new Product
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

                // Tiramisu
                new Product
                {
                    ProductName = "Tiramisu truyền thống",
                    Price = 250000,
                    Description = "Tiramisu Ý truyền thống",
                    Image = "/images/sp13.jpg",
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

            context.Products.AddRange(products);
            context.SaveChanges();

            Console.WriteLine($"Đã thêm {products.Length} products.");
            Console.WriteLine("Seed dữ liệu hoàn tất!");
        }
    }
}
