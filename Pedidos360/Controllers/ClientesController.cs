using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pedidos360.Data;
using Pedidos360.Models;
using Pedidos360.Models.ViewModels;

namespace Pedidos360.Controllers
{
    [Authorize]
    public class ClientesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Clientes
        public async Task<IActionResult> Index(string? nombre, string? cedula, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 5) pageSize = 5;
            if (pageSize > 50) pageSize = 50;

            var query = _context.Clientes.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(nombre))
                query = query.Where(c => c.Nombre.Contains(nombre));

            if (!string.IsNullOrWhiteSpace(cedula))
                query = query.Where(c => c.Cedula.Contains(cedula));

            query = query.OrderBy(c => c.Nombre);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new ClientesIndexVM
            {
                Items = items,
                Nombre = nombre,
                Cedula = cedula,
                Page = page,
                PageSize = pageSize,
                TotalItems = total
            };

            return View(vm);
        }

        // GET: Clientes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var cliente = await _context.Clientes
                .AsNoTracking()
                .Include(c => c.Direcciones)
                .FirstOrDefaultAsync(c => c.ClienteId == id);

            if (cliente == null) return NotFound();

            return View(cliente);
        }

        // GET: Clientes/Create

        public IActionResult Create() => View();

        // POST: Clientes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Cedula,Correo,Telefono")] Cliente cliente)
        {
            if (!ModelState.IsValid) return View(cliente);

            // Duplicados (por índices únicos)
            if (await _context.Clientes.AnyAsync(c => c.Cedula == cliente.Cedula))
                ModelState.AddModelError(nameof(Cliente.Cedula), "Ya existe un cliente con esa cédula.");

            if (await _context.Clientes.AnyAsync(c => c.Correo == cliente.Correo))
                ModelState.AddModelError(nameof(Cliente.Correo), "Ya existe un cliente con ese correo.");

            if (!ModelState.IsValid) return View(cliente);

            _context.Clientes.Add(cliente);

            try
            {
                await _context.SaveChangesAsync();
                TempData["Ok"] = "Cliente creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "No se pudo guardar el cliente. Verifique datos duplicados e intente nuevamente.");
                return View(cliente);
            }
        }

        // GET: Clientes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();

            return View(cliente);
        }

        // POST: Clientes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ClienteId,Nombre,Cedula,Correo,Telefono")] Cliente cliente)
        {
            if (id != cliente.ClienteId) return NotFound();

            if (!ModelState.IsValid) return View(cliente);

            if (await _context.Clientes.AnyAsync(c => c.ClienteId != id && c.Cedula == cliente.Cedula))
                ModelState.AddModelError(nameof(Cliente.Cedula), "Ya existe otro cliente con esa cédula.");

            if (await _context.Clientes.AnyAsync(c => c.ClienteId != id && c.Correo == cliente.Correo))
                ModelState.AddModelError(nameof(Cliente.Correo), "Ya existe otro cliente con ese correo.");

            if (!ModelState.IsValid) return View(cliente);

            try
            {
                _context.Update(cliente);
                await _context.SaveChangesAsync();
                TempData["Ok"] = "Cliente actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Clientes.AnyAsync(e => e.ClienteId == id)) return NotFound();
                throw;
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "No se pudo actualizar el cliente. Verifique datos duplicados e intente nuevamente.");
                return View(cliente);
            }
        }

        // GET: Clientes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var cliente = await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ClienteId == id);

            if (cliente == null) return NotFound();

            return View(cliente);
        }

        // POST: Clientes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return RedirectToAction(nameof(Index));

            bool tienePedidos = await _context.Pedidos.AnyAsync(p => p.ClienteId == id);
            if (tienePedidos)
            {
                TempData["Err"] = "No se puede eliminar: el cliente tiene pedidos asociados.";
                return RedirectToAction(nameof(Index));
            }

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();

            TempData["Ok"] = "Cliente eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}
