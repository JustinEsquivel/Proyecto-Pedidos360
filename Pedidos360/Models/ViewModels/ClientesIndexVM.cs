using Pedidos360.Models;

namespace Pedidos360.Models.ViewModels
{
    public class ClientesIndexVM
    {
        public List<Cliente> Items { get; set; } = new();

        public string? Nombre { get; set; }
        public string? Cedula { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
        public bool HasPrev => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}
