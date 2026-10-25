-- Esquema generado desde las entidades EF Core. Ejecutar en una base NUEVA.
CREATE TABLE `Clientes` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Nombres` varchar(60) NOT NULL,
    `Apellidos` varchar(60) NOT NULL,
    `Nit` varchar(12) NOT NULL,
    `Genero` tinyint(1) NOT NULL,
    `Telefono` varchar(25) NULL,
    `CorreoElectronico` varchar(45) NULL,
    `FechaIngreso` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`)
);


CREATE TABLE `Marcas` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Nombre` varchar(50) NOT NULL,
    PRIMARY KEY (`Id`)
);


CREATE TABLE `Menu` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Titulo` varchar(50) NOT NULL,
    `Ruta` varchar(100) NOT NULL,
    `PadreId` int NULL,
    `Orden` int NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Menu_Menu_PadreId` FOREIGN KEY (`PadreId`) REFERENCES `Menu` (`Id`) ON DELETE RESTRICT
);


CREATE TABLE `Proveedores` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Nombre` varchar(60) NOT NULL,
    `Nit` varchar(12) NOT NULL,
    `Direccion` varchar(80) NOT NULL,
    `Telefono` varchar(25) NOT NULL,
    PRIMARY KEY (`Id`)
);


CREATE TABLE `Puestos` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Nombre` varchar(50) NOT NULL,
    PRIMARY KEY (`Id`)
);


CREATE TABLE `Productos` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Revision` int NOT NULL,
    `Nombre` varchar(50) NOT NULL,
    `MarcaId` int NOT NULL,
    `Descripcion` varchar(100) NOT NULL,
    `Imagen` varchar(255) NULL,
    `PrecioCosto` decimal(8,2) NOT NULL,
    `PrecioVenta` decimal(8,2) NOT NULL,
    `Existencia` int NOT NULL,
    `FechaIngreso` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Productos_Marcas_MarcaId` FOREIGN KEY (`MarcaId`) REFERENCES `Marcas` (`Id`) ON DELETE RESTRICT
);


CREATE TABLE `Empleados` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Sueldo` decimal(8,2) NOT NULL,
    `Activo` tinyint(1) NOT NULL,
    `FechaEgreso` datetime(6) NULL,
    `Nombres` varchar(60) NOT NULL,
    `Apellidos` varchar(60) NOT NULL,
    `Direccion` varchar(80) NOT NULL,
    `Telefono` varchar(25) NOT NULL,
    `Dpi` varchar(15) NOT NULL,
    `Genero` tinyint(1) NOT NULL,
    `FechaNacimiento` datetime(6) NOT NULL,
    `PuestoId` int NOT NULL,
    `FechaInicioLabores` datetime(6) NOT NULL,
    `FechaIngreso` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Empleados_Puestos_PuestoId` FOREIGN KEY (`PuestoId`) REFERENCES `Puestos` (`Id`) ON DELETE RESTRICT
);


CREATE TABLE `Usuarios` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `EmpleadoId` int NULL,
    `EsAdministrador` tinyint(1) NOT NULL,
    `Vendedor` tinyint(1) NOT NULL,
    `Comprador` tinyint(1) NOT NULL,
    `Administrativo` tinyint(1) NOT NULL,
    `Activo` tinyint(1) NOT NULL,
    `Nombre` varchar(50) NOT NULL,
    `PasswordHash` varchar(255) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Usuarios_Empleados_EmpleadoId` FOREIGN KEY (`EmpleadoId`) REFERENCES `Empleados` (`Id`) ON DELETE RESTRICT
);


CREATE TABLE `Ventas` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ClienteNombre` varchar(125) NULL,
    `ClienteNit` varchar(12) NULL,
    `EmpleadoNombre` varchar(125) NULL,
    `Numero` int NOT NULL,
    `Serie` varchar(1) NOT NULL,
    `Fecha` datetime(6) NOT NULL,
    `ClienteId` int NOT NULL,
    `EmpleadoId` int NOT NULL,
    `FechaIngreso` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Ventas_Clientes_ClienteId` FOREIGN KEY (`ClienteId`) REFERENCES `Clientes` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Ventas_Empleados_EmpleadoId` FOREIGN KEY (`EmpleadoId`) REFERENCES `Empleados` (`Id`) ON DELETE RESTRICT
);


CREATE TABLE `Compras` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `RegistradoPorId` int NULL,
    `ProveedorNombre` varchar(120) NULL,
    `ProveedorNit` varchar(12) NULL,
    `Numero` int NOT NULL,
    `ProveedorId` int NOT NULL,
    `Fecha` datetime(6) NOT NULL,
    `FechaIngreso` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Compras_Proveedores_ProveedorId` FOREIGN KEY (`ProveedorId`) REFERENCES `Proveedores` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Compras_Usuarios_RegistradoPorId` FOREIGN KEY (`RegistradoPorId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT
);


