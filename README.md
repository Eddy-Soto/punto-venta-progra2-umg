# Punto de Venta · Programación II · UMG

Este proyecto permite administrar un pequeño punto de venta: mantener los catálogos, recibir compras, registrar ventas y consultar reportes. Se desarrolló con .NET 10, ASP.NET Core Web API, Blazor interactivo, Entity Framework Core y MySQL 8.0.

## Para el docente: cómo probarlo

La forma más sencilla de revisarlo es ejecutar la demostración en Windows. El iniciador prepara su propia base con datos ficticios; no necesita las contraseñas del estudiante ni acceso a Azure.

### 1. Antes de comenzar

Instale estos dos componentes si aún no los tiene:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). Para esta opción se necesita el SDK; el Runtime por sí solo no compila el proyecto.
- [MySQL Server 8.0](https://dev.mysql.com/downloads/installer/). MySQL Workbench por sí solo no incluye el servidor. El iniciador busca los ejecutables en `C:\Program Files\MySQL\MySQL Server 8.0\bin`.

Necesitará Internet durante la primera compilación para descargar los paquetes NuGet. La carpeta debe permitir escritura. Si MySQL está instalado en otra ubicación, ajuste `$mysqlHome` en `Iniciar.ps1`.

### 2. Descargar y abrir

1. En GitHub, seleccione **Code → Download ZIP** y extraiga todo su contenido. También puede clonar el repositorio con Git.
2. Abra la carpeta donde aparecen `PuntoVenta.csproj`, `INICIAR.cmd` y `Program.cs`.
3. Haga doble clic en **INICIAR.cmd**. Mantenga abierta la consola hasta ver **PUNTO DE VENTA LISTO**. El primer inicio tarda más porque prepara la base y compila.
4. Abra **http://127.0.0.1:5080**.

El iniciador utiliza una instancia independiente de MySQL en `127.0.0.1:3307` y la aplicación en el puerto `5080`. Si esos puertos están ocupados por otro programa, primero resuelva el conflicto.

### 3. Credenciales para ingresar

| Pestaña de acceso | Usuario | Contraseña |
|---|---|---|
| Administrador | `admin` | La que aparece como **Clave inicial admin (privada)** en la consola de inicio |
| Empleado | `EMPLEADO` | La que aparece como **Clave inicial EMPLEADO (privada)** en esa misma consola |

Las contraseñas se generan automáticamente para cada instalación nueva. Copie la clave completa de la consola y péguela en el formulario. Se conservan al volver a iniciar; no es necesario crear una cuenta de GitHub para ingresar al sistema. Si después cambia una contraseña desde Cuentas y roles, utilice la nueva; la clave inicial mostrada en consola no se actualiza con ese cambio.

El administrador tiene acceso a todo. El empleado de demostración tiene los roles de Vendedor y Comprador. Para revisar el rol Administrativo o probar combinaciones distintas, ingrese como administrador y utilice **Cuentas y roles**. Cada empleado necesita su propia cuenta; registrar al empleado no crea automáticamente su acceso.

### 4. Recorrido sugerido para evaluar

1. Inicie sesión como administrador y observe el menú y el resumen del negocio.
2. En Marcas y Puestos, cree un registro temporal, búsquelo, edítelo y elimínelo.
3. Revise Clientes, Proveedores y Empleados. Compruebe los campos obligatorios, las listas relacionadas y los enlaces entre mantenimientos.
4. Cree un producto con su marca, costo, precio, existencia e imagen PNG, JPG o WebP de hasta 2 MB.
5. Registre una compra de 10 unidades con costo Q100.00. Revise la vista previa y confirme: la existencia aumenta en 10, el costo queda en Q100.00 y el precio de venta en Q125.00.
6. En Ventas, busque un cliente por NIT o seleccione CF. Un NIT nuevo abre el formulario de alta de cliente.
7. Intente vender más unidades de las disponibles: la operación debe rechazarse. Después venda 2 unidades, revise la factura y confirme; el total será Q250.00 si el producto conserva el precio anterior.
8. Consulte los historiales y los reportes de Productos, Existencias, Compras y Ventas. Pruebe búsqueda, fechas y filtros de inventario. Imprimir / Guardar PDF utiliza el diálogo del navegador.
9. Ingrese como empleado y compruebe que solo aparezcan las funciones de sus roles.
10. Abra otra ventana del mismo navegador y dirección para comprobar que la sesión continúa. Al cerrar sesión, las ventanas que comparten esa sesión vuelven al acceso.

Las compras y ventas confirmadas se consultan, pero no se editan ni se anulan. Las líneas sí se pueden corregir antes de registrar el documento. El documento imprimible es una factura interna del proyecto. El campo Sueldo registra el salario mensual; no calcula planillas ni liquidaciones.

### 5. Cerrar la demostración

Ejecute **DETENER.cmd**. Se detienen los procesos de la demostración y los registros se conservan para el siguiente inicio. No elimine `.local`, `.keys` ni `appsettings.Local.json`: contienen datos y configuración privada. Para respaldar una instalación también debe conservar sus imágenes.

## Qué incluye

- CRUD de Puestos, Empleados, Clientes, Proveedores, Marcas y Productos, con búsqueda y validaciones.
- Imágenes en archivos locales; MySQL almacena su ruta.
- Compras y Ventas Maestro–Detalle con varios productos, vistas previas, totales e historiales.
- Actualización transaccional de inventario, costo de última compra y precio de venta con 25 % de recargo sobre costo.
- Validación del precio oficial y de existencias, con protección frente a ventas simultáneas.
- JWT, contraseñas con hash, sesión persistente durante siete días y cierre sincronizado de ventanas.
- Roles combinables de Vendedor, Comprador y Administrativo; asignación de cuentas por el administrador.
- Menú dinámico almacenado en MySQL, con estructura padre–hijo.
- Cuatro reportes globales reservados al administrador.

## Organización del código

| Carpeta o archivo | Responsabilidad |
|---|---|
| `Program.cs` | Configuración, servicios, autenticación y arranque |
| `Models` | Entidades, solicitudes y validaciones |
| `Data/PosDb.cs` | Relaciones y configuración de MySQL mediante EF Core |
| `Data/DbInitializer.cs` | Datos iniciales y menú |
| `Controllers/CatalogosController.cs` | CRUD de los seis catálogos |
| `Controllers/DocumentosController.cs` | Reglas de compras, ventas e inventario |
| `Controllers/ReportesController.cs` | Consultas de los reportes |
| `Controllers/CuentasController.cs` | Cuentas, roles y búsqueda NIT |
| `Services` | Consumo HTTP de API, JWT y sesiones |
| `Components/Pages` | Pantallas Blazor |
| `Database` | Esquema y datos ficticios de referencia |
| `Pruebas` | Pruebas de integración y resultados registrados |

La interfaz consume los controladores REST mediante HttpClient. El modelo corresponde a las entidades, la vista a Blazor y los controladores a la API. Interfaz y API se alojan en la misma aplicación.

## Abrir el código fuente

En Visual Studio abra `PuntoVenta.csproj`. En Visual Studio Code abra la carpeta completa. Puede consultar varios archivos a la vez. Para la primera ejecución local utilice INICIAR.cmd, porque prepara MySQL y la configuración que necesita la aplicación.

## Si algo no inicia

| Situación | Qué revisar |
|---|---|
| No reconoce `dotnet` | Instale .NET 10 SDK y vuelva a abrir la consola |
| No encuentra `mysqld.exe` | Verifique MySQL Server 8.0 y la ruta `$mysqlHome` |
| Puerto 3307 o 5080 ocupado | Cierre la otra instancia o identifique el programa que usa el puerto |
| La página dejó de responder | Confirme que el iniciador terminó y revise `.local/app-out.log` y `.local/app-error.log` |
| MySQL no inicia | Revise `.local/mysql-error.log`; no elimine la base para corregirlo |
| La contraseña no funciona | Seleccione la pestaña correcta y copie la clave de su instalación, sin espacios adicionales |
| No recupera sesión en otra ventana | Use el mismo navegador, perfil y dirección; `localhost` y `127.0.0.1` son orígenes distintos |

Los logs y la configuración local pueden contener información privada; no deben adjuntarse a un repositorio público sin revisión.

## Instalación en IIS y laboratorio UMG

El proyecto se publicó en IIS dentro del laboratorio y el autor confirmó el acceso desde otra máquina virtual el 23 de octubre de 2026. Esa instancia requiere que el servidor esté encendido y que el equipo cliente tenga conectividad a la red del laboratorio. Las credenciales de esa instancia se entregan al docente por un medio privado; las de una copia nueva son las indicadas arriba.

Para publicar una instalación propia:

1. Instale IIS con su consola y WebSocket Protocol; después instale .NET 10 Hosting Bundle.
2. Genere la publicación: `dotnet publish -c Release -o C:\Sitios\PuntoVenta`.
3. Configure MySQL y un archivo `appsettings.Local.json` privado usando el ejemplo incluido. Ajuste la conexión, clave JWT y `ApiBaseUrl` a la dirección real del sitio.
4. Para una base vacía, configure `Database:Initialize=true` y una clave `Seed:AdminPassword` de al menos 10 caracteres durante la preparación; esta fase necesita permisos para crear y actualizar el esquema. `Seed:EmployeePassword` es opcional. Para una base completa ya importada, utilice `Database:Initialize=false` y una cuenta de aplicación con permisos de lectura y escritura.
5. Cree un sitio IIS y un grupo dedicado con No Managed Code. La identidad del grupo necesita lectura de la publicación y modificación en `.keys` y `wwwroot/images/productos`.
6. Permita el puerto HTTP/HTTPS únicamente desde la red autorizada y pruebe desde otro equipo. No es necesario exponer MySQL a los clientes.

El repositorio contiene código; GitHub Pages no ejecuta esta aplicación .NET. No incluye respaldos reales, sesiones, contraseñas del servidor ni imágenes privadas. El SQL de ejemplo no incluye cuentas con contraseñas predefinidas; el iniciador crea los accesos mediante configuración.

## Pruebas realizadas

La versión funcional anterior registró 13 grupos de integración, 11 de sesiones y roles y 5 de reportes. Los resultados están en `Pruebas/resultado-*.json`. También se revisaron los flujos en navegador. La copia preparada para GitHub compiló sin errores; los registros históricos no representan una nueva ejecución de todas las pruebas sobre esta copia.

Para repetir los scripts configure `PV_TEST_URL` y `PV_TEST_PASSWORD`; `reportes.py` también necesita `PV_EMPLOYEE_PASSWORD`. Use una base destinada a pruebas: `integracion.py` y `sesiones_roles.py` crean registros. `reportes.py` verifica consultas sin modificar datos comerciales. Las sesiones de prueba se generan en la base indicada.

## Autor

Eddy Soto · Programación II · Universidad Mariano Gálvez de Guatemala.
