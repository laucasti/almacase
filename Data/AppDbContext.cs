using AlmaCase.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AlmaCase.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    /// <summary>Llaves de cifrado de las sesiones; se guardan en MySQL para que no se cierre la sesión cuando el servidor se reinicia.</summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();
    public DbSet<Pago> Pagos => Set<Pago>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Venta>()
            .HasOne(v => v.Cliente).WithMany(c => c.Ventas)
            .HasForeignKey(v => v.ClienteId).OnDelete(DeleteBehavior.Restrict);

        mb.Entity<DetalleVenta>()
            .HasOne(d => d.Venta).WithMany(v => v.Detalles)
            .HasForeignKey(d => d.VentaId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<DetalleVenta>()
            .HasOne(d => d.Producto).WithMany()
            .HasForeignKey(d => d.ProductoId).OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Pago>()
            .HasOne(p => p.Venta).WithMany(v => v.Pagos)
            .HasForeignKey(p => p.VentaId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<Usuario>().HasIndex(u => u.NombreUsuario).IsUnique();
        mb.Entity<Venta>().HasIndex(v => v.Fecha);
        mb.Entity<Pago>().HasIndex(p => p.Fecha);
    }
}
