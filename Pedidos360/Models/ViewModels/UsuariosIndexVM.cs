using Pedidos360.Areas.Identity.Data;

namespace Pedidos360.Models.ViewModels;

public class UsuariosIndexVM
{
    public List<ApplicationUser> Items { get; set; } = new();
    public string? Texto { get; set; }
    public string? Rol { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }

    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
}
