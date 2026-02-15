using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pedidos360.Areas.Identity.Data;

namespace Pedidos360.Models
{
    [Table("PEDIDO")]
    public class Pedido
    {
        [Key]
        [Column("PedidoId")]
        public int PedidoId { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Impuestos { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        [Required]
        [StringLength(50)]
        public string Estado { get; set; } = "Pendiente";

        // Foreign Keys
        [Required]
        public int ClienteId { get; set; }

        [Required]
        public string UsuarioId { get; set; }

        // Navegación
        [ForeignKey("ClienteId")]
        public virtual Cliente Cliente { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual ApplicationUser Usuario { get; set; }

        public virtual ICollection<PedidoDetalle> Detalles { get; set; }
    }
}
