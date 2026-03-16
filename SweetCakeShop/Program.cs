using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;

namespace SweetCakeShop
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Seed database với dữ liệu mẫu
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    Console.WriteLine("Đang khởi tạo seed data...");
                    SeedData.Initialize(services);
                    IdentitySeed.SeedAdminAsync(services).GetAwaiter().GetResult();
                    Console.WriteLine("Seed data hoàn tất!");
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "LỖI KHI SEED DỮ LIỆU: {Message}", ex.Message);
                    Console.WriteLine($"LỖI: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    // Không ném lỗi ra ngoài để app vẫn chạy được
                }

            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();
            app.MapRazorPages().WithStaticAssets();

            app.Run();
        }
    }
}
