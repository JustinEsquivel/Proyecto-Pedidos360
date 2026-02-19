using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pedidos360.Areas.Identity.Data;
using Pedidos360.Models.ViewModels;

namespace Pedidos360.Controllers;

[Authorize(Roles = "Admin")]
public class UsuariosController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsuariosController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // GET: Usuarios
    public async Task<IActionResult> Index(string? texto, string? rol, int page = 1, int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 5) pageSize = 5;
        if (pageSize > 50) pageSize = 50;

        var query = _userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(texto))
        {
            query = query.Where(u =>
                u.UserName.Contains(texto) ||
                u.Email.Contains(texto) ||
                u.NombreCompleto.Contains(texto));
        }

        // Filtro por rol (se filtra por ids de usuarios en el rol)
        if (!string.IsNullOrWhiteSpace(rol))
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(rol);
            var ids = usersInRole.Select(u => u.Id).ToHashSet();
            query = query.Where(u => ids.Contains(u.Id));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var vm = new UsuariosIndexVM
        {
            Items = items,
            Texto = texto,
            Rol = rol,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };

        ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).OrderBy(n => n).ToList();
        return View(vm);
    }

    // GET: Usuarios/Details/id
    public async Task<IActionResult> Details(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        ViewBag.Roles = await _userManager.GetRolesAsync(user);
        return View(user);
    }

    // GET: Usuarios/Edit/id
    public async Task<IActionResult> Edit(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var vm = new UsuarioEditVM
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            NombreCompleto = user.NombreCompleto,
            Rol = roles.FirstOrDefault() ?? "Ventas"
        };

        ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).OrderBy(n => n).ToList();
        return View(vm);
    }

    // POST: Usuarios/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UsuarioEditVM vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).OrderBy(n => n).ToList();
            return View(vm);
        }

        if (!await _roleManager.RoleExistsAsync(vm.Rol))
        {
            ModelState.AddModelError(nameof(vm.Rol), "Rol inválido.");
            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).OrderBy(n => n).ToList();
            return View(vm);
        }

        var user = await _userManager.FindByIdAsync(vm.Id);
        if (user == null) return NotFound();

        // Validaciones de duplicados
        var byName = await _userManager.FindByNameAsync(vm.UserName);
        if (byName != null && byName.Id != user.Id)
            ModelState.AddModelError(nameof(vm.UserName), "Ese nombre de usuario ya existe.");

        var byEmail = await _userManager.FindByEmailAsync(vm.Email);
        if (byEmail != null && byEmail.Id != user.Id)
            ModelState.AddModelError(nameof(vm.Email), "Ese correo ya existe.");

        if (!ModelState.IsValid)
        {
            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).OrderBy(n => n).ToList();
            return View(vm);
        }

        // Actualizar datos
        if (user.UserName != vm.UserName)
        {
            var r = await _userManager.SetUserNameAsync(user, vm.UserName);
            if (!r.Succeeded)
            {
                foreach (var e in r.Errors) ModelState.AddModelError("", e.Description);
                ViewBag.Roles = _roleManager.Roles.Select(r2 => r2.Name).OrderBy(n => n).ToList();
                return View(vm);
            }
        }

        if (user.Email != vm.Email)
        {
            var r = await _userManager.SetEmailAsync(user, vm.Email);
            if (!r.Succeeded)
            {
                foreach (var e in r.Errors) ModelState.AddModelError("", e.Description);
                ViewBag.Roles = _roleManager.Roles.Select(r2 => r2.Name).OrderBy(n => n).ToList();
                return View(vm);
            }
        }

        user.NombreCompleto = vm.NombreCompleto;
        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            foreach (var e in update.Errors) ModelState.AddModelError("", e.Description);
            ViewBag.Roles = _roleManager.Roles.Select(r2 => r2.Name).OrderBy(n => n).ToList();
            return View(vm);
        }

        // Actualizar rol (1 rol por usuario en tu diseño)
        var currentRoles = await _userManager.GetRolesAsync(user);
        var current = currentRoles.FirstOrDefault();

        if (current != vm.Rol)
        {
            if (current != null)
                await _userManager.RemoveFromRoleAsync(user, current);

            await _userManager.AddToRoleAsync(user, vm.Rol);
        }

        TempData["Ok"] = "Usuario actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Usuarios/Delete/id
    public async Task<IActionResult> Delete(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        ViewBag.Roles = await _userManager.GetRolesAsync(user);
        return View(user);
    }

    // POST: Usuarios/DeleteConfirmed/id
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return RedirectToAction(nameof(Index));

        // Evitar que el Admin se borre a sí mismo
        var currentUserId = _userManager.GetUserId(User);
        if (currentUserId == user.Id)
        {
            TempData["Err"] = "No puedes eliminar tu propio usuario.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["Err"] = string.Join(" | ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        TempData["Ok"] = "Usuario eliminado correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
