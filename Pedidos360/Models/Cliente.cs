using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pedidos360.Models
{
    [Table("CLIENTE")]
    public class Cliente
    {
        [Key]
        [Column("ClienteId")]
        public int ClienteId { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La cédula es requerida")]
        [StringLength(20, ErrorMessage = "La cédula no puede exceder 20 caracteres")]
        public string Cedula { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es requerido")]
        [EmailAddress(ErrorMessage = "El correo no es válido")]
        [StringLength(100)]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es requerido")]
        [Phone(ErrorMessage = "El teléfono no es válido")]
        [StringLength(20)]
        public string Telefono { get; set; } = string.Empty;

        public virtual ICollection<ClienteDireccion> Direcciones { get; set; } = new List<ClienteDireccion>();
        public virtual ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    }
}
