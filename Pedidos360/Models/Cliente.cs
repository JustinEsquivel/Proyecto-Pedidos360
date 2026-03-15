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
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$",
            ErrorMessage = "El nombre solo puede contener letras y espacios")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La cédula es requerida")]
        [StringLength(20, ErrorMessage = "La cédula no puede exceder 20 caracteres")]
        [RegularExpression(@"^[1-7]\d{8}$",
            ErrorMessage = "La cédula debe tener 9 dígitos y comenzar con un número de provincia válido")]
        public string Cedula { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es requerido")]
        [EmailAddress(ErrorMessage = "Debe ingresar un correo válido")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "El correo debe tener entre 5 y 100 caracteres")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            ErrorMessage = "El correo debe tener un formato válido (ejemplo: usuario@dominio.com)")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es requerido")]
        [Phone(ErrorMessage = "El teléfono no es válido")]
        [StringLength(20)]
        public string Telefono { get; set; } = string.Empty;

        public virtual ICollection<ClienteDireccion> Direcciones { get; set; } = new List<ClienteDireccion>();
        public virtual ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    }
}
