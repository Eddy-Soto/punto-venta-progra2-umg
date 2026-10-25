using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuntoVenta.Data;
using PuntoVenta.Models;
using PuntoVenta.Services;
namespace PuntoVenta.Controllers;
[ApiController, Route("api")]
public class SistemaController(PosDb db,TokenService tokens,IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("auth/login"), Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user=await db.Usuarios.SingleOrDefaultAsync(x=>x.Nombre==request.Usuario.Trim());
        if(user is null||!user.Activo||new PasswordHasher<Usuario>().VerifyHashedPassword(user,user.PasswordHash,request.Password)==PasswordVerificationResult.Failed)
            return Unauthorized(new{message="Usuario o contraseña incorrectos."});
        if(request.Acceso!=""&&request.Acceso!=(user.EsAdministrador?"administrador":"empleado"))
            return Unauthorized(new{message="Seleccione el tipo de acceso que corresponde a su cuenta."});
        if(user.EmpleadoId is int id&&!await db.Empleados.AnyAsync(x=>x.Id==id&&x.Activo))
            return Unauthorized(new{message="El empleado no está activo."});
        var expires=DateTime.UtcNow.AddDays(7);var sid=Guid.NewGuid().ToString("N");
        var token=tokens.Create(user,sid,expires);
        db.Sesiones.Add(new(){Id=sid,UsuarioId=user.Id,TokenHash=TokenService.Hash(token),ExpiraUtc=expires});
        await db.SaveChangesAsync();
        var profile=await Profile(user);
        if(Request.Headers["X-PV-Client"]=="1")
        {
            Response.Cookies.Append(TokenService.Cookie,token,new CookieOptions{HttpOnly=true,Secure=Request.IsHttps,SameSite=SameSiteMode.Strict,Path="/",Expires=expires,IsEssential=true});
            return Ok(new{usuario=user.Nombre,perfil=profile,expiraUtc=expires});
        }
        return Ok(new LoginResponse(token,user.Nombre,profile,expires));
    }
    private async Task<PerfilUsuario> Profile(Usuario user)
    {
        var employee=user.EmpleadoId is int id?await db.Empleados.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id):null;
        return new(user.Id,user.Nombre,user.EmpleadoId,employee?.NombreCompleto??"Administrador",user.EsAdministrador,user.Vendedor,user.Comprador,user.Administrativo);
    }
    [HttpGet("auth/session"),Authorize]
    public async Task<ActionResult<PerfilUsuario>> Session()=>await Profile(await db.Usuarios.SingleAsync(x=>x.Id==Permisos.UserId(User)));
    [HttpPost("auth/logout"),Authorize]
    public async Task<IActionResult> Logout([FromServices]SessionEvents events)
    {
        var sid=User.FindFirst("sid")!.Value;
        await db.Sesiones.Where(x=>x.Id==sid&&x.RevocadaUtc==null).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.RevocadaUtc,DateTime.UtcNow));
        Response.Cookies.Delete(TokenService.Cookie,new CookieOptions{Path="/",Secure=Request.IsHttps,SameSite=SameSiteMode.Strict});
        events.Notify(sid);return NoContent();
    }
    [HttpGet("menu"),Authorize]
    public async Task<List<MenuItem>> Menu()
    {
        bool Can(string path)=>User.IsInRole(Permisos.Administrador)||path switch
        {"documentos/ventas"=>Permisos.Vender(User),"documentos/compras"=>Permisos.Comprar(User),"catalogos/empleados" or "catalogos/puestos"=>Permisos.Personal(User),_=>false};
        var list=(await db.Menu.AsNoTracking().OrderBy(x=>x.Orden).ToListAsync()).Where(x=>Can(x.Ruta)).ToList();
        var ids=list.Select(x=>x.Id).ToHashSet();foreach(var item in list)if(item.PadreId is int parent&&!ids.Contains(parent))item.PadreId=null;
        return list;
    }
    [HttpPost("imagenes"),Authorize(Roles=Permisos.Administrador),RequestSizeLimit(3*1024*1024)]
    public async Task<IActionResult> Imagen()
    {
        if(!Request.HasFormContentType) return BadRequest(new{message="Seleccione una imagen."});
        var file=(await Request.ReadFormAsync()).Files.FirstOrDefault();
        if(file is null || file.Length==0 || file.Length>2*1024*1024) return BadRequest(new{message="Imagen requerida. Máximo 2 MB."});
        await using var stream=file.OpenReadStream();
        var buffer=new byte[12]; var read=await stream.ReadAsync(buffer);
        var ext=read>=8 && buffer.Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}) ? ".png"
            :read>=3 && buffer[0]==255 && buffer[1]==216 && buffer[2]==255 ? ".jpg"
            :read>=12 && System.Text.Encoding.ASCII.GetString(buffer,0,4)=="RIFF" && System.Text.Encoding.ASCII.GetString(buffer,8,4)=="WEBP" ? ".webp" : null;
        if(ext is null) return BadRequest(new{message="Solo se permiten imágenes PNG, JPG o WebP válidas."});
        var folder=Path.Combine(environment.WebRootPath,"images","productos"); Directory.CreateDirectory(folder);
        var name=Guid.NewGuid().ToString("N")+ext;
        await using var output=System.IO.File.Create(Path.Combine(folder,name));
        await output.WriteAsync(buffer.AsMemory(0,read)); await stream.CopyToAsync(output);
        return Ok(new{url="/images/productos/"+name});
    }
}
