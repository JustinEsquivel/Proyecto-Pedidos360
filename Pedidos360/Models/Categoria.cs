using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pedidos360.Areas.Identity.Data;

namespace Pedidos360.Models
{
    [Table("CATEGORIA")]
    public class Categoria
    {
        [Key]
        [Column("CategoriaId")]
        public int CategoriaId { get; set; }

        [Required(ErrorMessage = "El nombre de la categoría es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; }

        // Navegación
        public virtual ICollection<Producto> Productos { get; set; }
    }
}
