using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuntoVenta.Data;
using PuntoVenta.Models;
using PuntoVenta.Services;

namespace PuntoVenta.Controllers;

[ApiController, Authorize, Route("api/documentos/{tipo}")]
public class DocumentosController(PosDb db) : ControllerBase
{
    private bool Allowed(string tipo)=>tipo=="compras"?Permisos.Comprar(User):tipo=="ventas"&&Permisos.Vender(User);
    [HttpGet("siguiente")]public async Task<IActionResult> Siguiente(string tipo)
    {
        if(!Allowed(tipo))return Forbid();
        var last=tipo=="compras"?await db.Compras.MaxAsync(x=>(int?)x.Numero):await db.Ventas.MaxAsync(x=>(int?)x.Numero);
        return Ok((last??0)+1);
    }
    [HttpGet] public async Task<ActionResult<List<DocumentoVista>>> Listar(string tipo)
    {
        if(!Allowed(tipo))return Forbid();
        if(tipo=="compras")
        {
            var compras=await db.Compras.AsNoTracking().Where(x=>User.IsInRole(Permisos.Administrador)||x.RegistradoPorId==Permisos.UserId(User)).Include(x=>x.Detalles).OrderByDescending(x=>x.Id).ToListAsync();
            var proveedores=await db.Proveedores.ToDictionaryAsync(x=>x.Id,x=>x.Nombre);
            var productos=await db.Productos.ToDictionaryAsync(x=>x.Id,x=>x.Nombre);
            return compras.Select(c=>new DocumentoVista(c.Id,c.Numero,"",c.Fecha,c.ProveedorNombre??proveedores[c.ProveedorId],"",
                c.Detalles.Select(d=>new DocumentoLinea(d.ProductoId,productos[d.ProductoId],d.Cantidad,d.PrecioCostoUnitario)).ToList(),c.ProveedorNit??"")).ToList();
        }
        if(tipo!="ventas") return NotFound();
        var ventas=await db.Ventas.AsNoTracking().Where(x=>User.IsInRole(Permisos.Administrador)||x.EmpleadoId==Permisos.EmpleadoId(User)).Include(x=>x.Detalles).OrderByDescending(x=>x.Id).ToListAsync();
        var clientes=await db.Clientes.ToDictionaryAsync(x=>x.Id,x=>x.Nombres+" "+x.Apellidos);
        var empleados=await db.Empleados.ToDictionaryAsync(x=>x.Id,x=>x.Nombres+" "+x.Apellidos);
        var nombres=await db.Productos.ToDictionaryAsync(x=>x.Id,x=>x.Nombre);
        return ventas.Select(v=>new DocumentoVista(v.Id,v.Numero,v.Serie,v.Fecha,v.ClienteNombre??clientes[v.ClienteId],v.EmpleadoNombre??empleados[v.EmpleadoId],
            v.Detalles.Select(d=>new DocumentoLinea(d.ProductoId,nombres[d.ProductoId],d.Cantidad,d.PrecioUnitario)).ToList(),v.ClienteNit??"")).ToList();
    }
    [HttpGet("{id:int}")]public async Task<ActionResult<DocumentoVista>> Obtener(string tipo,int id)
    {
        var result=await Listar(tipo);
        if(result.Value is null)return result.Result!;
        var doc=result.Value.SingleOrDefault(x=>x.Id==id);
        return doc is null?NotFound():doc;
    }
    [HttpPost("previsualizar")]public Task<IActionResult> Preview(string tipo,DocumentoRequest request)=>Procesar(tipo,request,true);
    [HttpPost] public async Task<IActionResult> Crear(string tipo,DocumentoRequest request)
        => await Procesar(tipo,request,false);
    private async Task<IActionResult> Procesar(string tipo,DocumentoRequest request,bool preview)
    {
        if(tipo is not ("compras" or "ventas")) return NotFound();
        if(!Allowed(tipo))return Forbid();
        if(tipo=="ventas"&&!User.IsInRole(Permisos.Administrador))request.EmpleadoId=Permisos.EmpleadoId(User)??0;
        if(request.Fecha.Date>DateTime.Today || request.Fecha.Year<2000) return BadRequest(new{message="La fecha debe estar entre el año 2000 y hoy."});
        if(request.Detalles.Count==0 || request.Detalles.Count>100) return BadRequest(new{message="Agregue entre 1 y 100 productos."});
        if(request.Detalles.Any(x=>x is null)) return BadRequest(new{message="El detalle contiene una línea inválida."});
        if(request.Detalles.Select(x=>x.ProductoId).Distinct().Count()!=request.Detalles.Count)
            return BadRequest(new{message="Un producto no puede repetirse. Modifique su cantidad."});
        var compra=tipo=="compras";
        if(compra && !await db.Proveedores.AnyAsync(x=>x.Id==request.TerceroId)) return BadRequest(new{message="Seleccione un proveedor válido."});
        if(!compra && (!await db.Clientes.AnyAsync(x=>x.Id==request.TerceroId) || !await db.Empleados.AnyAsync(x=>x.Id==request.EmpleadoId&&x.Activo)))
            return BadRequest(new{message="Seleccione cliente y empleado válidos."});
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var productos=new Dictionary<int,Producto>();
        foreach(var linea in request.Detalles.OrderBy(x=>x.ProductoId))
        {
            // Bloqueo por producto, en orden estable: dos ventas no pueden consumir el mismo stock.
            var found=await db.Productos.FromSqlInterpolated($"SELECT * FROM Productos WHERE Id = {linea.ProductoId} FOR UPDATE").ToListAsync();
            var p=found.SingleOrDefault();
            if(p is null) return BadRequest(new{message="Uno de los productos ya no existe."});
            if(!compra && p.Existencia<linea.Cantidad) return Conflict(new{message=$"Existencia insuficiente para {p.Nombre}. Disponible: {p.Existencia}."});
            if(compra && (long)p.Existencia+linea.Cantidad>int.MaxValue) return BadRequest(new{message="La cantidad excede la capacidad del inventario."});
            if(compra && decimal.Round(linea.Precio*1.25m,2,MidpointRounding.AwayFromZero)>999999.99m) return BadRequest(new{message="El costo produce un precio de venta fuera del límite permitido."});
            productos[p.Id]=p;
        }
        var provider=compra?await db.Proveedores.SingleAsync(x=>x.Id==request.TerceroId):null;
        var client=!compra?await db.Clientes.SingleAsync(x=>x.Id==request.TerceroId):null;
        var employee=!compra?await db.Empleados.SingleAsync(x=>x.Id==request.EmpleadoId):null;
        DocumentoVista View(int savedId)=>new(savedId,request.Numero,compra?"":request.Serie.ToUpperInvariant(),request.Fecha.Date,
            compra?provider!.Nombre:client!.NombreCompleto,employee?.NombreCompleto??"",
            request.Detalles.Select(l=>new DocumentoLinea(l.ProductoId,productos[l.ProductoId].Nombre,l.Cantidad,
                compra?decimal.Round(l.Precio,2,MidpointRounding.AwayFromZero):productos[l.ProductoId].PrecioVenta)).ToList(),compra?provider!.Nit:client!.Nit);
        if(preview)return Ok(View(0));
        int id;
        if(compra)
        {
            var c=new Compra{RegistradoPorId=Permisos.UserId(User),ProveedorNombre=provider!.Nombre,ProveedorNit=provider.Nit,Numero=request.Numero,ProveedorId=request.TerceroId,Fecha=request.Fecha.Date};
            foreach(var l in request.Detalles)
            {
                var p=productos[l.ProductoId];
                var costo=decimal.Round(l.Precio,2,MidpointRounding.AwayFromZero);
                p.Existencia+=l.Cantidad; p.PrecioCosto=costo;
                p.PrecioVenta=decimal.Round(costo*1.25m,2,MidpointRounding.AwayFromZero); p.Revision++;
                c.Detalles.Add(new(){ProductoId=p.Id,Cantidad=l.Cantidad,PrecioCostoUnitario=costo});
            }
            db.Compras.Add(c); await db.SaveChangesAsync(); id=c.Id;
        }
        else
        {
            var v=new Venta{ClienteNombre=client!.NombreCompleto,ClienteNit=client.Nit,EmpleadoNombre=employee!.NombreCompleto,Numero=request.Numero,Serie=request.Serie.ToUpperInvariant(),ClienteId=request.TerceroId,EmpleadoId=request.EmpleadoId,Fecha=request.Fecha.Date};
            foreach(var l in request.Detalles)
            {
                var p=productos[l.ProductoId]; p.Existencia-=l.Cantidad; p.Revision++;
                // El precio viene del servidor, nunca se confía en el precio enviado por el navegador.
                v.Detalles.Add(new(){ProductoId=p.Id,Cantidad=l.Cantidad,PrecioUnitario=p.PrecioVenta});
            }
            db.Ventas.Add(v); await db.SaveChangesAsync(); id=v.Id;
        }
        await tx.CommitAsync(); return Created($"/api/documentos/{tipo}/{id}",View(id));
    }
}
