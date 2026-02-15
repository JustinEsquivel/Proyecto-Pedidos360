using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pedidos360.Areas.Identity.Data;

namespace Pedidos360.Models
{
    [Table("CLIENTE_DIRECCION")]
    public class ClienteDireccion
    {
        [Key]
        [Column("ClienteDireccionId")]
        public int ClienteDireccionId { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [Required(ErrorMessage = "La provincia es requerida")]
        [StringLength(100)]
        public string Provincia { get; set; }

        [Required(ErrorMessage = "El cantón es requerido")]
        [StringLength(100)]
        public string Canton { get; set; }

        [Required(ErrorMessage = "El distrito es requerido")]
        [StringLength(100)]
        public string Distrito { get; set; }

        public bool EsPrincipal { get; set; } = false;

        // Navegación
        [ForeignKey("ClienteId")]
        public virtual Cliente Cliente { get; set; }
    }
}
