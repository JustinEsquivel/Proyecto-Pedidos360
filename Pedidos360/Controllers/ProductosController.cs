using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pedidos360.Data;
using Pedidos360.Models;
using Pedidos360.Models.ViewModels;

namespace Pedidos360.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // INDEX
        public async Task<IActionResult> Index(string? nombre, int? categoriaId, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 5) pageSize = 5;
            if (pageSize > 50) pageSize = 50;

            var query = _context.Productos.Include(p => p.Categoria).AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(nombre))
                query = query.Where(p => p.Nombre.Contains(nombre));

            if (categoriaId.HasValue)
                query = query.Where(p => p.CategoriaId == categoriaId.Value);

            query = query.OrderBy(p => p.Nombre);

            var total = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new ProductosIndexVM
            {
                Items = items,
                Nombre = nombre,
                Page = page,
                PageSize = pageSize,
                TotalItems = total
            };

            ViewBag.Categorias = await _context.Categorias
            .Select(c => new SelectListItem
            {
                Value = c.CategoriaId.ToString(),
                Text = c.Nombre
            })
            .ToListAsync();

            return View(vm);
        }

        // DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            
            var producto = await _context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.ProductoId == id);

            if (producto == null) return NotFound();
            
            return View(producto);
        }

        // CREATE
        public async Task<IActionResult> Create()
        {
            ViewBag.Categorias = await _context.Categorias
            .Select(c => new SelectListItem
            {
                Value = c.CategoriaId.ToString(),
                Text = c.Nombre
            })
            .ToListAsync();

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Precio,ImpuestoPorc,Stock,Activo,CategoriaId")] Producto producto, IFormFile? imagenFile)
        {
            if (!ModelState.IsValid)
                return AjaxModelStateErrorOrView(producto);

            if (await _context.Productos.AnyAsync(c => c.Nombre == producto.Nombre))
                ModelState.AddModelError(nameof(Producto.Nombre), "Ya existe otro producto con ese nombre.");


            if (!ModelState.IsValid)
                return AjaxModelStateErrorOrView(producto);

            if(imagenFile != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await imagenFile.CopyToAsync(memoryStream);
                    var imageBytes = memoryStream.ToArray();
                    producto.ImagenUrl = Convert.ToBase64String(imageBytes);
                }

            }

            _context.Productos.Add(producto);

            try
            {
                await _context.SaveChangesAsync();

                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        ok = true,
                        message = "Producto creado correctamente.",
                        redirectUrl = Url.Action(nameof(Index))
                    });
                }

                TempData["Ok"] = "Cliente creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "No se pudo guardar el producto.");
                return AjaxModelStateErrorOrView(producto);
            }
        }

        // EDIT
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            ViewBag.Categorias = await _context.Categorias
            .Select(c => new SelectListItem
            {
                Value = c.CategoriaId.ToString(),
                Text = c.Nombre
            })
            .ToListAsync();

            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductoId,Nombre,Precio,ImpuestoPorc,Stock,Activo,CategoriaId")] Producto producto, IFormFile? imagenFile)
        {
            if (id != producto.ProductoId) return NotFound();

            if (!ModelState.IsValid)
                return AjaxModelStateErrorOrView(producto);

            var productoDb = await _context.Productos.FindAsync(id);

            if (await _context.Productos.AnyAsync(p => p.ProductoId != id && p.Nombre == producto.Nombre))
                ModelState.AddModelError(nameof(Producto.Nombre), "Ya existe otro cliente con ese nombre.");


            if (!ModelState.IsValid)
                return AjaxModelStateErrorOrView(producto);

            ViewBag.Categorias = await _context.Categorias
            .Select(c => new SelectListItem
            {
                Value = c.CategoriaId.ToString(),
                Text = c.Nombre
            })
            .ToListAsync();

            // Actualizar campos normales
            productoDb.Nombre = producto.Nombre;
            productoDb.Precio = producto.Precio;
            productoDb.ImpuestoPorc = producto.ImpuestoPorc;
            productoDb.Stock = producto.Stock;
            productoDb.Activo = producto.Activo;
            productoDb.CategoriaId = producto.CategoriaId;

            // Solo actualizar imagen si subieron una nueva
            if (imagenFile != null && imagenFile.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await imagenFile.CopyToAsync(memoryStream);
                    productoDb.ImagenUrl = Convert.ToBase64String(memoryStream.ToArray());
                }
            }


            try
            {
                _context.Update(productoDb);
                await _context.SaveChangesAsync();

                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        ok = true,
                        message = "Producto actualizado correctamente.",
                        redirectUrl = Url.Action(nameof(Index))
                    });
                }

                TempData["Ok"] = "Producto actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Productos.AnyAsync(e => e.ProductoId == id))
                    return NotFound();

                throw;
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "No se pudo actualizar el cliente.");
                return AjaxModelStateErrorOrView(producto);
            }
        }

        // DELETE
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var cliente = await _context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(m => m.ProductoId == id);

            if (cliente == null) return NotFound();

            return View(cliente);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return RedirectToAction(nameof(Index));

            
            bool tienePedidos = await _context.PedidoDetalles.AnyAsync(p => p.ProductoId == id);
            

            if (tienePedidos)
            {
                TempData["Err"] = "No se puede eliminar: el producto tiene pedidos asociados.";
                return RedirectToAction(nameof(Index));
            }
            

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();

            TempData["Ok"] = "Producto eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // Helpers de AJAX
        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }

        private IActionResult AjaxModelStateErrorOrView(Producto producto)
        {
            if (IsAjaxRequest())
            {
                var errors = ModelState
                    .Where(ms => ms.Value != null && ms.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(new { ok = false, errors });
            }

            return View(producto);
        }
    }
}