CREATE TABLE `Sesiones` (
    `Id` varchar(64) NOT NULL,
    `UsuarioId` int NOT NULL,
    `TokenHash` varchar(64) NOT NULL,
    `ExpiraUtc` datetime(6) NOT NULL,
    `RevocadaUtc` datetime(6) NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Sesiones_Usuarios_UsuarioId` FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
);


CREATE TABLE `Ventas_Detalle` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `VentaId` int NOT NULL,
    `ProductoId` int NOT NULL,
    `Cantidad` int NOT NULL,
    `PrecioUnitario` decimal(8,2) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Ventas_Detalle_Productos_ProductoId` FOREIGN KEY (`ProductoId`) REFERENCES `Productos` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Ventas_Detalle_Ventas_VentaId` FOREIGN KEY (`VentaId`) REFERENCES `Ventas` (`Id`) ON DELETE CASCADE
);


CREATE TABLE `Compras_Detalle` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `CompraId` int NOT NULL,
    `ProductoId` int NOT NULL,
    `Cantidad` int NOT NULL,
    `PrecioCostoUnitario` decimal(8,2) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Compras_Detalle_Compras_CompraId` FOREIGN KEY (`CompraId`) REFERENCES `Compras` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_Compras_Detalle_Productos_ProductoId` FOREIGN KEY (`ProductoId`) REFERENCES `Productos` (`Id`) ON DELETE RESTRICT
);


CREATE UNIQUE INDEX `IX_Clientes_Nit` ON `Clientes` (`Nit`);


CREATE UNIQUE INDEX `IX_Compras_Numero` ON `Compras` (`Numero`);


CREATE INDEX `IX_Compras_ProveedorId` ON `Compras` (`ProveedorId`);


CREATE INDEX `IX_Compras_RegistradoPorId` ON `Compras` (`RegistradoPorId`);


CREATE INDEX `IX_Compras_Detalle_CompraId` ON `Compras_Detalle` (`CompraId`);


CREATE INDEX `IX_Compras_Detalle_ProductoId` ON `Compras_Detalle` (`ProductoId`);


CREATE UNIQUE INDEX `IX_Empleados_Dpi` ON `Empleados` (`Dpi`);


CREATE INDEX `IX_Empleados_PuestoId` ON `Empleados` (`PuestoId`);


CREATE UNIQUE INDEX `IX_Marcas_Nombre` ON `Marcas` (`Nombre`);


CREATE INDEX `IX_Menu_PadreId` ON `Menu` (`PadreId`);


CREATE INDEX `IX_Productos_MarcaId` ON `Productos` (`MarcaId`);


CREATE UNIQUE INDEX `IX_Productos_Nombre_MarcaId` ON `Productos` (`Nombre`, `MarcaId`);


CREATE UNIQUE INDEX `IX_Proveedores_Nit` ON `Proveedores` (`Nit`);


CREATE UNIQUE INDEX `IX_Puestos_Nombre` ON `Puestos` (`Nombre`);


CREATE INDEX `IX_Sesiones_UsuarioId` ON `Sesiones` (`UsuarioId`);


CREATE UNIQUE INDEX `IX_Usuarios_EmpleadoId` ON `Usuarios` (`EmpleadoId`);


CREATE UNIQUE INDEX `IX_Usuarios_Nombre` ON `Usuarios` (`Nombre`);


CREATE INDEX `IX_Ventas_ClienteId` ON `Ventas` (`ClienteId`);


CREATE INDEX `IX_Ventas_EmpleadoId` ON `Ventas` (`EmpleadoId`);


CREATE UNIQUE INDEX `IX_Ventas_Numero_Serie` ON `Ventas` (`Numero`, `Serie`);


CREATE INDEX `IX_Ventas_Detalle_ProductoId` ON `Ventas_Detalle` (`ProductoId`);


CREATE INDEX `IX_Ventas_Detalle_VentaId` ON `Ventas_Detalle` (`VentaId`);
