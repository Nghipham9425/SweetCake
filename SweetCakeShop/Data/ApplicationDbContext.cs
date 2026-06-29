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
        public DbSet<CustomerChatMessage> CustomerChatMessages { get; set; }
        public DbSet<ProductPriceHistory> ProductPriceHistories { get; set; }
        public DbSet<CustomerBehaviorEvent> CustomerBehaviorEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Order.User is optional because guest checkout has no AspNetUsers row.
            builder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            builder.Entity<Order>()
                .Property(o => o.UserId)
                .HasMaxLength(450)
                .IsRequired(false);

            // set precision / column types to avoid silent truncation
            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            builder.Entity<Product>()
                .Property(p => p.CostPrice)
                .HasPrecision(18, 2);

            builder.Entity<Product>()
                .Property(p => p.DiscountPrice)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.TotalPrice)
                .HasPrecision(18, 2);

            builder.Entity<OrderDetail>()
                .Property(od => od.Price)
                .HasPrecision(18, 2);

            builder.Entity<OrderDetail>()
                .Property(od => od.CostPrice)
                .HasPrecision(18, 2);

            builder.Entity<ProductPriceHistory>(entity =>
            {
                entity.ToTable("ProductPriceHistories");
                entity.HasKey(h => h.ProductPriceHistoryId);
                entity.Property(h => h.OldPrice).HasPrecision(18, 2);
                entity.Property(h => h.NewPrice).HasPrecision(18, 2);
                entity.Property(h => h.OldCostPrice).HasPrecision(18, 2);
                entity.Property(h => h.NewCostPrice).HasPrecision(18, 2);
                entity.Property(h => h.OldDiscountPrice).HasPrecision(18, 2);
                entity.Property(h => h.NewDiscountPrice).HasPrecision(18, 2);
                entity.Property(h => h.ChangedBy).HasMaxLength(256);
                entity.HasOne(h => h.Product)
                    .WithMany(p => p.PriceHistories)
                    .HasForeignKey(h => h.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(h => h.ProductId);
                entity.HasIndex(h => h.ChangedAt);
            });

            builder.Entity<CustomerBehaviorEvent>(entity =>
            {
                entity.ToTable("CustomerBehaviorEvents");
                entity.HasKey(e => e.CustomerBehaviorEventId);
                entity.Property(e => e.EventType).HasMaxLength(32).IsRequired();
                entity.Property(e => e.ProductName).HasMaxLength(256).IsRequired();
                entity.Property(e => e.UserId).HasMaxLength(450);
                entity.Property(e => e.CustomerEmail).HasMaxLength(256);
                entity.Property(e => e.ChatToken).HasMaxLength(64);
                entity.Property(e => e.SessionId).HasMaxLength(128);
                entity.Property(e => e.PageUrl).HasMaxLength(1024);
                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.EventType);
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CustomerEmail);
                entity.HasIndex(e => e.ChatToken);
                entity.HasIndex(e => e.CreatedAt);
            });

            builder.Entity<Ingredient>()
                .Property(i => i.Quantity)
                .HasPrecision(10, 2);

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

            builder.Entity<CustomerChatMessage>(entity =>
            {
                entity.ToTable("CustomerChatMessages");
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Sender).HasMaxLength(16).IsRequired();
                entity.Property(m => m.Content).HasMaxLength(4000).IsRequired();
                entity.Property(m => m.ChatToken).HasMaxLength(64);
                entity.Property(m => m.UserId).HasMaxLength(450);
                entity.HasIndex(m => m.UserId);
                entity.HasIndex(m => m.ChatToken);
                entity.HasIndex(m => m.CreatedAt);
            });
        }
    }
}
