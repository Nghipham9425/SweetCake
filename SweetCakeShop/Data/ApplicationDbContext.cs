using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Models;

namespace SweetCakeShop.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<Recipe> Recipes { get; set; } // NEW

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // config relationship nếu cần
            builder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId);

            builder.Entity<Recipe>(entity =>
            {
                entity.ToTable("Recipe");

                entity.HasKey(r => r.RecipeID);

                entity.Property(r => r.Quantity)
                      .HasPrecision(10, 2);

                entity.HasOne(r => r.Product)
                    .WithMany()
                    .HasForeignKey(r => r.ProductID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Ingredient)
                    .WithMany()
                    .HasForeignKey(r => r.IngredientsID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(r => new { r.ProductID, r.IngredientsID }).IsUnique();
            });
        }
    }
}
