using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pedidos360.Areas.Identity.Data;
using Pedidos360.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

var mvc = builder.Services.AddControllersWithViews();

if (builder.Environment.IsDevelopment())
{
    mvc.AddRazorRuntimeCompilation();
}

builder.Services.AddRazorPages();

var app = builder.Build();

app.UseExceptionHandler("/Error/Index");
app.UseStatusCodePagesWithReExecute("/Error/Index", "?statusCode={0}");

await SeedData.EnsureSeedAsync(app.Services);

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Mapea controladores con rutas por atributo (API controllers)
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
