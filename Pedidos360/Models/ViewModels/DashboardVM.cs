namespace Pedidos360.Models.ViewModels
{
    public class DashboardVM
    {
        public int     TotalPedidos       { get; set; }
        public int     PedidosPendientes  { get; set; }
        public int     PedidosConfirmados { get; set; }
        public int     PedidosFacturados  { get; set; }
        public decimal TotalFacturado     { get; set; }
        public int     TotalClientes      { get; set; }
        public int     TotalProductos     { get; set; }
        public int     ProductosBajoStock { get; set; }
        public List<Pedido> PedidosRecientes { get; set; } = new();
    }
}
