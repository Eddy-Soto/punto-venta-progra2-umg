using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuntoVenta.Data;
using PuntoVenta.Models;
using PuntoVenta.Services;
namespace PuntoVenta.Controllers;
[ApiController,Authorize(Roles=Permisos.Administrador),Route("api/cuentas")]
public class CuentasController(PosDb db,SessionEvents events):ControllerBase
{
    [HttpGet]public async Task<List<CuentaVista>> Listar()
    {
        var employees=await db.Empleados.ToDictionaryAsync(x=>x.Id,x=>x.NombreCompleto);
        return (await db.Usuarios.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()).Select(u=>new CuentaVista(u.Id,u.Nombre,u.EmpleadoId,u.EmpleadoId is int id?employees.GetValueOrDefault(id,""):"Administrador",u.EsAdministrador,u.Vendedor,u.Comprador,u.Administrativo,u.Activo)).ToList();
    }
    [HttpPost]public Task<IActionResult> Crear(CuentaRequest r)=>Guardar(0,r);
    [HttpPut("{id:int}")]public async Task<IActionResult> Guardar(int id,CuentaRequest r)
    {
        var name=r.Nombre.Trim().ToUpperInvariant();
        if(string.IsNullOrWhiteSpace(name))return BadRequest(new{message="Ingrese un usuario."});
        var user=id==0?new Usuario():await db.Usuarios.SingleOrDefaultAsync(x=>x.Id==id);
        if(user is null)return NotFound();
        if(user.EsAdministrador)return BadRequest(new{message="La cuenta principal conserva sus permisos de administrador."});
        if(r.EmpleadoId is not int employee||!await db.Empleados.AnyAsync(x=>x.Id==employee))return BadRequest(new{message="Seleccione un empleado."});
        if(r.Activo&&!await db.Empleados.AnyAsync(x=>x.Id==employee&&x.Activo))return BadRequest(new{message="No puede activar el acceso de un empleado egresado."});
        if(!r.Vendedor&&!r.Comprador&&!r.Administrativo)return BadRequest(new{message="Asigne al menos un rol."});
        if(await db.Usuarios.AnyAsync(x=>x.Id!=id&&(x.Nombre==name||x.EmpleadoId==employee)))return Conflict(new{message="El nombre de usuario o empleado ya tiene una cuenta."});
        if(id!=0&&user.EmpleadoId!=employee)return BadRequest(new{message="No puede transferir una cuenta a otra persona. Cree una cuenta nueva."});
        if(id==0&&string.IsNullOrWhiteSpace(r.Password))return BadRequest(new{message="Ingrese una contraseña inicial."});
        user.Nombre=name;user.EmpleadoId=employee;user.Vendedor=r.Vendedor;user.Comprador=r.Comprador;user.Administrativo=r.Administrativo;user.Activo=r.Activo;
        if(!string.IsNullOrEmpty(r.Password))user.PasswordHash=new PasswordHasher<Usuario>().HashPassword(user,r.Password);
        if(id==0)db.Usuarios.Add(user);
        await db.SaveChangesAsync();
        var sessions=await db.Sesiones.Where(x=>x.UsuarioId==user.Id&&x.RevocadaUtc==null).ToListAsync();
        foreach(var session in sessions)session.RevocadaUtc=DateTime.UtcNow;
        await db.SaveChangesAsync();foreach(var session in sessions)events.Notify(session.Id);
        return Ok(new{id=user.Id,message="Cuenta y roles guardados. Las sesiones anteriores se cerraron."});
    }
}
[ApiController,Authorize,Route("api/clientes/por-nit")]
public class BuscarClienteController(PosDb db):ControllerBase
{
    [HttpGet]public async Task<IActionResult> Buscar([FromQuery]string nit)
    {
        if(!Permisos.Vender(User))return Forbid();
        var normalized=Permisos.Nit(nit);
        if(normalized.Length==0||normalized.Length>12)return BadRequest(new{message="Ingrese NIT o CF."});
        var cliente=await db.Clientes.AsNoTracking().FirstOrDefaultAsync(x=>x.Nit.Replace("-","").Replace("/","").Replace(" ","").ToUpper()==normalized);
        return Ok(new{encontrado=cliente is not null,cliente});
    }
}
