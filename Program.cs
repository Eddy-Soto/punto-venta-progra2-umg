using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using PuntoVenta.Components;
using PuntoVenta.Data;
using PuntoVenta.Services;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using System.Security.Cryptography;

var builder=WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath,".keys")))
    .UseCustomCryptographicAlgorithms(new ManagedAuthenticatedEncryptorConfiguration
    { EncryptionAlgorithmType=typeof(Aes),EncryptionAlgorithmKeySize=256,ValidationAlgorithmType=typeof(HMACSHA256) });
builder.Configuration.AddJsonFile("appsettings.Local.json",optional:true,reloadOnChange:false).AddEnvironmentVariables();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddControllers();
builder.Services.AddDbContext<PosDb>(o=>o.UseMySQL(builder.Configuration.GetConnectionString("MySql")
    ?? "Server=localhost;Database=punto_venta_progra2;User ID=puntoventa;Password=CONFIGURAR;"));
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<SessionEvents>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication("Bearer").AddScheme<AuthenticationSchemeOptions,JwtHandler>("Bearer",null);
builder.Services.AddAuthorization();
builder.Services.AddScoped<ApiClient>();
builder.Services.AddHttpClient("PosApi");
builder.Services.AddRateLimiter(o=>
{
    o.RejectionStatusCode=429;
    o.AddPolicy("login",context=>RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString()??"local",
        _=>new FixedWindowRateLimiterOptions{PermitLimit=20,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));
});
var app=builder.Build();
if(args.Length==2 && args[0]=="--export-schema")
{
    await using var schemaScope=app.Services.CreateAsyncScope();
    var schema=schemaScope.ServiceProvider.GetRequiredService<PosDb>().Database.GenerateCreateScript();
    await File.WriteAllTextAsync(args[1],"-- Esquema generado desde las entidades EF Core. Ejecutar en una base NUEVA.\n"+schema);
    return;
}
app.Use(async(context,next)=>
{
    try { await next(); }
    catch(DbUpdateConcurrencyException)
    { context.Response.StatusCode=409; await context.Response.WriteAsJsonAsync(new{message="El registro cambió. Actualice e intente nuevamente."}); }
    catch(DbUpdateException ex)
    {
        context.Response.StatusCode=409;
        var mysql=ex.InnerException as MySqlException;
        var message=mysql?.Number==1062 ? "Ya existe un registro con esos datos (NIT, DPI, nombre o número de documento)."
            :mysql?.Number==1451 ? "No puede eliminar este registro porque está relacionado con otros registros."
            :mysql?.Number==1452 ? "El registro relacionado ya no existe. Actualice e intente nuevamente."
            :"No se pudo guardar. Revise los datos y vuelva a intentarlo.";
        app.Logger.LogWarning(ex,"Error de escritura en base de datos");
        await context.Response.WriteAsJsonAsync(new{message});
    }
    catch(Exception ex) when(context.Request.Path.StartsWithSegments("/api"))
    {
        app.Logger.LogError(ex,"Error en API"); context.Response.StatusCode=500;
        await context.Response.WriteAsJsonAsync(new{message="No se pudo completar la operación. Verifique la conexión a MySQL."});
    }
});
app.UseStaticFiles(); app.UseAuthentication();
app.Use(async(context,next)=>
{
    context.Response.Headers["X-Content-Type-Options"]="nosniff";
    if(context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.Headers.CacheControl="no-store";
        var unsafeMethod=context.Request.Method is "POST" or "PUT" or "DELETE" or "PATCH";
        var cookieUsed=context.Request.Cookies.ContainsKey(TokenService.Cookie)&&!context.Request.Headers.Authorization.ToString().StartsWith("Bearer ");
        if(unsafeMethod&&cookieUsed&&context.Request.Headers["X-PV-Client"]!="1")
        {context.Response.StatusCode=403;await context.Response.WriteAsJsonAsync(new{message="Solicitud de navegador no válida."});return;}
    }
    await next();
});
app.UseAuthorization(); app.UseRateLimiter(); app.UseAntiforgery();
app.MapControllers(); app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
if(app.Configuration.GetValue<bool>("Database:Initialize"))
{
    await using var scope=app.Services.CreateAsyncScope();
    await DbInitializer.Initialize(scope.ServiceProvider.GetRequiredService<PosDb>(),app.Configuration);
}
app.Run();
