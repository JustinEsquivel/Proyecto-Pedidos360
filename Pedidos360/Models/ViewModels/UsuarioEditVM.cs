using System.ComponentModel.DataAnnotations;

namespace Pedidos360.Models.ViewModels;

public class UsuarioEditVM
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required, StringLength(50)]
    [Display(Name = "Nombre de usuario")]
    public string UserName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    [Display(Name = "Correo")]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = "Ventas";
}
