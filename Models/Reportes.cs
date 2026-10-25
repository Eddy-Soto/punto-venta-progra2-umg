namespace PuntoVenta.Models;
public record ReporteProducto(int Id,string Producto,string Marca,int Existencia,decimal PrecioCosto,decimal PrecioVenta)
{
    public decimal ValorCosto=>Existencia*PrecioCosto;
    public decimal ValorVenta=>Existencia*PrecioVenta;
}
public record ReporteOperacion(int Id,int Numero,string Serie,DateTime Fecha,string Tercero,string Nit,string Empleado,decimal Total);
public class ReporteVista
{
    public string Tipo {get;set;}="";
    public DateTime Generado {get;set;}
    public List<ReporteProducto> Productos {get;set;}=[];
    public List<ReporteOperacion> Operaciones {get;set;}=[];
}
