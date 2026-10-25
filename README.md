# Punto de Venta — avance de Programación II

Aplicación en .NET 10, ASP.NET Core Web API con controladores, Blazor Server interactivo, Entity Framework Core 10 y MySQL 8.0. Blazor consume los controladores mediante HttpClient y JWT.

## Abrir la demostración

1. Abra esta carpeta y haga doble clic en **INICIAR.cmd**.
2. Espere el mensaje `PUNTO DE VENTA LISTO`.
3. Abra http://127.0.0.1:5080.
4. Seleccione **Administrador**: usuario **admin**, contraseña generada por el iniciador y mostrada en la consola local.
5. Para acceso de empleado, seleccione **Empleado**: usuario **EMPLEADO**, contraseña generada y mostrada en la consola local. La cuenta inicial está vinculada al primer empleado activo y tiene Vendedor y Comprador. El administrador puede ajustar sus roles en **Cuentas y roles**.

Requiere el SDK .NET 10 y MySQL Server 8.0 en `C:\Program Files\MySQL\MySQL Server 8.0`. Se verificaron ambos en este equipo. El primer arranque tarda más porque crea y configura la base de demostración.

El arranque prepara una instancia independiente de MySQL en el puerto **3307**, enlazada únicamente a 127.0.0.1. Los archivos de datos quedan en `.local/mysql`; la configuración está en `appsettings.Local.json`. El servidor MySQL del sistema utiliza su configuración habitual. Los datos de ejemplo son ficticios. Para cerrar los procesos de la demostración utilice **DETENER.cmd**; los datos se conservan.

Si los puertos 3307 o 5080 están ocupados por otra aplicación, revise el conflicto antes de iniciar. No elimine `.local` ni `appsettings.Local.json`: contienen la base de datos y sus credenciales locales. Nunca publique esos archivos en GitHub. Para una instalación nueva en otra carpeta, copie el código sin esos archivos y ejecute INICIAR.cmd; se crea una base nueva.

## Alcance del avance

| Módulo | Funciones incluidas |
|---|---|
| Puestos y Marcas | Crear, consultar, editar, eliminar, buscar y rechazar duplicados |
| Clientes y Proveedores | CRUD, búsqueda, validación de datos y NIT único |
| Empleados | CRUD, puesto, validación de DPI/fechas, sueldo mensual, estado activo/egresado, fecha de egreso y reingreso |
| Productos | CRUD, marca desde combo, enlaces con Marcas, precios, existencia, fecha e imagen local |
| Compras | Encabezado y varios detalles en una pantalla; proveedor; agregar, editar y quitar líneas antes de confirmar; total; registro transaccional; historial y consulta de detalle |
| Ventas | Cliente y empleado; varios productos; cantidades y totales; precio oficial desde la base; validación de stock; registro transaccional; historial y consulta de detalle |
| Acceso | Administrador/Empleado, contraseñas con hash, JWT y sesión persistente/revocable durante 7 días |
| Roles | Vendedor, Comprador y Administrativo combinables por empleado; administrador controla todo |
| Menú | Opciones obtenidas de una tabla de MySQL con jerarquía padre–hijo |

Una compra aumenta la existencia, actualiza el último precio de costo y calcula el precio de venta con **25 % de recargo sobre el costo**. Una venta disminuye la existencia y usa el precio vigente del producto. La actualización de inventario y el guardado del documento ocurren dentro de la misma transacción. Las filas de productos se bloquean mientras se confirma una operación para evitar sobreventa simultánea.

Los documentos confirmados se pueden consultar. **La edición, anulación o eliminación de compras y ventas ya confirmadas queda pendiente.** Esas operaciones necesitan reglas para revertir inventario y costos. La publicación en IIS dentro del laboratorio Azure UMG y el acceso desde otra máquina virtual se completaron y fueron confirmados por el autor el 23 de octubre de 2026. Los cuatro reportes obligatorios están incluidos. Ya se incluyen los roles solicitados de empleados, con restricciones en la interfaz y en la API.

