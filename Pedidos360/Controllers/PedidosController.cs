using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pedidos360.Areas.Identity.Data;
using Pedidos360.Data;
using Pedidos360.Models;
using System.Text.Json;

namespace Pedidos360.Controllers
{
    /// Módulo de Pedidos.
    /// Admin y Ventas: crear y ver pedidos.
    /// Operaciones: solo ver pedidos.
    [Authorize(Roles = "Admin,Ventas,Operaciones")]
    public class PedidosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PedidosController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 5) pageSize = 5;
            if (pageSize > 50) pageSize = 50;

            var query = _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Cliente)
                .Include(p => p.Usuario)
                .OrderByDescending(p => p.Fecha);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page     = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total    = total;

            return View(items);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Cliente)
                .Include(p => p.Usuario)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.PedidoId == id);

            if (pedido == null) return NotFound();

            return View(pedido);
        }

        [Authorize(Roles = "Admin,Ventas")]
        public async Task<IActionResult> Create()
        {
            await CargarClientesViewBag();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Ventas")]
        public async Task<IActionResult> Create(int clienteId, string lineasJson)
        {
            var cliente = await _context.Clientes.FindAsync(clienteId);
            if (cliente == null)
            {
                ModelState.AddModelError("clienteId", "El cliente seleccionado no existe.");
                await CargarClientesViewBag();
                return View();
            }

            List<LineaFormDto>? lineas = null;
            try
            {
                lineas = JsonSerializer.Deserialize<List<LineaFormDto>>(
                    lineasJson ?? "[]",
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                ModelState.AddModelError("", "Los datos del pedido son inválidos.");
                await CargarClientesViewBag();
                return View();
            }

            if (lineas == null || lineas.Count == 0)
            {
                ModelState.AddModelError("", "El pedido debe contener al menos un producto.");
                await CargarClientesViewBag();
                return View();
            }

            var productoIds = lineas.Select(l => l.ProductoId).Distinct().ToList();
            var productos   = await _context.Productos
                .Where(p => productoIds.Contains(p.ProductoId) && p.Activo)
                .ToListAsync();

            foreach (var linea in lineas)
            {
                var prod = productos.FirstOrDefault(p => p.ProductoId == linea.ProductoId);
                if (prod == null)
                {
                    ModelState.AddModelError("", $"El producto ID {linea.ProductoId} no existe o está inactivo.");
                    await CargarClientesViewBag();
                    return View();
                }
                if (linea.Cantidad <= 0)
                {
                    ModelState.AddModelError("", $"La cantidad del producto '{prod.Nombre}' debe ser mayor a 0.");
                    await CargarClientesViewBag();
                    return View();
                }
                if (prod.Stock < linea.Cantidad)
                {
                    ModelState.AddModelError("", $"Stock insuficiente para '{prod.Nombre}'. Disponible: {prod.Stock}.");
                    await CargarClientesViewBag();
                    return View();
                }
            }

            decimal subtotal  = 0m;
            decimal impuestos = 0m;
            var detalles = new List<PedidoDetalle>();

            foreach (var linea in lineas)
            {
                var prod = productos.First(p => p.ProductoId == linea.ProductoId);

                decimal descuento    = linea.Descuento < 0 ? 0 : linea.Descuento;
                decimal baseLinea    = (prod.Precio * linea.Cantidad) - descuento;
                if (baseLinea < 0) baseLinea = 0;
                decimal impuestoLin  = baseLinea * (prod.ImpuestoPorc / 100m);
                decimal totalLinea   = baseLinea + impuestoLin;

                subtotal  += baseLinea;
                impuestos += impuestoLin;

                detalles.Add(new PedidoDetalle
                {
                    ProductoId   = prod.ProductoId,
                    Cantidad     = linea.Cantidad,
                    PrecioUnit   = prod.Precio,
                    Descuento    = descuento,
                    ImpuestoPorc = prod.ImpuestoPorc,
                    TotalLinea   = Math.Round(totalLinea, 2)
                });

                prod.Stock -= linea.Cantidad;
            }

            var usuario = await _userManager.GetUserAsync(User);
            var pedido = new Pedido
            {
                ClienteId = clienteId,
                UsuarioId = usuario!.Id,
                Fecha     = DateTime.Now,
                Subtotal  = Math.Round(subtotal,  2),
                Impuestos = Math.Round(impuestos, 2),
                Total     = Math.Round(subtotal + impuestos, 2),
                Estado    = "Pendiente",
                Detalles  = detalles
            };

            _context.Pedidos.Add(pedido);

            try
            {
                await _context.SaveChangesAsync();
                TempData["Ok"] = $"Pedido #{pedido.PedidoId} creado correctamente.";
                return RedirectToAction(nameof(Details), new { id = pedido.PedidoId });
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "No se pudo guardar el pedido. Intente nuevamente.");
                await CargarClientesViewBag();
                return View();
            }
        }


        private async Task CargarClientesViewBag()
        {
            ViewBag.Clientes = await _context.Clientes
                .AsNoTracking()
                .OrderBy(c => c.Nombre)
                .Select(c => new SelectListItem
                {
                    Value = c.ClienteId.ToString(),
                    Text  = $"{c.Nombre} — {c.Cedula}"
                })
                .ToListAsync();
        }
    }
    internal class LineaFormDto
    {
        public int     ProductoId { get; set; }
        public int     Cantidad   { get; set; }
        public decimal Descuento  { get; set; } = 0;
    }
}
