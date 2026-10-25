using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PuntoVenta.Data;
using PuntoVenta.Models;
namespace PuntoVenta.Services;
public static class Permisos
{
    public const string Administrador="Administrador",Vendedor="Vendedor",Comprador="Comprador",Administrativo="Administrativo";
    public static bool Vender(ClaimsPrincipal p)=>p.IsInRole(Administrador)||p.IsInRole(Vendedor);
    public static bool Comprar(ClaimsPrincipal p)=>p.IsInRole(Administrador)||p.IsInRole(Comprador);
    public static bool Personal(ClaimsPrincipal p)=>p.IsInRole(Administrador)||p.IsInRole(Administrativo);
    public static int UserId(ClaimsPrincipal p)=>int.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public static int? EmpleadoId(ClaimsPrincipal p)=>int.TryParse(p.FindFirstValue("empleado"),out var id)?id:null;
    public static string Nit(string? value)=>(value??"").Trim().ToUpperInvariant().Replace("/","").Replace("-","").Replace(" ","");
}
public class TokenService(IConfiguration config)
{
    public const string Cookie="pv_sesion";
    private readonly SymmetricSecurityKey key=new(Encoding.UTF8.GetBytes(config["Jwt:Key"]??throw new InvalidOperationException("Configure Jwt:Key.")));
    public string Create(Usuario user,string sid,DateTime expires)=>new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
        issuer:"PuntoVenta",audience:"PuntoVenta",claims:[new(ClaimTypes.Name,user.Nombre),new(ClaimTypes.NameIdentifier,user.Id.ToString()),new("sid",sid)],expires:expires,
        signingCredentials:new(key,SecurityAlgorithms.HmacSha256)));
    public ClaimsPrincipal Validate(string token)=>new JwtSecurityTokenHandler().ValidateToken(token,new TokenValidationParameters
    {ValidateIssuer=true,ValidIssuer="PuntoVenta",ValidateAudience=true,ValidAudience="PuntoVenta",ValidateIssuerSigningKey=true,IssuerSigningKey=key,ValidateLifetime=true,ClockSkew=TimeSpan.FromSeconds(5),ValidAlgorithms=[SecurityAlgorithms.HmacSha256]},out _);
    public static string Hash(string token)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
// Notifica a los circuitos de todas las ventanas conectadas a la misma sesión.
public class SessionEvents
{
    public event Action<string>? Revoked;
    public void Notify(string sid)=>Revoked?.Invoke(sid);
}
public class JwtHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger,UrlEncoder encoder,TokenService tokens,PosDb db)
    :AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header=Request.Headers.Authorization.ToString();
        var token=header.StartsWith("Bearer ",StringComparison.OrdinalIgnoreCase)?header[7..]:Request.Cookies[TokenService.Cookie];
        if(string.IsNullOrEmpty(token))return AuthenticateResult.NoResult();
        ClaimsPrincipal principal;
        try{principal=tokens.Validate(token);}catch{return AuthenticateResult.Fail("Token inválido o vencido.");}
        var sid=principal.FindFirstValue("sid");
        var session=await db.Sesiones.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==sid);
        if(session is null||session.RevocadaUtc is not null||session.ExpiraUtc<=DateTime.UtcNow||session.TokenHash!=TokenService.Hash(token))return AuthenticateResult.Fail("Sesión cerrada o vencida.");
        var user=await db.Usuarios.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==session.UsuarioId);
        if(user is null||!user.Activo)return AuthenticateResult.Fail("Usuario inactivo.");
        if(user.EmpleadoId is int employee && !await db.Empleados.AnyAsync(x=>x.Id==employee&&x.Activo))return AuthenticateResult.Fail("Empleado inactivo.");
        var identity=(ClaimsIdentity)principal.Identity!;
        if(user.EsAdministrador)identity.AddClaim(new(ClaimTypes.Role,Permisos.Administrador));
        if(user.Vendedor)identity.AddClaim(new(ClaimTypes.Role,Permisos.Vendedor));
        if(user.Comprador)identity.AddClaim(new(ClaimTypes.Role,Permisos.Comprador));
        if(user.Administrativo)identity.AddClaim(new(ClaimTypes.Role,Permisos.Administrativo));
        if(user.EmpleadoId is int id)identity.AddClaim(new("empleado",id.ToString()));
        // Solo se conserva dentro del circuito de servidor, nunca se serializa al navegador.
        identity.AddClaim(new("pv_token",token));
        return AuthenticateResult.Success(new AuthenticationTicket(principal,Scheme.Name));
    }
}
