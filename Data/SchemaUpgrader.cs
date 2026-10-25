using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
namespace PuntoVenta.Data;
// Actualización aditiva para la base inicial creada mediante EnsureCreated.
// Cada paso comprueba su existencia y puede retomarse sin borrar los datos.
public static class SchemaUpgrader
{
    public static async Task Apply(PosDb db)
    {
        await using var connection=new MySqlConnection(db.Database.GetConnectionString());
        await connection.OpenAsync();
        async Task Execute(string sql){await using var c=new MySqlCommand(sql,connection);await c.ExecuteNonQueryAsync();}
        async Task Add(string table,string column,string definition)
        {
            await using var check=new MySqlCommand("SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name=@t AND column_name=@c",connection);
            check.Parameters.AddWithValue("@t",table);check.Parameters.AddWithValue("@c",column);
            if(Convert.ToInt32(await check.ExecuteScalarAsync())==0) await Execute($"ALTER TABLE `{table}` ADD COLUMN `{column}` {definition}");
        }
        await Add("Empleados","Sueldo","decimal(8,2) NOT NULL DEFAULT 0");
        await Add("Empleados","Activo","tinyint(1) NOT NULL DEFAULT 1");
        await Add("Empleados","FechaEgreso","datetime(6) NULL");
        await Add("Usuarios","EmpleadoId","int NULL");
        await Add("Usuarios","EsAdministrador","tinyint(1) NOT NULL DEFAULT 0");
        await Add("Usuarios","Vendedor","tinyint(1) NOT NULL DEFAULT 0");
        await Add("Usuarios","Comprador","tinyint(1) NOT NULL DEFAULT 0");
        await Add("Usuarios","Administrativo","tinyint(1) NOT NULL DEFAULT 0");
        await Add("Usuarios","Activo","tinyint(1) NOT NULL DEFAULT 1");
        await Add("Compras","RegistradoPorId","int NULL");
        await Add("Compras","ProveedorNombre","varchar(120) NULL");
        await Add("Compras","ProveedorNit","varchar(12) NULL");
        await Add("Ventas","ClienteNombre","varchar(125) NULL");
        await Add("Ventas","ClienteNit","varchar(12) NULL");
        await Add("Ventas","EmpleadoNombre","varchar(125) NULL");
        await using(var check=new MySqlCommand("SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='Clientes' AND column_name IN ('Telefono','CorreoElectronico') AND is_nullable='NO'",connection))
            if(Convert.ToInt32(await check.ExecuteScalarAsync())>0)
                await Execute("ALTER TABLE Clientes MODIFY Telefono varchar(25) NULL, MODIFY CorreoElectronico varchar(45) NULL");
        await Execute("CREATE TABLE IF NOT EXISTS Sesiones (Id varchar(64) NOT NULL PRIMARY KEY, UsuarioId int NOT NULL, TokenHash varchar(64) NOT NULL, ExpiraUtc datetime(6) NOT NULL, RevocadaUtc datetime(6) NULL, INDEX IX_Sesiones_UsuarioId(UsuarioId), CONSTRAINT FK_Sesiones_Usuarios_UsuarioId FOREIGN KEY(UsuarioId) REFERENCES Usuarios(Id) ON DELETE CASCADE)");
        async Task Constraint(string table,string name,string ddl)
        {
            await using var c=new MySqlCommand("SELECT COUNT(*) FROM information_schema.table_constraints WHERE constraint_schema=DATABASE() AND table_name=@t AND constraint_name=@n",connection);
            c.Parameters.AddWithValue("@t",table);c.Parameters.AddWithValue("@n",name);
            if(Convert.ToInt32(await c.ExecuteScalarAsync())==0) await Execute(ddl);
        }
        await Constraint("Usuarios","FK_Usuarios_Empleados_EmpleadoId","ALTER TABLE Usuarios ADD CONSTRAINT FK_Usuarios_Empleados_EmpleadoId FOREIGN KEY(EmpleadoId) REFERENCES Empleados(Id) ON DELETE RESTRICT");
        await Constraint("Usuarios","IX_Usuarios_EmpleadoId","ALTER TABLE Usuarios ADD CONSTRAINT IX_Usuarios_EmpleadoId UNIQUE(EmpleadoId)");
        await Constraint("Compras","FK_Compras_Usuarios_RegistradoPorId","ALTER TABLE Compras ADD CONSTRAINT FK_Compras_Usuarios_RegistradoPorId FOREIGN KEY(RegistradoPorId) REFERENCES Usuarios(Id) ON DELETE RESTRICT");
        await Execute("UPDATE Usuarios SET EsAdministrador=1 WHERE Nombre='admin' AND EmpleadoId IS NULL");
    }
}
