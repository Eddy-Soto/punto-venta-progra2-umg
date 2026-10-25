using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PuntoVenta.Models;
namespace PuntoVenta.Data;
public static class DbInitializer
{
    public static async Task Initialize(PosDb db,IConfiguration config)
    {
        await db.Database.EnsureCreatedAsync();
        await SchemaUpgrader.Apply(db);
        if(!await db.Usuarios.AnyAsync())
        {
            var password=config["Seed:AdminPassword"];
            if(string.IsNullOrWhiteSpace(password) || password.Length<10) throw new InvalidOperationException("Configure Seed:AdminPassword con al menos 10 caracteres.");
            var admin=new Usuario{Nombre="admin",EsAdministrador=true}; admin.PasswordHash=new PasswordHasher<Usuario>().HashPassword(admin,password);
            db.Usuarios.Add(admin); await db.SaveChangesAsync();
        }
        if(!await db.Menu.AnyAsync())
        {
            var productos=new MenuItem{Titulo="Productos",Ruta="catalogos/productos",Orden=1};
            var ventas=new MenuItem{Titulo="Ventas",Ruta="documentos/ventas",Orden=3};
            var compras=new MenuItem{Titulo="Compras",Ruta="documentos/compras",Orden=7};
            db.Menu.AddRange(productos,ventas,compras); await db.SaveChangesAsync();
            var empleados=new MenuItem{Titulo="Empleados",Ruta="catalogos/empleados",PadreId=ventas.Id,Orden=5};
            db.Menu.AddRange(new MenuItem{Titulo="Marcas",Ruta="catalogos/marcas",PadreId=productos.Id,Orden=2},
                new MenuItem{Titulo="Clientes",Ruta="catalogos/clientes",PadreId=ventas.Id,Orden=4},empleados,
                new MenuItem{Titulo="Proveedores",Ruta="catalogos/proveedores",PadreId=compras.Id,Orden=8});
            await db.SaveChangesAsync();
            db.Menu.Add(new MenuItem{Titulo="Puestos",Ruta="catalogos/puestos",PadreId=empleados.Id,Orden=6}); await db.SaveChangesAsync();
        }
        if(config.GetValue<bool>("Seed:DemoData") && !await db.Productos.AnyAsync())
        {
            var puesto=new Puesto{Nombre="Vendedor"}; var marca=new Marca{Nombre="Logitech"};
            var marca2=new Marca{Nombre="Kingston"};
            db.AddRange(puesto,marca,marca2); await db.SaveChangesAsync();
            db.Clientes.Add(new Cliente{Nombres="Carlos",Apellidos="Cliente de ejemplo",Nit="DEMO-C01",Telefono="55551001",CorreoElectronico="cliente@example.com",Genero=true});
            db.Proveedores.Add(new Proveedor{Nombre="Distribuidora de Tecnología Demo",Nit="DEMO-P01",Direccion="Ciudad de Guatemala · Datos de ejemplo",Telefono="55552002"});
            db.Empleados.Add(new Empleado{Nombres="Ana",Apellidos="Vendedora de ejemplo",Dpi="0000000000001",Direccion="Dirección de ejemplo",Telefono="55553003",PuestoId=puesto.Id,FechaNacimiento=new(2000,1,15),FechaInicioLabores=new(2024,1,1)});
            db.Productos.AddRange(
                new Producto{Nombre="Teclado inalámbrico",Descripcion="Teclado de demostración",MarcaId=marca.Id,PrecioCosto=100,PrecioVenta=125,Existencia=20,Imagen="/images/productos/demo-teclado.png"},
                new Producto{Nombre="Mouse óptico",Descripcion="Mouse de demostración",MarcaId=marca.Id,PrecioCosto=40,PrecioVenta=50,Existencia=15,Imagen="/images/productos/demo-mouse.png"},
                new Producto{Nombre="Memoria USB 64 GB",Descripcion="Memoria de demostración",MarcaId=marca2.Id,PrecioCosto=60,PrecioVenta=75,Existencia=5,Imagen="/images/productos/demo-usb.png"});
            await db.SaveChangesAsync();
        }
        if(!await db.Clientes.AnyAsync(x=>x.Nit.Replace("/", "").ToUpper()=="CF"))
        {db.Clientes.Add(new Cliente{Nombres="Cliente",Apellidos="final",Nit="CF"});await db.SaveChangesAsync();}
        if(!string.IsNullOrWhiteSpace(config["Seed:EmployeePassword"]) && !await db.Usuarios.AnyAsync(x=>x.Nombre=="EMPLEADO"))
        {
            var empleado=await db.Empleados.OrderBy(x=>x.Id).FirstOrDefaultAsync(x=>x.Activo);
            if(empleado is not null)
            {
                var user=new Usuario{Nombre="EMPLEADO",EmpleadoId=empleado.Id,Vendedor=true,Comprador=true};
                user.PasswordHash=new PasswordHasher<Usuario>().HashPassword(user,config["Seed:EmployeePassword"]!);
                db.Usuarios.Add(user);await db.SaveChangesAsync();
            }
        }
        if(!await db.Menu.AnyAsync(x=>x.Ruta=="reportes"))
        {db.Menu.Add(new MenuItem{Titulo="Reportes",Ruta="reportes",Orden=9});await db.SaveChangesAsync();}
        if(!await db.Menu.AnyAsync(x=>x.Ruta=="administracion/cuentas"))
        {db.Menu.Add(new MenuItem{Titulo="Cuentas y roles",Ruta="administracion/cuentas",Orden=10});await db.SaveChangesAsync();}
    }
}
