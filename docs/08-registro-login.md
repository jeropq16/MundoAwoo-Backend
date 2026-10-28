# Registro y login con ASP.NET Identity y JWT

## 1. Qué quedó conectado

| Método | Ruta | Resultado correcto |
| --- | --- | --- |
| POST | /api/autenticacion/registro | 201: crea cuenta, usuario, perfil y avatar; devuelve JWT |
| POST | /api/autenticacion/login | 200: comprueba credenciales y devuelve un nuevo JWT |
| GET | /api/perfil | 200: consulta el perfil del propietario del JWT |
| PUT | /api/perfil | 200: guarda los cambios del perfil del propietario del JWT |

Registro y login tienen AllowAnonymous: son el punto de entrada para quien aún no tiene token. El perfil sigue protegido. Las respuestas de autenticación usan Cache-Control: no-store y no crean cookies.

## 2. Usuario de dominio y cuenta de Identity

Usuario continúa en Domain y representa al miembro de Awoorld. UsuarioIdentity está en Infrastructure y hereda de IdentityUser<Guid>; Identity administra sus credenciales, hash de contraseña, normalización de correo, fallos y bloqueo.

```text
AspNetUsers (UsuarioIdentity)
    Id: identificador de la cuenta de Identity
    UsuarioId: clave foránea única → Usuarios.Id
    Email / NormalizedEmail
    PasswordHash
    AccessFailedCount / LockoutEnd / LockoutEnabled
                  ↓
Usuarios (Usuario)
    ├── PerfilesUsuario
    ├── Avatares
    └── ItemsDesbloqueados
```

Los dos identificadores pueden ser diferentes. El JWT usa **UsuarioIdentity.UsuarioId** en `sub`, porque PerfilService busca al usuario de dominio. Así conectamos la autenticación con el modelo que ya teníamos.

El índice único de UsuarioId permite como máximo una cuenta Identity por usuario de dominio. El índice único de NormalizedEmail evita duplicar el correo incluso ante escrituras concurrentes. Identity también valida el correo antes de guardar; las dos protecciones cumplen funciones distintas.

Los usuarios creados antes de esta migración se conservan. No reciben automáticamente una contraseña o una cuenta Identity. El registro crea un usuario nuevo; no vincula cuentas a perfiles existentes por un Id proporcionado por el cliente.

## 3. Archivos principales

| Archivo | Responsabilidad |
| --- | --- |
| Application/DTOs/Autenticacion/RegistroDto.cs | Correo, contraseña y nombre visible de entrada |
| Application/DTOs/Autenticacion/LoginDto.cs | Correo y contraseña para entrar |
| Application/DTOs/Autenticacion/AutenticacionRespuestaDto.cs | UsuarioId, Token, TipoToken y ExpiraEnUtc |
| Application/Interfaces/IAutenticacionService.cs | Contrato de registro y login |
| Application/Interfaces/IJwtTokenService.cs | Contrato para generar el token |
| Infrastructure/Identity/UsuarioIdentity.cs | Cuenta de Identity vinculada al dominio |
| Infrastructure/Identity/AutenticacionService.cs | Implementa los dos casos con Identity y EF Core |
| Infrastructure/Security/JwtTokenService.cs | Crea JWT firmados |
| API/Controllers/AutenticacionController.cs | Traduce las solicitudes HTTP a llamadas al servicio |

AutenticacionService vive en Infrastructure porque usa directamente UserManager, SignInManager y una transacción de EF Core, como definimos en la arquitectura inicial. Application conserva los contratos sin importar Identity ni MySQL. El servicio de perfil sigue en Application.

## 4. Registro, paso a paso

RegistrarAsync hace lo siguiente:

1. Comprueba que existan los datos y que la operación no esté cancelada.
2. Quita espacios exteriores del correo y valida su formato y límite de 256 caracteres.
3. Comprueba que la contraseña exista y tenga como máximo 128 caracteres. No la recorta ni cambia mayúsculas.
4. Crea Usuario, que valida el nombre y crea PerfilUsuario; crea también Avatar vacío con su UsuarioId.
5. Busca si ya hay una cuenta con el correo.
6. Abre una transacción y deja el usuario y el avatar pendientes en el contexto.
7. Crea UsuarioIdentity y llama a UserManager.CreateAsync(cuenta, contraseña).
8. Identity valida la fortaleza, genera el hash y guarda usando el mismo contexto.
9. Genera el token y confirma la transacción. Solo entonces devuelve éxito.

El método clave es:

```csharp
IdentityResult resultado = await _cuentas.CreateAsync(cuenta, datos.Contrasena);
```

Succeeded indica si Identity pudo crear la cuenta. Errors contiene códigos como PasswordTooShort; los traducimos a mensajes sencillos en español. No devolvemos la contraseña ni el hash.

