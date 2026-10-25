using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using PuntoVenta.Models;
namespace PuntoVenta.Services;
public class ApiClient(IHttpClientFactory factory,NavigationManager navigation,IConfiguration configuration,IJSRuntime js,AuthenticationStateProvider auth,SessionEvents events):IDisposable
{
    public string? Token {get;private set;}
    public PerfilUsuario? Perfil {get;private set;}
    public string? Usuario=>Perfil?.Usuario;
    public string? SessionId {get;private set;}
    public bool LoggedIn=>Token is not null&&Perfil is not null;
    public bool Admin=>Perfil?.EsAdministrador==true;
    public bool Vender=>Perfil?.PuedeVender==true;
    public bool Comprar=>Perfil?.PuedeComprar==true;
    public bool Personal=>Perfil?.PuedeAdministrarPersonal==true;
    public event Action? Changed;
    public bool PuedeRuta(string path)
    {
        path=path.Trim('/').Split('?')[0];
        if(Admin||path=="")return true;
        return path switch {"documentos/ventas"=>Vender,"documentos/compras"=>Comprar,"catalogos/empleados" or "catalogos/puestos"=>Personal,_=>false};
    }
    public async Task Restore()
    {
        events.Revoked+=Revoked;
        var principal=(await auth.GetAuthenticationStateAsync()).User;
        Token=principal.FindFirstValue("pv_token");SessionId=principal.FindFirstValue("sid");
        if(Token is not null){try{Perfil=await Get<PerfilUsuario>("api/auth/session");}catch{Clear();}}
        Changed?.Invoke();
    }
    private void Revoked(string sid){if(sid==SessionId)Clear();}
    private void Clear(){Token=null;Perfil=null;SessionId=null;Changed?.Invoke();}
    public async Task Login(LoginRequest request)
    {
        var result=await js.InvokeAsync<string?>("pvSession.login",request);
        if(result is not null)throw new InvalidOperationException(result);
        navigation.NavigateTo(navigation.Uri,forceLoad:true);
    }
    public async Task Logout()
    {
        var result=await js.InvokeAsync<string?>("pvSession.logout");
        if(result is not null)throw new InvalidOperationException(result);
        Clear();navigation.NavigateTo("/",forceLoad:true);
    }
    private HttpClient Client()
    {
        var client=factory.CreateClient("PosApi");client.BaseAddress=new Uri(configuration["ApiBaseUrl"]??navigation.BaseUri);
        if(Token is not null)client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",Token);
        return client;
    }
    public Task<T?> Get<T>(string route)=>Send<T>(HttpMethod.Get,route);
    public async Task<T?> Send<T>(HttpMethod method,string route,object? body=null)
    {
        using var client=Client();using var request=new HttpRequestMessage(method,route);
        if(body is not null)request.Content=JsonContent.Create(body);
        using var response=await client.SendAsync(request);await Check(response);
        return response.StatusCode==System.Net.HttpStatusCode.NoContent?default:await response.Content.ReadFromJsonAsync<T>();
    }
    public async Task<string> Upload(Stream stream,string name)
    {
        using var client=Client();using var content=new MultipartFormDataContent();content.Add(new StreamContent(stream),"file",name);
        using var response=await client.PostAsync("api/imagenes",content);await Check(response);
        var result=await response.Content.ReadFromJsonAsync<JsonElement>();return result.GetProperty("url").GetString()!;
    }
    private async Task Check(HttpResponseMessage response)
    {
        if(response.IsSuccessStatusCode)return;
        if(response.StatusCode==System.Net.HttpStatusCode.Unauthorized){Clear();throw new InvalidOperationException("Su sesión está cerrada o vencida. Inicie sesión nuevamente.");}
        if(response.StatusCode==System.Net.HttpStatusCode.Forbidden)throw new InvalidOperationException("Su rol no tiene permiso para esta operación.");
        var text=await response.Content.ReadAsStringAsync();
        try{var json=JsonDocument.Parse(text).RootElement;
            if(json.TryGetProperty("message",out var msg))throw new InvalidOperationException(msg.GetString());
            if(json.TryGetProperty("errors",out var errors))throw new InvalidOperationException(string.Join(" ",errors.EnumerateObject().SelectMany(x=>x.Value.EnumerateArray().Select(y=>y.GetString()))));
        }catch(JsonException){}
        throw new InvalidOperationException(response.StatusCode==System.Net.HttpStatusCode.TooManyRequests?"Espere un minuto antes de volver a intentar.":"La operación no pudo completarse.");
    }
    public void Dispose()=>events.Revoked-=Revoked;
}