El porcentaje académico de Maestro–Detalle depende del criterio del docente. Este avance incluye el flujo completo de alta y consulta, las líneas editables antes de confirmar y las reglas principales de Compras/Ventas.

## Guion de demostración de 10 a 15 minutos

1. Iniciar sesión y mostrar el menú.
2. Crear una Marca y un Puesto. Buscar, editar y eliminar un registro temporal sin relaciones.
3. Mostrar Empleados y su combo de Puestos; mostrar Clientes y Proveedores.
4. Crear o editar un Producto. Elegir marca y cargar una imagen PNG/JPG/WebP de hasta 2 MB. Guardar y mostrar la imagen en la lista. La base guarda únicamente la ruta.
5. Revisar la existencia del Teclado inalámbrico (inicialmente 20).
6. En Compras, elegir proveedor, agregar 10 teclados con costo Q100.00 y confirmar. Consultar su detalle. Comprobar existencia 30 y precio de venta Q125.00.
7. En Ventas, buscar el cliente por NIT o pulsar CF (Cliente final). Si no existe el NIT, registrar al cliente en el modal. El administrador elige vendedor; los empleados venden bajo su propia identidad. Intentar agregar 31 teclados: se rechaza por stock insuficiente.
8. Agregar 5 teclados y confirmar: aparece la vista previa de la factura, total Q625.00. Pulsar Registrar venta, revisar el documento confirmado y comprobar existencia 25. Las compras siguen el mismo flujo de revisión y registro.
9. Mostrar que eliminar una marca o un puesto que tenga registros relacionados se rechaza con un mensaje.

Los números de documento se sugieren a partir del historial. Si practica varias veces, continúe usando números nuevos. No vuelva a registrar el mismo número de compra o la misma combinación serie/número de factura. La sesión se recupera al recargar o abrir otra ventana del mismo navegador y perfil. Al cerrar sesión, todas las ventanas que comparten esa sesión vuelven al acceso. La cookie vence a los 7 días; un navegador distinto o una ventana de incógnito requiere su propio acceso.

## Estructura para explicar el código

- `Models/Entities.cs`: entidades y DTO con validaciones.
- `Data/PosDb.cs`: tablas, relaciones, índices únicos y precisión de importes.
- `Data/DbInitializer.cs`: creación inicial, administrador, menú y datos opcionales.
- `Controllers/CatalogosController.cs`: GET, POST, PUT y DELETE de los seis catálogos.
- `Controllers/DocumentosController.cs`: consultas y registro transaccional de Compras/Ventas.
- `Controllers/SistemaController.cs`: acceso, menú e imágenes.
- `Services/Security.cs`: JWT, permisos, validación de sesión y notificación de revocaciones.
- `Controllers/CuentasController.cs`: cuentas y asignación de roles (solo administrador), búsqueda de cliente por NIT.
- `Data/SchemaUpgrader.cs`: actualización aditiva de la base existente.
- `wwwroot/session.js`: acceso mediante cookie y cierre sincronizado entre ventanas.
- `Services/ApiClient.cs`: consumo HTTP de la API desde Blazor.
- `Components/Shared/GenericCrud.razor`: lista, búsqueda, editor y confirmación de eliminación reutilizables.
- `Components/Pages`: formularios y pantallas de operaciones.
- `Database/esquema.sql`: estructura generada desde EF Core para una base nueva.
- `Database/datos-demostracion.sql`: datos de ejemplo; requiere primero el esquema vacío.

Las entidades conservan la información del diagrama, con nombres de propiedades C# y columnas en PascalCase; los identificadores son INT y las rutas de imágenes admiten 255 caracteres. Se añaden Usuarios, Menu y Revision en Productos para detectar ediciones obsoletas. Las contraseñas se guardan como hash. El proyecto utiliza un único host para interfaz y API, con responsabilidades separadas por carpetas; no requiere servicios de nube para esta demostración local.

