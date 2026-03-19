using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pedidos360.Data;

namespace Pedidos360.Controllers.Api
{
    /// Solo accesible por usuarios autenticados.
    [Route("api/productos")]
    [ApiController]
    [Authorize]
    public class ProductosApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductosApiController(ApplicationDbContext context)
        {
            _context = context;
        }


        [HttpGet("buscar")]
        public async Task<IActionResult> Buscar([FromQuery] string? q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 1)
                return Ok(Array.Empty<object>());

            var resultados = await _context.Productos
                .AsNoTracking()
                .Where(p => p.Activo && p.Nombre.Contains(q))
                .OrderBy(p => p.Nombre)
                .Take(10)
                .Select(p => new
                {
                    id       = p.ProductoId,
                    nombre   = p.Nombre,
                    precio   = p.Precio,
                    impuesto = p.ImpuestoPorc,
                    stock    = p.Stock
                })
                .ToListAsync();

            return Ok(resultados);
        }
    }
}
