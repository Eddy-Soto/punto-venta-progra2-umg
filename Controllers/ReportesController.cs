using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuntoVenta.Data;
using PuntoVenta.Models;
using PuntoVenta.Services;

namespace PuntoVenta.Controllers;

// Los informes globales incluyen costos y operaciones de todos los empleados.
[ApiController, Authorize(Roles=Permisos.Administrador), Route("api/reportes")]
public class ReportesController(PosDb db):ControllerBase
{
    [HttpGet("{tipo}")]
    public async Task<ActionResult<ReporteVista>> Obtener(string tipo,DateTime? desde=null,DateTime? hasta=null,string? buscar=null,string estado="todos")
    {
        if(tipo is not ("productos" or "existencias" or "compras" or "ventas"))return NotFound();
        if(desde>hasta)return BadRequest(new{message="La fecha inicial no puede ser posterior a la final."});
        if(estado is not ("todos" or "bajo" or "agotado"))return BadRequest(new{message="Estado de existencia inválido."});
        if(buscar?.Length>100)return BadRequest(new{message="La búsqueda admite hasta 100 caracteres."});
        var term=buscar?.Trim()??"";
        var result=new ReporteVista{Tipo=tipo,Generado=DateTime.Now};
        if(tipo is "productos" or "existencias")
        {
            var query=from p in db.Productos.AsNoTracking() join m in db.Marcas on p.MarcaId equals m.Id
                      where term==""||p.Nombre.Contains(term)||m.Nombre.Contains(term)
                      where estado=="todos"||(estado=="agotado"?p.Existencia==0:p.Existencia<=5)
                      orderby p.Nombre,p.Id
                      select new ReporteProducto(p.Id,p.Nombre,m.Nombre,p.Existencia,p.PrecioCosto,p.PrecioVenta);
            result.Productos=await query.ToListAsync();
        }
        else if(tipo=="compras")
        {
            var query=from c in db.Compras.AsNoTracking() join p in db.Proveedores on c.ProveedorId equals p.Id
                      where (!desde.HasValue||c.Fecha>=desde.Value.Date)&&(!hasta.HasValue||c.Fecha.Date<=hasta.Value.Date)
                      select new {c.Id,c.Numero,Serie="",c.Fecha,Tercero=c.ProveedorNombre??p.Nombre,Nit=c.ProveedorNit??p.Nit,Empleado="",
                          Total=c.Detalles.Sum(d=>(decimal?)d.Cantidad*d.PrecioCostoUnitario)??0};
            result.Operaciones=await query.Where(x=>term==""||x.Tercero.Contains(term)||x.Nit.Contains(term)).OrderByDescending(x=>x.Fecha).ThenByDescending(x=>x.Id).Select(x=>new ReporteOperacion(x.Id,x.Numero,x.Serie,x.Fecha,x.Tercero,x.Nit,x.Empleado,x.Total)).ToListAsync();
        }
        else
        {
            var query=from v in db.Ventas.AsNoTracking() join c in db.Clientes on v.ClienteId equals c.Id join e in db.Empleados on v.EmpleadoId equals e.Id
                      where (!desde.HasValue||v.Fecha>=desde.Value.Date)&&(!hasta.HasValue||v.Fecha.Date<=hasta.Value.Date)
                      select new {v.Id,v.Numero,v.Serie,v.Fecha,Tercero=v.ClienteNombre??(c.Nombres+" "+c.Apellidos),Nit=v.ClienteNit??c.Nit,Empleado=v.EmpleadoNombre??(e.Nombres+" "+e.Apellidos),
                          Total=v.Detalles.Sum(d=>(decimal?)d.Cantidad*d.PrecioUnitario)??0};
            result.Operaciones=await query.Where(x=>term==""||x.Tercero.Contains(term)||x.Nit.Contains(term)||x.Empleado.Contains(term)).OrderByDescending(x=>x.Fecha).ThenByDescending(x=>x.Id).Select(x=>new ReporteOperacion(x.Id,x.Numero,x.Serie,x.Fecha,x.Tercero,x.Nit,x.Empleado,x.Total)).ToListAsync();
        }
        return result;
    }
}
