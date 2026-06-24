using Microsoft.AspNetCore.Identity;
using SweetCakeShop.Constants;

namespace SweetCakeShop.Data
{
    public static class IdentitySeed
    {
        public static async Task SeedAdminAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

            foreach (var roleName in Enum.GetNames<Roles>())
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            await EnsureUserInRoleAsync(
                userManager,
                email: "admin@gmail.com",
                password: "Admin@123",
                roleName: nameof(Roles.Admin));

            await EnsureUserInRoleAsync(
                userManager,
                email: "shipper@gmail.com",
                password: "Shipper@123",
                roleName: nameof(Roles.Shipper));
        }

        private static async Task EnsureUserInRoleAsync(
            UserManager<IdentityUser> userManager,
            string email,
            string password,
            string roleName)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"{roleName} user seed failed: {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(user, roleName))
            {
                await userManager.AddToRoleAsync(user, roleName);
            }
        }
    }
}
