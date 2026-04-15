using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pedidos360.Data;
using Pedidos360.Models;

namespace Pedidos360.Controllers.Api
{
    [Route("api/categorias")]
    [ApiController]
    [Authorize(Roles = "Admin,Ventas")]
    public class CategoriasApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriasApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearCategoriaRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Nombre))
                return BadRequest(new { error = "El nombre de la categoría es requerido." });

            var nombre = request.Nombre.Trim();

            if (nombre.Length > 100)
                return BadRequest(new { error = "El nombre no puede exceder 100 caracteres." });

            if (await _context.Categorias.AnyAsync(c => c.Nombre == nombre))
                return BadRequest(new { error = "Ya existe una categoría con ese nombre." });

            var categoria = new Categoria { Nombre = nombre };
            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();

            return Ok(new { id = categoria.CategoriaId, nombre = categoria.Nombre });
        }
    }

    public class CrearCategoriaRequest
    {
        public string Nombre { get; set; } = string.Empty;
    }
}
