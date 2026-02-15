using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pedidos360.Areas.Identity.Data;
using Pedidos360.Models;

namespace Pedidos360.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Constructor sin parámetros para design-time
        public ApplicationDbContext() : base()
        {
        }

        // DbSets
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<ClienteDireccion> ClienteDirecciones { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<PedidoDetalle> PedidoDetalles { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Leer la cadena de conexión desde appsettings.json
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

                var connectionString = configuration.GetConnectionString("DefaultConnection");
                optionsBuilder.UseSqlServer(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuraciones adicionales de las entidades

            //ÍNDICES ÚNICOS 
            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.Cedula)
                .IsUnique();

            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.Correo)
                .IsUnique();

            // ====== CONFIGURACIÓN DE DECIMALES ======
            // Producto
            modelBuilder.Entity<Producto>()
                .Property(p => p.Precio)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Producto>()
                .Property(p => p.ImpuestoPorc)
                .HasPrecision(5, 2);

            // Pedido
            modelBuilder.Entity<Pedido>()
                .Property(p => p.Subtotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Pedido>()
                .Property(p => p.Impuestos)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Pedido>()
                .Property(p => p.Total)
                .HasPrecision(18, 2);

            // PedidoDetalle
            modelBuilder.Entity<PedidoDetalle>()
                .Property(pd => pd.PrecioUnit)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PedidoDetalle>()
                .Property(pd => pd.Descuento)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PedidoDetalle>()
                .Property(pd => pd.ImpuestoPorc)
                .HasPrecision(5, 2);

            modelBuilder.Entity<PedidoDetalle>()
                .Property(pd => pd.TotalLinea)
                .HasPrecision(18, 2);

            // ====== CONFIGURACIÓN DE RELACIONES ======

            // Producto -> Categoria
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            // ClienteDireccion -> Cliente
            modelBuilder.Entity<ClienteDireccion>()
                .HasOne(cd => cd.Cliente)
                .WithMany(c => c.Direcciones)
                .HasForeignKey(cd => cd.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Pedido -> Cliente
            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Cliente)
                .WithMany(c => c.Pedidos)
                .HasForeignKey(p => p.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Pedido -> Usuario
            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Usuario)
                .WithMany(u => u.Pedidos)
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // PedidoDetalle -> Pedido
            modelBuilder.Entity<PedidoDetalle>()
                .HasOne(pd => pd.Pedido)
                .WithMany(p => p.Detalles)
                .HasForeignKey(pd => pd.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            // PedidoDetalle -> Producto
            modelBuilder.Entity<PedidoDetalle>()
                .HasOne(pd => pd.Producto)
                .WithMany(p => p.PedidoDetalles)
                .HasForeignKey(pd => pd.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        }


    }
}