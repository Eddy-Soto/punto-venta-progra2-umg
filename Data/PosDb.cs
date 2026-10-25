using Microsoft.EntityFrameworkCore;
using PuntoVenta.Models;

namespace PuntoVenta.Data;
public class PosDb(DbContextOptions<PosDb> options) : DbContext(options)
{
    public DbSet<Puesto> Puestos => Set<Puesto>();
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<CompraDetalle> ComprasDetalle => Set<CompraDetalle>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<VentaDetalle> VentasDetalle => Set<VentaDetalle>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<MenuItem> Menu => Set<MenuItem>();
    public DbSet<Sesion> Sesiones => Set<Sesion>();
    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<Usuario>().HasOne<Empleado>().WithMany().HasForeignKey(x=>x.EmpleadoId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Usuario>().HasIndex(x=>x.EmpleadoId).IsUnique();
        m.Entity<Sesion>().HasOne<Usuario>().WithMany().HasForeignKey(x=>x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        m.Entity<Sesion>().HasIndex(x=>x.UsuarioId);
        m.Entity<Compra>().HasOne<Usuario>().WithMany().HasForeignKey(x=>x.RegistradoPorId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Compra>().Property(x=>x.ProveedorNombre).HasMaxLength(120);
        m.Entity<Compra>().Property(x=>x.ProveedorNit).HasMaxLength(12);
        m.Entity<Venta>().Property(x=>x.ClienteNombre).HasMaxLength(125);
        m.Entity<Venta>().Property(x=>x.ClienteNit).HasMaxLength(12);
        m.Entity<Venta>().Property(x=>x.EmpleadoNombre).HasMaxLength(125);
        m.Entity<Puesto>().HasIndex(x=>x.Nombre).IsUnique();
        m.Entity<Marca>().HasIndex(x=>x.Nombre).IsUnique();
        m.Entity<Proveedor>().HasIndex(x=>x.Nit).IsUnique();
        m.Entity<Cliente>().HasIndex(x=>x.Nit).IsUnique();
        m.Entity<Empleado>().HasIndex(x=>x.Dpi).IsUnique();
        m.Entity<Usuario>().HasIndex(x=>x.Nombre).IsUnique();
        m.Entity<Usuario>().Property(x=>x.Nombre).HasMaxLength(50);
        m.Entity<Usuario>().Property(x=>x.PasswordHash).HasMaxLength(255);
        m.Entity<MenuItem>().Property(x=>x.Titulo).HasMaxLength(50);
        m.Entity<MenuItem>().Property(x=>x.Ruta).HasMaxLength(100);
        m.Entity<MenuItem>().HasOne<MenuItem>().WithMany().HasForeignKey(x=>x.PadreId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Empleado>().HasOne<Puesto>().WithMany().HasForeignKey(x=>x.PuestoId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Producto>().HasOne<Marca>().WithMany().HasForeignKey(x=>x.MarcaId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Producto>().HasIndex(x=>new {x.Nombre,x.MarcaId}).IsUnique();
        m.Entity<Producto>().Property(x=>x.Revision).IsConcurrencyToken();
        m.Entity<Compra>().HasOne<Proveedor>().WithMany().HasForeignKey(x=>x.ProveedorId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Compra>().HasIndex(x=>x.Numero).IsUnique();
        m.Entity<Compra>().HasMany(x=>x.Detalles).WithOne().HasForeignKey(x=>x.CompraId).OnDelete(DeleteBehavior.Cascade);
        m.Entity<CompraDetalle>().HasOne<Producto>().WithMany().HasForeignKey(x=>x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Venta>().HasOne<Cliente>().WithMany().HasForeignKey(x=>x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Venta>().HasOne<Empleado>().WithMany().HasForeignKey(x=>x.EmpleadoId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Venta>().Property(x=>x.Serie).HasMaxLength(1);
        m.Entity<Venta>().HasIndex(x=>new{x.Numero,x.Serie}).IsUnique();
        m.Entity<Venta>().HasMany(x=>x.Detalles).WithOne().HasForeignKey(x=>x.VentaId).OnDelete(DeleteBehavior.Cascade);
        m.Entity<VentaDetalle>().HasOne<Producto>().WithMany().HasForeignKey(x=>x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        foreach(var entity in m.Model.GetEntityTypes())
            foreach(var prop in entity.GetProperties().Where(p=>p.ClrType==typeof(decimal)))
            { prop.SetPrecision(8); prop.SetScale(2); }
        m.Entity<CompraDetalle>().ToTable("Compras_Detalle");
        m.Entity<VentaDetalle>().ToTable("Ventas_Detalle");
    }
}
