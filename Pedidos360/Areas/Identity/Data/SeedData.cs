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

            // 1. Crear roles si no existen
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

            // 2. Usuarios de prueba: email, username, nombreCompleto, password, rol
            var seedUsers = new[]
            {
                ("admin@pedidos360.local",     "admin",    "Administrador",      "Admin123$",  "Admin"),
                ("ventas@pedidos360.local",    "ventas",   "Usuario de Ventas",  "Ventas123$", "Ventas"),
                ("ops@pedidos360.local",       "operaciones",      "Usuario Operaciones","Ops123$",    "Operaciones"),
            };

            foreach (var (email, username, nombre, password, rol) in seedUsers)
            {
                var user = await userManager.FindByNameAsync(username);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName       = username,
                        Email          = email,
                        EmailConfirmed = true,
                        NombreCompleto = nombre
                    };

                    var create = await userManager.CreateAsync(user, password);
                    if (!create.Succeeded)
                        throw new Exception($"No se pudo crear usuario '{username}': " +
                            string.Join(" | ", create.Errors.Select(e => e.Description)));
                }

                if (!await userManager.IsInRoleAsync(user, rol))
                {
                    var addRole = await userManager.AddToRoleAsync(user, rol);
                    if (!addRole.Succeeded)
                        throw new Exception($"No se pudo asignar rol '{rol}' a '{username}': " +
                            string.Join(" | ", addRole.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}