## Compilar y ejecutar manualmente

Desde esta carpeta:

```powershell
dotnet restore --ignore-failed-sources -p:NuGetAudit=false
dotnet build --no-restore
dotnet run --no-launch-profile --urls http://127.0.0.1:5080
```

MySQL debe estar iniciado y `appsettings.Local.json` configurado. Si la aplicación ya está abierta, use DETENER.cmd antes de recompilar el ejecutable.

## Usar otra base MySQL

Detenga la aplicación. Cree una base vacía, por ejemplo `punto_venta_progra2`, y un usuario con permisos sobre esa base. Copie `appsettings.Local.example.json` a `appsettings.Local.json` y escriba la conexión, una clave JWT aleatoria de al menos 32 caracteres y la contraseña inicial del administrador. Cambie `LocalDemo` a false para que los scripts no intenten administrar su servidor habitual.

Con `Database:Initialize=true`, EF crea las tablas cuando la base está vacía. SchemaUpgrader agrega las columnas y tablas de sesiones/roles a las bases de la versión anterior de forma idempotente. Respalde sus datos antes de actualizar; no elimina ni recrea la base existente. No utilice una base con otras tablas ni importe el SQL sobre datos que quiera conservar. Las claves y contraseñas de demostración deben cambiarse antes de exponer la aplicación fuera del equipo.

## Pruebas

`Pruebas/resultado-integracion.json` registra las pruebas realizadas en una base aislada: seis CRUD, búsqueda, duplicados, relaciones, JWT, compra y venta con varias líneas, precio oficial, rollback, concurrencia e imágenes. Se aprobaron 13 grupos de verificación. `Pruebas/integracion.py` permite repetirlas con Python 3, pero **crea datos de prueba en la API de http://127.0.0.1:5080**; úselo con una instancia destinada a pruebas. No lo ejecute justo antes de presentar sobre su base limpia.

La revisión manual en navegador verificó el inicio de sesión, la creación de Puestos, la compra, la venta, el rechazo por existencias insuficientes y la carga/guardado de imagen.

## Git y GitHub

Esta copia de distribución utiliza un historial inicial limpio para excluir las antiguas credenciales de demostración. El desarrollo original se conserva en el repositorio local del autor. Revise `git status` antes de publicar; `.gitignore` excluye credenciales, datos locales, claves, binarios y las imágenes que suban los usuarios. Al respaldar datos reales copie también las imágenes de `wwwroot/images/productos`.




## Sesiones y permisos de esta actualización

El JWT se guarda en una cookie HttpOnly, SameSite=Strict, con Secure al servir HTTPS. En MySQL se conserva el identificador de la sesión y el SHA-256 del token, junto con su vencimiento/revocación. La contraseña del usuario tiene su hash separado. Cada petición de API comprueba que la sesión y el empleado sigan activos. La interfaz recupera la identidad de la conexión autenticada de Blazor sin almacenar el JWT en localStorage. Las escrituras desde cookie requieren una cabecera que impide enviarlas mediante un formulario de otro sitio.

El cierre de sesión revoca ese token en la API y avisa a las ventanas conectadas. Cambiar roles, desactivar una cuenta o registrar el egreso del empleado revoca sus sesiones existentes. Un token copiado deja de funcionar tras su revocación. La sesión se comparte por navegador/perfil/origen: use siempre la misma dirección, por ejemplo 127.0.0.1, en lugar de alternarla con localhost.

| Acceso | Color y operaciones |
|---|---|
| Administrador | Verde; todos los módulos, cuentas y asignación de roles |
| Vendedor | Azul; NIT/CF, alta de cliente desde la venta, venta y facturas propias |
| Comprador | Ámbar; registro de compras e historial de sus compras |
| Administrativo | Morado; puestos, empleados, ingresos/egresos, sueldos y modificaciones |
| Varios roles | Pantalla de selección con las funciones autorizadas; cada módulo usa su color |

