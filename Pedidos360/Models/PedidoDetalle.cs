using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pedidos360.Areas.Identity.Data;

namespace Pedidos360.Models
{
    [Table("PEDIDO_DETALLE")]
    public class PedidoDetalle
    {
        [Key]
        [Column("PedidoDetalleId")]
        public int PedidoDetalleId { get; set; }

        [Required]
        public int Cantidad { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioUnit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Descuento { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal ImpuestoPorc { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalLinea { get; set; }

        // Foreign Keys
        [Required]
        public int PedidoId { get; set; }

        [Required]
        public int ProductoId { get; set; }

        // Navegación
        [ForeignKey("PedidoId")]
        public virtual Pedido Pedido { get; set; }

        [ForeignKey("ProductoId")]
        public virtual Producto Producto { get; set; }
    }
}
