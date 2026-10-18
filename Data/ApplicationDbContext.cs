using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ControlClientesRestaurante.Models;

namespace ControlClientesRestaurante.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Factura> Facturas { get; set; }
        public DbSet<LineaFactura> LineasFactura { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<LineaPedido> LineasPedido { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Mesa> Mesas { get; set; }
        public DbSet<Reserva> Reservas { get; set; }
        public DbSet<Resena> Resenas { get; set; }
        public DbSet<TransaccionPago> TransaccionesPago { get; set; }
        public DbSet<TurnoCaja> TurnosCaja { get; set; }
        public DbSet<Cupon> Cupones { get; set; }
        public DbSet<AuditoriaLog> AuditoriaLogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Suprimir la advertencia de "pending model changes" para permitir
            // que las migraciones manuales funcionen sin regenerar el snapshot.
            optionsBuilder.ConfigureWarnings(w =>
                w.Ignore(RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Clientes
            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.Documento)
                .IsUnique();

            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.Codigo)
                .IsUnique();

            // Usuarios
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.NombreUsuario)
                .IsUnique();

            // Facturas
            modelBuilder.Entity<Factura>()
                .HasIndex(f => f.Numero)
                .IsUnique();

            modelBuilder.Entity<Factura>()
                .HasMany(f => f.Lineas)
                .WithOne(l => l.Factura)
                .HasForeignKey(l => l.FacturaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Linea: Subtotal es calculado, se ignora en BD
            modelBuilder.Entity<LineaFactura>()
                .Ignore(l => l.Subtotal);

            // Pedidos
            modelBuilder.Entity<Pedido>()
                .HasIndex(p => p.Numero)
                .IsUnique();

            modelBuilder.Entity<Pedido>()
                .HasMany(p => p.Lineas)
                .WithOne(l => l.Pedido)
                .HasForeignKey(l => l.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LineaPedido>()
                .Ignore(l => l.Subtotal);

            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Factura)
                .WithMany()
                .HasForeignKey(p => p.FacturaId)
                .OnDelete(DeleteBehavior.SetNull);

            // Reservas
            modelBuilder.Entity<Reserva>()
                .HasIndex(r => r.Codigo)
                .IsUnique();

            // Cupones
            modelBuilder.Entity<Cupon>()
                .HasIndex(c => c.Codigo)
                .IsUnique();
        }
    }
}