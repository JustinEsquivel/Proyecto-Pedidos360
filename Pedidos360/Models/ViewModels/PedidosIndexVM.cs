namespace Pedidos360.Models.ViewModels
{
    public class PedidosIndexVM
    {
        public List<Pedido> Items      { get; set; } = new();
        public string?   Estado        { get; set; }
        public DateTime? FechaDesde    { get; set; }
        public DateTime? FechaHasta    { get; set; }
        public string?   ClienteNombre { get; set; }
        public string?   VendedorNombre{ get; set; }
        public int Page      { get; set; } = 1;
        public int PageSize  { get; set; } = 10;
        public int Total     { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
        public bool HasPrev  => Page > 1;
        public bool HasNext  => Page < TotalPages;
    }
}
