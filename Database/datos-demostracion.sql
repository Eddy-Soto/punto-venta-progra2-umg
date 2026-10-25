
/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

LOCK TABLES `Clientes` WRITE;
/*!40000 ALTER TABLE `Clientes` DISABLE KEYS */;
INSERT INTO `Clientes` (`Id`,`Nombres`,`Apellidos`,`Nit`,`Genero`,`Telefono`,`CorreoElectronico`,`FechaIngreso`) VALUES (1,'Carlos','Samayoa','3456879',1,'55551001','carlosamayoa@gmail.com','2026-10-17 21:09:19.814206'),(2,'Gertrudis','Arrivillaga','123456789',0,'25654297','Gertrudisa@gmail.com','2026-10-17 21:28:09.693605');
/*!40000 ALTER TABLE `Clientes` ENABLE KEYS */;
UNLOCK TABLES;

LOCK TABLES `Compras` WRITE;
/*!40000 ALTER TABLE `Compras` DISABLE KEYS */;
/*!40000 ALTER TABLE `Compras` ENABLE KEYS */;
UNLOCK TABLES;

LOCK TABLES `Compras_Detalle` WRITE;
/*!40000 ALTER TABLE `Compras_Detalle` DISABLE KEYS */;
/*!40000 ALTER TABLE `Compras_Detalle` ENABLE KEYS */;
UNLOCK TABLES;

LOCK TABLES `Empleados` WRITE;
/*!40000 ALTER TABLE `Empleados` DISABLE KEYS */;
INSERT INTO `Empleados` (`Id`,`Nombres`,`Apellidos`,`Direccion`,`Telefono`,`Dpi`,`Genero`,`FechaNacimiento`,`PuestoId`,`FechaInicioLabores`,`FechaIngreso`,`Sueldo`,`Activo`,`FechaEgreso`) VALUES (1,'Ana','Juarez','Boca del Monte','55553003','0000000000001',0,'2000-01-15 00:00:00.000000',1,'2024-01-01 00:00:00.000000','2026-10-17 21:09:19.859701',0,1,NULL),(2,'Carlos Enrrique','Benavente','Ciudad Guatemala','65498731','1234654165490',1,'2006-10-17 00:00:00.000000',1,'2026-10-17 00:00:00.000000','2026-10-17 21:29:44.999132',0,1,NULL);
/*!40000 ALTER TABLE `Empleados` ENABLE KEYS */;
UNLOCK TABLES;

LOCK TABLES `Marcas` WRITE;
/*!40000 ALTER TABLE `Marcas` DISABLE KEYS */;
INSERT INTO `Marcas` (`Id`,`Nombre`) VALUES (2,'Kingston'),(1,'Logitech');
/*!40000 ALTER TABLE `Marcas` ENABLE KEYS */;
UNLOCK TABLES;

LOCK TABLES `Menu` WRITE;
/*!40000 ALTER TABLE `Menu` DISABLE KEYS */;
INSERT INTO `Menu` (`Id`,`Titulo`,`Ruta`,`PadreId`,`Orden`) VALUES (1,'Productos','catalogos/productos',NULL,1),(2,'Ventas','documentos/ventas',NULL,3),(3,'Compras','documentos/compras',NULL,7),(4,'Marcas','catalogos/marcas',1,2),(5,'Clientes','catalogos/clientes',2,4),(6,'Empleados','catalogos/empleados',2,5),(7,'Proveedores','catalogos/proveedores',3,8),(8,'Puestos','catalogos/puestos',6,6);
/*!40000 ALTER TABLE `Menu` ENABLE KEYS */;
UNLOCK TABLES;

LOCK TABLES `Productos` WRITE;
/*!40000 ALTER TABLE `Productos` DISABLE KEYS */;
INSERT INTO `Productos` (`Id`,`Revision`,`Nombre`,`MarcaId`,`Descripcion`,`Imagen`,`PrecioCosto`,`PrecioVenta`,`Existencia`,`FechaIngreso`) VALUES (1,2,'Teclado inalámbrico',1,'Teclado de demostración','/images/productos/demo-teclado.png',100.00,125.00,20,'2026-10-17 21:09:19.888476'),(2,1,'Mouse óptico',1,'Mouse de demostración','/images/productos/demo-mouse.png',40.00,50.00,15,'2026-10-17 21:09:19.888880'),(3,1,'Memoria USB 64 GB',2,'Memoria de demostración','/images/productos/demo-usb.png',60.00,75.00,5,'2026-10-17 21:09:19.888881');
/*!40000 ALTER TABLE `Productos` ENABLE KEYS */;
UNLOCK TABLES;

LOCK TABLES `Proveedores` WRITE;
/*!40000 ALTER TABLE `Proveedores` DISABLE KEYS */;
INSERT INTO `Proveedores` (`Id`,`Nombre`,`Nit`,`Direccion`,`Telefono`) VALUES (1,'Distribuidora de Tecnología','987654619','Ciudad de Guatemala','55552002');
/*!40000 ALTER TABLE `Proveedores` ENABLE KEYS */;
UNLOCK TABLES;

LOCK TABLES `Puestos` WRITE;
/*!40000 ALTER TABLE `Puestos` DISABLE KEYS */;
INSERT INTO `Puestos` (`Id`,`Nombre`) VALUES (1,'Vendedor');
/*!40000 ALTER TABLE `Puestos` ENABLE KEYS */;
UNLOCK TABLES;

LOCK TABLES `Usuarios` WRITE;
/*!40000 ALTER TABLE `Usuarios` DISABLE KEYS */;
/*!40000 ALTER TABLE `Usuarios` ENABLE KEYS */;
UNLOCK TABLES;

LOCK TABLES `Ventas` WRITE;
/*!40000 ALTER TABLE `Ventas` DISABLE KEYS */;
/*!40000 ALTER TABLE `Ventas` ENABLE KEYS */;
UNLOCK TABLES;

LOCK TABLES `Ventas_Detalle` WRITE;
/*!40000 ALTER TABLE `Ventas_Detalle` DISABLE KEYS */;
/*!40000 ALTER TABLE `Ventas_Detalle` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;