La política actual exige de 12 a 128 caracteres, con mayúscula, minúscula, número y símbolo. El mínimo y las clases de caracteres se configuran en Identity; el máximo se valida en el servicio. Un nombre vacío o de más de 50 caracteres se rechaza desde Domain.

## 5. Por qué el registro usa una transacción explícita

UserManager.CreateAsync guarda mediante el almacén EF de Identity. No espera a que llamemos a nuestro UnitOfWork. Por eso AwoorldDbContext ahora hereda de IdentityDbContext<UsuarioIdentity, IdentityRole<Guid>, Guid> y comparte todas las tablas en la misma conexión.

```csharp
await using var transaccion = await _contexto.Database.BeginTransactionAsync(cancellationToken);

// Preparar usuario, perfil y avatar; crear la cuenta con Identity.
// Si se lanza una excepción, salir del bloque revierte la transacción no confirmada.

await transaccion.CommitAsync(cancellationToken);
```

No llamamos adicionalmente a UnitOfWork durante este flujo. Identity ya ejecuta SaveChanges con el mismo contexto. La transacción externa mantiene esos cambios sin confirmar hasta completar también la creación del token. Esto no se podría conseguir con dos contextos independientes sin coordinación adicional.

Si otro registro usa el mismo correo entre la consulta inicial y la inserción, el índice único rechaza el duplicado. Tras revertir, el servicio comprueba si existe ese correo y responde 409; otros errores de persistencia se propagan como fallos del servidor.

## 6. Login, paso a paso

1. Valida y normaliza el correo; comprueba que la contraseña esté presente y dentro del límite.
2. Busca la cuenta con FindByEmailAsync. La normalización de Identity permite buscar sin distinguir mayúsculas del correo.
3. Comprueba contraseña y bloqueo con CheckPasswordSignInAsync.
4. Si no hay cuenta, la contraseña falla o existe bloqueo, responde con el mismo error 401.
5. Si la comprobación tiene éxito, emite un token para cuenta.UsuarioId.

```csharp
SignInResult resultado = await _acceso.CheckPasswordSignInAsync(
    cuenta, datos.Contrasena, lockoutOnFailure: true);
```

CheckPasswordSignInAsync verifica el acceso sin iniciar una sesión de cookies. Al activar lockoutOnFailure, Identity cuenta fallos y bloquea el login durante cinco minutos al alcanzar cinco intentos fallidos. Un acceso correcto restablece el contador según el flujo de Identity. No aplicamos las reglas de fortaleza de una contraseña nueva durante el login; validamos la contraseña ya almacenada.

Los métodos de UserManager/SignInManager usados aquí no reciben CancellationToken. El servicio comprueba cancelación en sus límites y lo pasa a la transacción; esto no significa que pueda interrumpir cada operación interna de Identity.

## 7. Cómo emitimos el token

JwtTokenService toma la misma configuración que el validador:

```json
"Jwt": {
  "Issuer": "Awoorld",
  "Audience": "Awoorld.Api",
  "DuracionMinutos": 15
}
```

SigningKey sigue fuera de Git, en User Secrets o en Jwt__SigningKey. La duración debe estar entre 1 y 60 minutos; el valor inicial es 15. Reinicia la API después de cambiar la clave o esta configuración.

Claims del token:

- sub: Id del usuario del dominio.
- jti: identificador nuevo para cada token; dos logins pueden emitir tokens diferentes incluso en el mismo segundo.
- iat: instante de emisión.
- nbf y exp: inicio de validez y expiración.
- iss y aud: emisor y destinatario.

La firma usa HS256. La respuesta contiene la expiración UTC real del token. El contenido JWT se puede leer: la firma protege su integridad, no lo cifra. Por eso no incluimos credenciales ni información privada innecesaria.

AspNetUserTokens es una tabla estándar de Identity, pero estos JWT de acceso no se guardan allí. El cliente los envía en Authorization: Bearer y la API valida su firma en cada solicitud.

## 8. Migración AgregarIdentity

La nueva migración añade siete tablas estándar:

- AspNetUsers.
- AspNetRoles.
- AspNetUserClaims.
- AspNetRoleClaims.
- AspNetUserRoles.
- AspNetUserLogins.
- AspNetUserTokens.

Las siete tablas del dominio permanecen. Las tablas de roles y proveedores externos forman parte del modelo estándar; no hay todavía endpoints para administrar roles ni login social.

