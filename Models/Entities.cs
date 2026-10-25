using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PuntoVenta.Models;

public abstract class Entity { public int Id { get; set; } }
public class Puesto : Entity
{
    [Required(ErrorMessage="Ingrese el puesto."), StringLength(50, ErrorMessage="El máximo permitido es {1} caracteres.")] public string Nombre { get; set; } = "";
}
public class Marca : Entity
{
    [Required(ErrorMessage="Ingrese la marca."), StringLength(50, ErrorMessage="El máximo permitido es {1} caracteres.")] public string Nombre { get; set; } = "";
}
public class Cliente : Entity, IValidatableObject
{
    [Required(ErrorMessage="Este campo es obligatorio."), StringLength(60, ErrorMessage="El máximo permitido es {1} caracteres.")] public string Nombres { get; set; } = "";
    [Required(ErrorMessage="Este campo es obligatorio."), StringLength(60, ErrorMessage="El máximo permitido es {1} caracteres.")] public string Apellidos { get; set; } = "";
    [Required(ErrorMessage="Este campo es obligatorio."), StringLength(12, ErrorMessage="El máximo permitido es {1} caracteres.")] public string Nit { get; set; } = "";
    public bool Genero { get; set; }
    [StringLength(25, ErrorMessage="El máximo permitido es {1} caracteres."), RegularExpression(@"[0-9+()\-\s]{7,25}", ErrorMessage="Teléfono inválido.")] public string? Telefono { get; set; }
    [EmailAddress(ErrorMessage="Ingrese un correo electrónico válido."), StringLength(45, ErrorMessage="El máximo permitido es {1} caracteres.")] public string? CorreoElectronico { get; set; }
    public DateTime FechaIngreso { get; set; } = DateTime.Now;
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(Nit?.Replace("/", "").ToUpperInvariant()!="CF")
        {
            if(string.IsNullOrWhiteSpace(Telefono)) yield return new("Ingrese el teléfono.",[nameof(Telefono)]);
            if(string.IsNullOrWhiteSpace(CorreoElectronico)) yield return new("Ingrese el correo electrónico.",[nameof(CorreoElectronico)]);
        }
    }
    [NotMapped] public string NombreCompleto => $"{Nombres} {Apellidos}";
}
public class Proveedor : Entity
{
    [Required(ErrorMessage="Este campo es obligatorio."), StringLength(60, ErrorMessage="El máximo permitido es {1} caracteres.")] public string Nombre { get; set; } = "";
    [Required(ErrorMessage="Este campo es obligatorio."), StringLength(12, ErrorMessage="El máximo permitido es {1} caracteres.")] public string Nit { get; set; } = "";
    [Required(ErrorMessage="Este campo es obligatorio."), StringLength(80, ErrorMessage="El máximo permitido es {1} caracteres.")] public string Direccion { get; set; } = "";
    [Required(ErrorMessage="Este campo es obligatorio."), StringLength(25, ErrorMessage="El máximo permitido es {1} caracteres."), RegularExpression(@"[0-9+()\-\s]{7,25}", ErrorMessage="Teléfono inválido.")] public string Telefono { get; set; } = "";
}
public class Empleado : Entity
{
    [Range(typeof(decimal),"0","999999.99",ErrorMessage="Sueldo inválido.")] public decimal Sueldo { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime? FechaEgreso { get; set; }
    [Required(ErrorMessage="Este campo es obligatorio."), StringLength(60, ErrorMessage="El máximo permitido es {1} caracteres.")] public string Nombres { get; set; } = "";
    [Required(ErrorMessage="Este campo es obligatorio."), StringLength(60, ErrorMessage="El máximo permitido es {1} caracteres.")] public string Apellidos { get; set; } = "";
    [Required(ErrorMessage="Este campo es obligatorio."), StringLength(80, ErrorMessage="El máximo permitido es {1} caracteres.")] public string Direccion { get; set; } = "";
    [Required(ErrorMessage="Este campo es obligatorio."), StringLength(25, ErrorMessage="El máximo permitido es {1} caracteres."), RegularExpression(@"[0-9+()\-\s]{7,25}", ErrorMessage="Teléfono inválido.")] public string Telefono { get; set; } = "";
    [Required(ErrorMessage="Este campo es obligatorio."), StringLength(15, ErrorMessage="El máximo permitido es {1} caracteres."), RegularExpression(@"\d{13}", ErrorMessage="El DPI debe contener 13 dígitos.")] public string Dpi { get; set; } = "";
    public bool Genero { get; set; }
    public DateTime FechaNacimiento { get; set; } = DateTime.Today.AddYears(-20);
    [Range(1, int.MaxValue, ErrorMessage="Seleccione un puesto.")] public int PuestoId { get; set; }
    public DateTime FechaInicioLabores { get; set; } = DateTime.Today;
    public DateTime FechaIngreso { get; set; } = DateTime.Now;
    [NotMapped] public string NombreCompleto => $"{Nombres} {Apellidos}";
}
public class Producto : Entity
{
    public int Revision { get; set; }
    [Required(ErrorMessage="Este campo es obligatorio."), StringLength(50, ErrorMessage="El máximo permitido es {1} caracteres.")] public string Nombre { get; set; } = "";
    [Range(1, int.MaxValue, ErrorMessage="Seleccione una marca.")] public int MarcaId { get; set; }
    [Required(ErrorMessage="Este campo es obligatorio."), StringLength(100, ErrorMessage="El máximo permitido es {1} caracteres.")] public string Descripcion { get; set; } = "";
    [StringLength(255, ErrorMessage="El máximo permitido es {1} caracteres.")] public string? Imagen { get; set; }
    [Range(typeof(decimal), "0.01", "999999.99",ErrorMessage="El precio debe estar entre 0.01 y 999,999.99.")] public decimal PrecioCosto { get; set; }
    [Range(typeof(decimal), "0.01", "999999.99",ErrorMessage="El precio debe estar entre 0.01 y 999,999.99.")] public decimal PrecioVenta { get; set; }
    [Range(0,int.MaxValue,ErrorMessage="La existencia no puede ser negativa.")] public int Existencia { get; set; }
    public DateTime FechaIngreso { get; set; } = DateTime.Now;
}
public class Compra : Entity
{
    public int? RegistradoPorId { get; set; }
    public string? ProveedorNombre { get; set; }
    public string? ProveedorNit { get; set; }
    public int Numero { get; set; }
    public int ProveedorId { get; set; }
    public DateTime Fecha { get; set; }
    public DateTime FechaIngreso { get; set; } = DateTime.Now;
    public List<CompraDetalle> Detalles { get; set; } = [];
}
public class CompraDetalle : Entity
{
    public int CompraId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioCostoUnitario { get; set; }
}
public class Venta : Entity
{
    public string? ClienteNombre { get; set; }
    public string? ClienteNit { get; set; }
    public string? EmpleadoNombre { get; set; }
    public int Numero { get; set; }
    public string Serie { get; set; } = "A";
    public DateTime Fecha { get; set; }
    public int ClienteId { get; set; }
    public int EmpleadoId { get; set; }
    public DateTime FechaIngreso { get; set; } = DateTime.Now;
    public List<VentaDetalle> Detalles { get; set; } = [];
}
public class VentaDetalle : Entity
{
    public int VentaId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}
public class Usuario : Entity
{
    public int? EmpleadoId { get; set; }
    public bool EsAdministrador { get; set; }
    public bool Vendedor { get; set; }
    public bool Comprador { get; set; }
    public bool Administrativo { get; set; }
    public bool Activo { get; set; } = true;
    public string Nombre { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}
public class MenuItem : Entity
{
    public string Titulo { get; set; } = "";
    public string Ruta { get; set; } = "";
    public int? PadreId { get; set; }
    public int Orden { get; set; }
}
public class LoginRequest
{
    public string Acceso {get;set;}="";
    [Required(ErrorMessage="Este campo es obligatorio.")] public string Usuario { get; set; } = "";
    [Required(ErrorMessage="Este campo es obligatorio.")] public string Password { get; set; } = "";
}
public record PerfilUsuario(int Id,string Usuario,int? EmpleadoId,string Empleado,bool EsAdministrador,bool Vendedor,bool Comprador,bool Administrativo)
{
    public bool PuedeVender => EsAdministrador || Vendedor;
    public bool PuedeComprar => EsAdministrador || Comprador;
    public bool PuedeAdministrarPersonal => EsAdministrador || Administrativo;
    public string[] Roles => new[]{EsAdministrador?"Administrador":null,Vendedor?"Vendedor":null,Comprador?"Comprador":null,Administrativo?"Administrativo":null}.Where(x=>x is not null).Cast<string>().ToArray();
}
public record LoginResponse(string Token,string Usuario,PerfilUsuario Perfil,DateTime ExpiraUtc);
public class Sesion
{
    [MaxLength(64)] public string Id { get; set; } = "";
    public int UsuarioId { get; set; }
    [MaxLength(64)] public string TokenHash { get; set; } = "";
    public DateTime ExpiraUtc { get; set; }
    public DateTime? RevocadaUtc { get; set; }
}
public class CuentaRequest : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(!string.IsNullOrEmpty(Password)&&(Password.Length<6||string.IsNullOrWhiteSpace(Password)))
            yield return new("La clave debe tener entre 6 y 100 caracteres.",[nameof(Password)]);
    }
    public int Id {get;set;}
    [Required,StringLength(50)] public string Nombre {get;set;}="";
    public int? EmpleadoId {get;set;}
    public bool Vendedor {get;set;}
    public bool Comprador {get;set;}
    public bool Administrativo {get;set;}
    public bool Activo {get;set;}=true;
    [StringLength(100,ErrorMessage="La clave debe tener como máximo 100 caracteres.")] public string? Password {get;set;}
}
public record CuentaVista(int Id,string Nombre,int? EmpleadoId,string Empleado,bool EsAdministrador,bool Vendedor,bool Comprador,bool Administrativo,bool Activo);
public class DocumentoRequest
{
    [Range(1,int.MaxValue,ErrorMessage="Seleccione un valor mayor que cero.")] public int Numero { get; set; } = 1;
    [Required(ErrorMessage="Este campo es obligatorio."), StringLength(1, ErrorMessage="El máximo permitido es {1} caracteres.")] public string Serie { get; set; } = "A";
    [Range(1,int.MaxValue,ErrorMessage="Seleccione un valor mayor que cero.")] public int TerceroId { get; set; }
    public int EmpleadoId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Today;
    [Required(ErrorMessage="El detalle es obligatorio."), MinLength(1, ErrorMessage="Agregue al menos un producto.")] public List<LineaRequest> Detalles { get; set; } = [];
}
public class LineaRequest
{
    [Range(1,int.MaxValue,ErrorMessage="Seleccione un valor mayor que cero.")] public int ProductoId { get; set; }
    [Range(1,1000000,ErrorMessage="La cantidad debe estar entre 1 y 1,000,000.")] public int Cantidad { get; set; } = 1;
    [Range(typeof(decimal),"0.01","999999.99",ErrorMessage="El precio debe estar entre 0.01 y 999,999.99.")] public decimal Precio { get; set; }
}
public record DocumentoLinea(int ProductoId, string Producto, int Cantidad, decimal Precio)
{
    public decimal Subtotal => Cantidad * Precio;
}
public record DocumentoVista(int Id, int Numero, string Serie, DateTime Fecha, string Tercero, string Empleado, List<DocumentoLinea> Detalles, string Nit="")
{
    public decimal Total => Detalles.Sum(x=>x.Subtotal);
}

public record ClienteBusqueda(bool Encontrado,Cliente? Cliente);
