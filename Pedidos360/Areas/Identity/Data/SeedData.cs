using Microsoft.AspNetCore.Identity;

namespace Pedidos360.Areas.Identity.Data
{
    public static class SeedData
    {
        public static async Task EnsureSeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = { "Admin", "Ventas", "Operaciones" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var r = await roleManager.CreateAsync(new IdentityRole(role));
                    if (!r.Succeeded)
                        throw new Exception("No se pudo crear rol " + role + ": " +
                            string.Join(" | ", r.Errors.Select(e => e.Description)));
                }
            }

            var adminEmail = "admin@pedidos360.local";
            var adminUserName = "admin";

            var admin = await userManager.FindByNameAsync(adminUserName);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    NombreCompleto = "Administrador"
                };

                var create = await userManager.CreateAsync(admin, "Admin123$"); 
                if (!create.Succeeded)
                    throw new Exception("No se pudo crear admin: " +
                        string.Join(" | ", create.Errors.Select(e => e.Description)));
            }

            if (!await userManager.IsInRoleAsync(admin, "Admin"))
            {
                var addRole = await userManager.AddToRoleAsync(admin, "Admin");
                if (!addRole.Succeeded)
                    throw new Exception("No se pudo asignar rol Admin: " +
                        string.Join(" | ", addRole.Errors.Select(e => e.Description)));
            }
        }
    }
}