La migración ya está generada en Infrastructure/Persistence/Migrations. No vuelvas a ejecutar migrations add AgregarIdentity. Tras instalar/configurar MySQL y establecer ConnectionStrings:Awoorld y Jwt:SigningKey, ejecuta desde la raíz:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet tool restore
dotnet restore MundoAwoo.sln --disable-parallel -m:1
dotnet build MundoAwoo.sln --no-restore -m:1
dotnet ef database update --project MundoAwoo.Infrastructure --startup-project MundoAwoo.Api --no-build
```

Si ya aplicaste InicialMySql, EF aplicará solamente AgregarIdentity. Si la base aún no tiene las tablas, aplicará ambas migraciones en orden. No hace falta borrar la base.

`docs/AgregarIdentity.sql` contiene el script incremental desde InicialMySql. Sirve para revisar en Workbench o como alternativa de aplicación sobre una base que ya tenga esa migración inicial. No lo ejecutes de nuevo después de database update ni lo uses solo sobre una base vacía.

Para generarlo nuevamente después de compilar:

```powershell
dotnet ef migrations script InicialMySql AgregarIdentity --project MundoAwoo.Infrastructure --startup-project MundoAwoo.Api --output docs/AgregarIdentity.sql --no-build
```

Verifica en Workbench:

```sql
USE awoorld;
SELECT * FROM __EFMigrationsHistory;

SELECT c.Email, c.UsuarioId, p.NombreVisible, a.Id AS AvatarId
FROM AspNetUsers AS c
JOIN Usuarios AS u ON u.Id = c.UsuarioId
JOIN PerfilesUsuario AS p ON p.UsuarioId = u.Id
JOIN Avatares AS a ON a.UsuarioId = u.Id;
```

No creamos ni modificamos una base MySQL real en este turno. Las migraciones y el SQL quedan preparados para cuando tengas el servidor configurado.

## 9. Probar registro, login y perfil

Inicia la API con el perfil HTTPS después de aplicar las migraciones:

```powershell
dotnet run --project MundoAwoo.Api --launch-profile https --no-build
```

En Postman, POST https://localhost:7018/api/autenticacion/registro, Body > raw > JSON:

```json
{
  "correo": "fan@example.com",
  "contrasena": "Ejemplo-Awoo-2026!",
  "nombreVisible": "Awoo Fan"
}
```

Esta contraseña es solo ilustrativa; usa una propia para tus cuentas reales. La respuesta 201 contiene usuarioId, token, tipoToken y expiraEnUtc. No pide un token previo.

Para login, POST https://localhost:7018/api/autenticacion/login:

```json
{
  "correo": "fan@example.com",
  "contrasena": "Ejemplo-Awoo-2026!"
}
```

La respuesta 200 devuelve un nuevo token. Copia el valor de token en Authorization > Bearer Token de GET https://localhost:7018/api/perfil. Para actualizar usa PUT sobre esa ruta:

```json
{
  "nombreVisible": "Fan de la gira",
  "biografia": "Mi álbum favorito es..."
}
```

No pegues contraseñas ni tokens reales en el chat o en archivos del repositorio. La clave de firma nunca debe llegar al frontend.

## 10. Errores y límites de esta primera versión

| Estado | Situación |
| --- | --- |
| 400 | Datos incompletos, correo inválido, contraseña nueva débil, campos desconocidos o nombre inválido |
| 401 | Login con correo/contraseña incorrectos o cuenta temporalmente bloqueada |
| 409 | Correo ya registrado |
| 500 | Fallo inesperado de persistencia o emisión del token |

La respuesta de login no distingue correo inexistente, contraseña incorrecta y bloqueo. El registro sí informa que el correo ya está registrado. El middleware conserva ProblemDetails y no devuelve hashes, contraseñas ni detalles técnicos. Las excepciones de validación nuevas contienen únicamente mensajes preparados para el usuario.

Esta primera versión no confirma el correo: EmailConfirmed permanece falso y no se exige para entrar. No implementa recuperación de contraseña, MFA, refresh tokens, revocación inmediata, logout del servidor ni límites por IP. Una cuenta marcada para MFA no puede entrar por este flujo de solo contraseña. Esas funciones requerirán sus propios pasos.

El bloqueo evita nuevos logins; no invalida automáticamente JWT ya emitidos. Un token anterior mantiene su vigencia hasta expirar (más la tolerancia de reloj configurada), salvo que falle otra comprobación como la existencia del usuario del dominio al consultar su perfil. Al vencer el token, por ahora hay que iniciar sesión otra vez.

## 11. Verificación

Verificado el 28 de octubre de 2026: compilación sin errores y 110 pruebas aprobadas, incluidas 16 nuevas de autenticación. EF Core confirma que el modelo coincide con las migraciones; también se generó el SQL incremental de MySQL. La compilación mostró advertencias NU1900 porque no pudo consultar el servicio de auditoría de NuGet.

Las pruebas HTTP utilizan Identity, hash, SignInManager, transacciones, generación JWT, middleware y controladores reales con SQLite temporal. Cubren registro completo, token aceptado por el perfil, login sin cookies, normalización y unicidad del correo, datos inválidos, rechazo de roles enviados por el cliente, fallo posterior al guardado de Identity, errores de credenciales y bloqueo temporal.

SQLite no sustituye la comprobación final contra MySQL. El script de migración debe aplicarse y comprobarse cuando exista una conexión real.

Fuentes: [modelo de Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-9.0), [configuración de contraseñas y bloqueo](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-9.0).
