using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pedidos360.Data;
using Pedidos360.Models.ViewModels;

namespace Pedidos360.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger  = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Usuario entró a Home/Index");

            if (User.Identity?.IsAuthenticated != true)
                return View(new DashboardVM());

            var vm = new DashboardVM
            {
                TotalPedidos       = await _context.Pedidos.CountAsync(),
                PedidosPendientes  = await _context.Pedidos.CountAsync(p => p.Estado == "Pendiente"),
                PedidosConfirmados = await _context.Pedidos.CountAsync(p => p.Estado == "Confirmado"),
                PedidosFacturados  = await _context.Pedidos.CountAsync(p => p.Estado == "Facturado"),
                TotalFacturado     = await _context.Pedidos
                                        .Where(p => p.Estado == "Facturado")
                                        .SumAsync(p => (decimal?)p.Total) ?? 0m,
                TotalClientes      = await _context.Clientes.CountAsync(),
                TotalProductos     = await _context.Productos.CountAsync(p => p.Activo),
                ProductosBajoStock = await _context.Productos.CountAsync(p => p.Activo && p.Stock <= 5),
                PedidosRecientes   = await _context.Pedidos
                                        .AsNoTracking()
                                        .Include(p => p.Cliente)
                                        .Include(p => p.Usuario)
                                        .OrderByDescending(p => p.Fecha)
                                        .Take(5)
                                        .ToListAsync()
            };

            return View(vm);
        }

        public IActionResult Error500()
        {
            _logger.LogError("Se produjo un error 500");
            return View();
        }

        public IActionResult Error404()
        {
            _logger.LogWarning("Página no encontrada (404)");
            return View();
        }

        public IActionResult ProbarError500()
        {
            throw new Exception("Error de prueba");
        }
    }
}
