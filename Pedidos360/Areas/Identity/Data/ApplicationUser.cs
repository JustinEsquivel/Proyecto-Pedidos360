using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using Pedidos360.Models;

namespace Pedidos360.Areas.Identity.Data
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string? NombreCompleto { get; set; } 

        public virtual ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    }
}