Los empleados no pueden elegir otro vendedor en una venta ni administrar las cuentas de acceso. Las bajas laborales conservan las operaciones históricas. Las compras y ventas confirmadas conservan una copia del nombre/NIT del tercero para que una modificación posterior del catálogo no reescriba el documento. Las operaciones antiguas mantienen su detalle y pueden consultarse.

El dato Sueldo es el salario mensual registrado; esta actualización no calcula planillas, descuentos ni liquidaciones. La vista previa es un documento interno del proyecto.

`Pruebas/sesiones_roles.py` comprueba cookies, revocación, cuatro combinaciones de roles, aislamiento de historiales, búsquedas NIT/CF, alta de cliente, vistas previas, cambios de roles, sueldo y egreso. Configure `PV_TEST_URL` y `PV_TEST_PASSWORD` para una API de pruebas; ambos scripts generan registros y deben ejecutarse en una base destinada a pruebas. No pruebe roles cambiando cuentas de personas reales.

## Reportes (22 de octubre de 2026)

Ingrese como administrador y abra **Reportes** en el menú dinámico.

- Productos: producto, marca, existencia, costo unitario y precio de venta.
- Existencias: unidades, valoración al costo y a precio de venta; filtros de stock bajo (5 o menos) y agotados. La valoración usa los precios actuales.
- Compras: fecha, número, proveedor, NIT y total calculado desde los detalles.
- Ventas: fecha, serie/número, cliente, NIT, empleado y total desde los detalles.

Seleccione el tipo, complete los filtros y pulse **Generar reporte**. Las fechas son opcionales e incluyen ambos extremos. La búsqueda de productos admite nombre o marca; compras admite proveedor o NIT; ventas admite cliente, NIT o empleado. Los filtros aplicados aparecen en el documento. **Imprimir / Guardar PDF** abre la impresión del navegador; seleccione Guardar como PDF si lo necesita.

Los informes son globales y están reservados al administrador: no amplían los permisos de empleados. No modifican existencias ni documentos. La opción se agrega automáticamente al menú de bases existentes al iniciar, sin recrear tablas.

`Pruebas/reportes.py` realiza cinco grupos de verificación sin crear ni modificar datos comerciales (inicia sesiones para validar permisos). Verifica relaciones, valores, fechas, totales, filtros, validaciones y acceso restringido. Resultado en `Pruebas/resultado-reportes.json`. El código está en `Controllers/ReportesController.cs`, `Models/Reportes.cs` y `Components/Pages/Reportes.razor`.

Despliegue IIS y conectividad entre máquinas virtuales: comprobados por el autor. Esta copia está preparada para publicarse en GitHub.

## Credenciales de una instalación nueva

No se distribuyen contraseñas ni hashes de cuentas reales. INICIAR.cmd genera claves aleatorias para admin y EMPLEADO cuando crea una configuración nueva; se muestran únicamente en su consola local. Las instalaciones existentes conservan sus credenciales. Si configura MySQL manualmente, defina Seed:AdminPassword; Seed:EmployeePassword es opcional. El administrador puede crear cuentas desde Cuentas y roles. No publique appsettings.Local.json.

Los scripts de pruebas requieren PV_TEST_PASSWORD; los que usan la cuenta EMPLEADO también requieren PV_EMPLOYEE_PASSWORD. Úselos con una base de pruebas. Los resultados JSON incluidos corresponden a las verificaciones anteriores documentadas, no a una ejecución nueva de todos los tests de esta copia.

El sitio en el laboratorio es privado y solo está disponible con el servidor encendido y conectividad a su red. El repositorio GitHub contiene el código; no es alojamiento de la aplicación .NET ni incluye los datos de la máquina virtual.
