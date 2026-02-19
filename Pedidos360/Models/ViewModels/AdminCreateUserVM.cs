using System.ComponentModel.DataAnnotations;

namespace Pedidos360.Models.ViewModels;

public class AdminCreateUserVM
{
    [Required, StringLength(100)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string UserName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Rol { get; set; } = "Ventas";
}
