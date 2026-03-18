using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pedidos360.Data;

namespace Pedidos360.Controllers.Api
{
    /// API para cálculo de totales de pedido en tiempo real (consumida via AJAX).
    /// Solo accesible por usuarios autenticados.
    [Route("api/pedidos")]
    [ApiController]
    [Authorize]
    public class PedidosApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PedidosApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// POST /api/pedidos/calcular
        /// Recibe las líneas del pedido y devuelve subtotal, impuestos y total calculados
        /// usando los precios e impuestos reales desde la base de datos.

        /// Body: [{ "productoId": 1, "cantidad": 2, "descuento": 0 }, ...]
        /// Response: { "subtotal": 1000.00, "impuestos": 130.00, "total": 1130.00 }
        /// Descuento es un monto fijo en moneda local que se resta a la línea completa.
        [HttpPost("calcular")]
        public async Task<IActionResult> Calcular([FromBody] List<LineaCalculoRequest>? lineas)
        {
            if (lineas == null || lineas.Count == 0)
                return Ok(new { subtotal = 0m, impuestos = 0m, total = 0m });

            if (lineas.Any(l => l.Cantidad <= 0))
                return BadRequest(new { error = "La cantidad debe ser mayor a 0." });

            if (lineas.Any(l => l.Descuento < 0 || l.Descuento > 100))
                return BadRequest(new { error = "El descuento debe estar entre 0 y 100%." });

            var productoIds = lineas.Select(l => l.ProductoId).Distinct().ToList();

            var productos = await _context.Productos
                .AsNoTracking()
                .Where(p => productoIds.Contains(p.ProductoId) && p.Activo)
                .Select(p => new { p.ProductoId, p.Precio, p.ImpuestoPorc })
                .ToListAsync();

            decimal subtotal  = 0m;
            decimal impuestos = 0m;

            foreach (var linea in lineas)
            {
                var producto = productos.FirstOrDefault(p => p.ProductoId == linea.ProductoId);
                if (producto == null)
                    return BadRequest(new { error = $"El producto con ID {linea.ProductoId} no existe o no está activo." });

                decimal brutoLinea = producto.Precio * linea.Cantidad;
                decimal baseLinea  = brutoLinea * (1 - linea.Descuento / 100m);

                decimal impuestoLinea = baseLinea * (producto.ImpuestoPorc / 100m);

                subtotal  += baseLinea;
                impuestos += impuestoLinea;
            }

            decimal total = subtotal + impuestos;

            return Ok(new
            {
                subtotal  = Math.Round(subtotal,  2),
                impuestos = Math.Round(impuestos, 2),
                total     = Math.Round(total,     2)
            });
        }
    }

    public class LineaCalculoRequest
    {
        public int     ProductoId { get; set; }
        public int     Cantidad   { get; set; }
        public decimal Descuento  { get; set; } = 0;
    }
}
