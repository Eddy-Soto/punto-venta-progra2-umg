using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuntoVenta.Data;
using PuntoVenta.Models;
using PuntoVenta.Services;

namespace PuntoVenta.Controllers;

[ApiController, Authorize]
public abstract class CatalogoController<T>(PosDb db) : ControllerBase where T : Entity
{
    private bool CanRead()=>User.IsInRole(Permisos.Administrador)||
        (typeof(T)==typeof(Producto)&&(Permisos.Vender(User)||Permisos.Comprar(User)))||
        (typeof(T)==typeof(Proveedor)&&Permisos.Comprar(User))||
        ((typeof(T)==typeof(Empleado)||typeof(T)==typeof(Puesto))&&Permisos.Personal(User));
    private bool CanWrite(bool creating=false)=>User.IsInRole(Permisos.Administrador)||
        ((typeof(T)==typeof(Empleado)||typeof(T)==typeof(Puesto))&&Permisos.Personal(User))||
        (creating&&typeof(T)==typeof(Cliente)&&Permisos.Vender(User));
    [HttpGet] public async Task<ActionResult<List<T>>> Listar([FromQuery]string? buscar)
    {
        if(!CanRead())return Forbid();
        var rows=await db.Set<T>().AsNoTracking().OrderBy(x=>x.Id).ToListAsync();
        if(!string.IsNullOrWhiteSpace(buscar))
            rows=rows.Where(x=>typeof(T).GetProperties().Where(p=>p.PropertyType==typeof(string))
                .Any(p=>(p.GetValue(x)?.ToString()??"").Contains(buscar,StringComparison.OrdinalIgnoreCase))).ToList();
        return rows;
    }
    [HttpGet("{id:int}")] public async Task<ActionResult<T>> Obtener(int id)
    {
        if(!CanRead())return Forbid();
        var row=await db.Set<T>().FindAsync(id);
        return row is null ? NotFound(new{message="El registro no existe."}) : row;
    }
    [HttpPost] public async Task<ActionResult<T>> Crear(T item)
    {
        if(!CanWrite(true))return Forbid();
        item.Id=0;
        var error=await Validate(item);
        if(error is not null) return BadRequest(new{message=error});
        db.Add(item); await db.SaveChangesAsync();
        return Created($"{Request.Path}/{item.Id}",item);
    }
    [HttpPut("{id:int}")] public async Task<IActionResult> Editar(int id,T item)
    {
        if(!CanWrite())return Forbid();
        if(id!=item.Id) return BadRequest(new{message="Identificador inválido."});
        var current=await db.Set<T>().FindAsync(id);
        if(current is null) return NotFound();
        if(current is Cliente cf&&Permisos.Nit(cf.Nit)=="CF")return BadRequest(new{message="El cliente CF es un registro reservado."});
        var error=await Validate(item);
        if(error is not null) return BadRequest(new{message=error});
        if(item is Producto p && current is Producto old && p.Revision!=old.Revision)
            return Conflict(new{message="El producto cambió mientras lo editaba. Actualice la lista y vuelva a intentarlo."});
        if(item is Producto product) product.Revision++;
        db.Entry(current).CurrentValues.SetValues(item);
        await db.SaveChangesAsync();
        if(item is Empleado employee&&!employee.Activo)
        {
            var userIds=await db.Usuarios.Where(x=>x.EmpleadoId==employee.Id).Select(x=>x.Id).ToListAsync();
            var sessions=await db.Sesiones.Where(x=>userIds.Contains(x.UsuarioId)&&x.RevocadaUtc==null).ToListAsync();
            foreach(var session in sessions)session.RevocadaUtc=DateTime.UtcNow;
            await db.SaveChangesAsync();
            var events=HttpContext.RequestServices.GetRequiredService<SessionEvents>();
            foreach(var session in sessions)events.Notify(session.Id);
        }
        return NoContent();
    }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Eliminar(int id)
    {
        if(!CanWrite())return Forbid();
        var item=await db.Set<T>().FindAsync(id);
        if(item is null) return NotFound();
        if(item is Cliente cf&&Permisos.Nit(cf.Nit)=="CF")return BadRequest(new{message="El cliente CF no puede eliminarse."});
        db.Remove(item); await db.SaveChangesAsync(); return NoContent();
    }
    private async Task<string?> Validate(T item)
    {
        foreach(var property in typeof(T).GetProperties().Where(p=>p.PropertyType==typeof(string) && p.CanWrite))
            if(property.GetValue(item) is string value) property.SetValue(item,value.Trim());
        var results=new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        if(!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(item,new(item),results,true))
            return string.Join(" ",results.Select(x=>x.ErrorMessage));
        if(item is Cliente client)
        {
            client.Nit=Permisos.Nit(client.Nit);
            if(await db.Clientes.AnyAsync(x=>x.Id!=client.Id&&x.Nit.Replace("-","").Replace("/","").Replace(" ","").ToUpper()==client.Nit))return "Ese NIT ya está registrado.";
        }
        if(item is Empleado e)
        {
            if(!e.Activo&&(e.FechaEgreso is null||e.FechaEgreso.Value.Date<e.FechaInicioLabores.Date||e.FechaEgreso.Value.Date>DateTime.Today))return "Para registrar el egreso indique una fecha entre el inicio de labores y hoy.";
            if(e.Activo&&e.FechaEgreso is not null)return "Un empleado activo no debe tener fecha de egreso.";
            if(!await db.Puestos.AnyAsync(x=>x.Id==e.PuestoId)) return "El puesto seleccionado no existe.";
            if(e.FechaNacimiento.Date>=DateTime.Today || e.FechaNacimiento.Year<1900) return "La fecha de nacimiento no es válida.";
            if(e.FechaInicioLabores.Date<e.FechaNacimiento.Date || e.FechaInicioLabores.Date>DateTime.Today) return "La fecha de inicio de labores no es válida.";
        }
        if(item is Producto p)
        {
            if(!await db.Marcas.AnyAsync(x=>x.Id==p.MarcaId)) return "La marca seleccionada no existe.";
            if(p.FechaIngreso>DateTime.Now.AddMinutes(5) || p.FechaIngreso.Year<2000) return "Fecha de ingreso inválida.";
            if(p.Imagen is not null && (!p.Imagen.StartsWith("/images/productos/") || p.Imagen.Contains(".."))) return "Ruta de imagen inválida.";
        }
        return null;
    }
}
[Route("api/puestos")] public class PuestosController(PosDb db):CatalogoController<Puesto>(db);
[Route("api/marcas")] public class MarcasController(PosDb db):CatalogoController<Marca>(db);
[Route("api/clientes")] public class ClientesController(PosDb db):CatalogoController<Cliente>(db);
[Route("api/proveedores")] public class ProveedoresController(PosDb db):CatalogoController<Proveedor>(db);
[Route("api/empleados")] public class EmpleadosController(PosDb db):CatalogoController<Empleado>(db);
[Route("api/productos")] public class ProductosController(PosDb db):CatalogoController<Producto>(db);
