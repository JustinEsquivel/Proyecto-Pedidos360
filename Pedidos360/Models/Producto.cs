using Pedidos360.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pedidos360.Models
{
    [Table("PRODUCTO")]
    public class Producto
    {
        [Key]
        [Column("ProductoId")]
        public int ProductoId { get; set; }

        [Required(ErrorMessage = "El nombre del producto es requerido")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El precio es requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio { get; set; }

        [Required]
        [Range(0, 100, ErrorMessage = "El impuesto debe estar entre 0 y 100%")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal ImpuestoPorc { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "La imagen es obligatoria")]
        [StringLength(500)]
        public string ImagenUrl { get; set; }

        [Required]
        public bool Activo { get; set; } = true;

        // Foreign Keys
        [Required]
        public int CategoriaId { get; set; }

        // Navegación
        [ForeignKey("CategoriaId")]
        public virtual Categoria Categoria { get; set; }

        public virtual ICollection<PedidoDetalle> PedidoDetalles { get; set; }
    }
}
