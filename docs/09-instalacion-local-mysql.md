# MySQL local y Workbench

Instalación verificada el 29 de octubre de 2026.

## Actualización después del pull de develop

Se integró el commit `5913ad7` (`cambios back`) mediante avance directo desde origin/develop. Se conservó la documentación local y se resolvió el conflicto de README manteniendo el contenido actualizado y el enlace a esta guía.

La autenticación actual usa BCrypt y JWT, sin ASP.NET Identity. Se aplicaron las tres migraciones incluidas por el compañero: `AutenticacionSinIdentity`, `SesionesRefreshToken` y `AdministracionCatalogo`. Antes de aplicarlas se comprobó que no había registros de Identity, usuarios ni catálogo, y se guardó un respaldo en `%LOCALAPPDATA%\Awoorld\Backups`.

El historial contiene cinco migraciones y el esquema actual tiene nueve tablas: las siete del dominio, SesionesRefreshToken y __EFMigrationsHistory. La sección de instalación inicial más abajo describe el estado anterior de 15 tablas.

Verificación del código actualizado: compilación sin errores, 148 pruebas aprobadas y modelo sin cambios pendientes respecto de sus migraciones. Se verificaron registro, login, refresh y perfil contra MySQL real; se eliminó la cuenta temporal y se detuvo la API de prueba. La compilación mostró una advertencia NU1900 de consulta de auditoría de NuGet.

No hace falta crear otra migración: las recibidas ya representan el modelo actual. En Workbench, actualiza la lista de tablas para ver el esquema nuevo. La conexión local y sus credenciales siguen iguales.

## Configuración de este equipo

- MySQL Server 8.4.9 y MySQL Workbench 26.10.0.
- Servicio Windows: MySQLAwoorld84, con inicio automático.
- Servidor: 127.0.0.1, puerto 3306; acepta conexiones solo desde este equipo.
- Base: awoorld. Usuario del proyecto: awoorld_dev, con permisos sobre esa base.
- Datos: C:\ProgramData\MySQL\Awoorld84\Data.
- Configuración del servidor: C:\ProgramData\MySQL\Awoorld84\my.ini.
- La contraseña solicitada se configuró para las cuentas locales root y awoorld_dev de esta nueva instancia. No se guarda en este documento.
- ConnectionStrings:Awoorld y Jwt:SigningKey están en User Secrets del proyecto API, fuera de Git. La API los carga en Development.

Los servicios antiguos MySQL80 y MySQL8020 se encontraron detenidos y se conservaron. No inicies simultáneamente otro servidor en el puerto 3306. Las conexiones antiguas a ese puerto podrían apuntar ahora a la instancia nueva: para este proyecto utiliza Awoorld local.

## Cómo se conecta todo

La API recibe las solicitudes. AwoorldDbContext utiliza EF Core y Pomelo para acceder a MySQL con la cadena de User Secrets. Workbench es un cliente visual que conecta al mismo servidor; no hace falta mantenerlo abierto para usar la API.

## Migraciones de la instalación inicial (estado histórico)

1. InicialMySql creó las siete tablas del dominio.
2. AgregarIdentity creó las siete tablas de cuentas, roles y relaciones de Identity.
3. __EFMigrationsHistory registra ambas migraciones. En total hay 15 tablas.

Para aplicar migraciones pendientes en el futuro, desde la raíz del proyecto:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project MundoAwoo.Infrastructure --startup-project MundoAwoo.Api
```

`--project` identifica dónde están las migraciones; `--startup-project` indica qué aplicación aporta la configuración. `database update` consulta el historial y aplica únicamente las pendientes. No vuelvas a crear InicialMySql ni AgregarIdentity y no ejecutes sus scripts SQL después de aplicarlas con EF.

Para revisar las tablas en Workbench, abre Awoorld local con la contraseña local que elegiste y ejecuta:

La conexión está en Home > Imported from MySQL Workbench. Se importaron los perfiles de conexión anteriores junto con Awoorld local, sin sus contraseñas. La conexión SQL se verificó desde la API y el cliente MySQL; la primera apertura interactiva de Workbench solicitará la contraseña.

```sql
USE awoorld;
SHOW TABLES;
SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory;
```

Para arrancar la API:

```powershell
dotnet run --project MundoAwoo.Api --launch-profile https
```

## Verificación real

Se comprobó acceso como awoorld_dev a MySQL 8.4.9. Se aplicaron las dos migraciones y se probaron registro, login y consulta de perfil con JWT contra MySQL real. La solicitud sin token devolvió 401. La cuenta temporal creada para verificar el flujo se eliminó al terminar. El proceso de prueba de la API también se cerró; el servicio MySQL permanece activo.

La contraseña de MySQL no es la contraseña de los usuarios del fandom. Esas cuentas se registran por la API y mantienen la política de contraseñas de Identity.

Referencia: [instalación oficial de MySQL en Windows](https://dev.mysql.com/doc/refman/8.4/en/windows-installation.html).
