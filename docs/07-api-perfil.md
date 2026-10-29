# API de perfil: acceso propio, JWT y errores

> **Nota histórica:** este documento describe una etapa anterior. La estructura y autenticación vigentes están en [README](../README.md) y [registro/login actual](08-registro-login.md).

Esta guía describe el paso de protección del perfil. Registro y login se implementaron después: consulta [la guía de autenticación](08-registro-login.md) para emitir tokens con credenciales y aplicar la nueva migración.

## 1. Alcance de este paso

Ya están conectados los casos de uso de Application con dos endpoints HTTP protegidos:

| Método | Ruta | Resultado correcto |
| --- | --- | --- |
| GET | /api/perfil | 200 con el perfil del usuario autenticado |
| PUT | /api/perfil | 200 con el perfil actualizado y guardado |

El controlador usa IPerfilService. No consulta directamente el contexto ni decide las reglas de longitud del perfil. No hay una ruta con UsuarioId para editar perfiles ajenos.

En este paso se configuró la validación JWT. Las pruebas de perfil conservan tokens firmados dentro de Tests con una clave separada de la configuración real; el flujo manual completo ya puede usar los endpoints de registro/login descritos en la guía siguiente, con MySQL configurado.

## 2. Archivos y responsabilidades

- API/Controllers/PerfilController.cs: recibe HTTP, obtiene la identidad autorizada y llama a Application.
- API/Security/AutenticacionJwt.cs: registra validación JWT, política de acceso y respuestas 401/403.
- Infrastructure/Options/JwtOptions.cs: propiedades de configuración Issuer, Audience y SigningKey.
- API/Middlewares/ExceptionMiddleware.cs: convierte excepciones en respuestas de error.
- API/Program.cs: conecta los componentes y define su orden.
- Tests/Api: inicia la API en memoria y prueba solicitudes HTTP con JWT reales de prueba.

## 3. Cómo funciona el controlador

```csharp
[ApiController]
[Route("api/perfil")]
[Authorize(Policy = AutenticacionJwt.PoliticaPerfilPropio)]
```

- ApiController activa comportamientos de API, como rechazar automáticamente JSON inválido o un cuerpo obligatorio ausente.
- Route define la dirección base.
- Authorize exige superar la política antes de ejecutar las acciones del controlador.
- HttpGet y HttpPut identifican qué acción atiende cada método HTTP.
- FromBody indica que el DTO de actualización se lee del cuerpo JSON.
- CancellationToken recibe la cancelación de la solicitud y se transmite al servicio.
- Ok(perfil) devuelve HTTP 200 y serializa el DTO a JSON.
- ResponseCache con NoStore evita que el resultado del perfil se almacene en cachés HTTP.

El flujo de actualización es:

```text
Validar JWT → comprobar acceso → extraer UsuarioId del token
    → PerfilService.ActualizarAsync
    → Usuario.ActualizarPerfil
    → UnitOfWork.GuardarCambiosAsync
    → respuesta 200 con DTO
```

## 4. Identidad y autorización del propietario

Un JWT lleva datos llamados claims. Usamos `sub` (subject) como identificador del usuario del dominio. El login escribe allí **Usuario.Id**, aunque Identity tenga otro identificador interno.

Primero el middleware JWT comprueba la firma y los datos de validación. Solo después el controlador lee `User`, que representa la identidad validada. No decodificamos manualmente el token para confiar en sus datos sin validar.

La política PerfilPropio exige identidad autenticada y claim sub. El método IntentarObtenerUsuarioId exige exactamente un sub, con formato Guid y diferente de Guid.Empty. Si no cumple, devuelve Forbid (403). Algunos formatos inválidos se rechazan antes: un sub serializado como array por tener varios valores no supera el validador JWT y devuelve 401.

Los métodos no reciben UsuarioId por ruta o query. Una query `?usuarioId=OTRO_ID` no cambia el propietario: se usa siempre el sub. Si se intenta enviar UsuarioId en el cuerpo del PUT, la configuración JSON rechaza el campo desconocido con 400. Esta regla de campos desconocidos se aplica a los cuerpos JSON de los controladores de la API.

Esta autorización limita la operación al perfil propio. No implementa todavía roles administrativos, revocación de tokens ni comprobación del estado de una cuenta Identity en cada petición. El login comprueba el bloqueo antes de emitir un token, pero bloquear una cuenta no revoca sus tokens anteriores. Si el usuario del token no existe en el dominio, el servicio devuelve el error que se traduce a 404.

## 5. Qué valida el JWT

AutenticacionJwt configura:

- Issuer: el emisor esperado.
- Audience: la API a la que va dirigido el token.
- Firma: debe verificarse con la clave configurada.
- Algoritmo permitido: HS256. Se rechazan tokens sin firma y otros algoritmos.
- Expiración: obligatoria; también se respeta not-before cuando está presente.
- ClockSkew: tolerancia de 30 segundos para diferencias pequeñas entre relojes.

MapInboundClaims = false conserva el nombre sub. IncludeErrorDetails = false evita devolver detalles técnicos de la validación al cliente.

La política se aplica al controlador de perfil. No se configuró una política global que proteja automáticamente cualquier futuro endpoint: al añadir otros controladores habrá que declarar sus permisos. OpenAPI sigue disponible en Development como documentación, sin devolver datos de perfiles.

Referencia: [validación JWT en ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-9.0).

## 6. Configurar JWT localmente

appsettings.json contiene valores no secretos:

```json
"Jwt": {
  "Issuer": "Awoorld",
  "Audience": "Awoorld.Api"
}
```

SigningKey debe ir en User Secrets o en la variable `Jwt__SigningKey`. La API rechaza una clave ausente o de menos de 32 bytes UTF-8; no tiene una clave de reserva. Usa una clave aleatoria, no una frase fácil de adivinar.

Desde PowerShell 7 en la raíz del proyecto puedes generar una clave local y guardarla sin imprimirla:

```powershell
$claveAwoorld = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet user-secrets set 'Jwt:SigningKey' $claveAwoorld --project MundoAwoo.Api
Remove-Variable claveAwoorld
```

La cadena base64 resultante se usa como texto UTF-8 para firmar y validar. JwtTokenService usa esa misma interpretación. No compartas la clave con React ni la subas al repositorio. Regenerarla invalida los tokens firmados con la anterior.

También necesitas ConnectionStrings:Awoorld, descrita en la guía MySQL. User Secrets se carga en Development. Para otros entornos, proporciona la configuración mediante el mecanismo de secretos del despliegue.

Este turno no creó ni escribió una clave real en tus User Secrets. La configuración de pruebas está aislada y no debe usarse en desarrollo ni producción.

## 7. Orden de los componentes

```csharp
app.UseMiddleware<ExceptionMiddleware>();
// ... OpenAPI en Development ...
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

El middleware de errores envuelve los componentes siguientes. Authentication valida el JWT y construye User. Authorization decide si se puede ejecutar el endpoint. Después se invoca el controlador.

## 8. Respuestas de error

| Estado | Situación |
| --- | --- |
| 400 Bad Request | JSON incorrecto, cuerpo ausente/nulo, campos desconocidos o datos de perfil inválidos |
| 401 Unauthorized | Falta token o no pasa la validación de formato, firma, emisor, destinatario o vigencia |
| 403 Forbidden | Token validado pero sin un sub único y utilizable como Usuario.Id |
| 404 Not Found | El usuario identificado por el token no existe |
| 500 Internal Server Error | Fallo inesperado de consulta, guardado u otra operación |

Los errores usan `application/problem+json`, con propiedades como title, status y detail. Los errores de formato del cuerpo pueden incluir `errors` por campo. Los de ExceptionMiddleware incluyen traceId para relacionar la respuesta con los registros del servidor. No todos los errores tienen exactamente las mismas propiedades opcionales.

Ejemplo de validación del dominio:

```json
{
  "title": "Datos inválidos",
  "status": 400,
  "detail": "Revisa los datos enviados y sus límites de longitud.",
  "traceId": "identificador-de-la-solicitud"
}
```

El middleware traduce UsuarioNoEncontradoException a 404 y las ArgumentException actuales a 400. No devuelve exception.Message ni trazas al cliente. Los fallos inesperados se registran con ILogger y se responden con 500 genérico. Si el cliente canceló la conexión, se aborta sin intentar escribir un error; si la respuesta ya empezó, no se intenta reemplazarla por otro JSON.

Esta traducción de ArgumentException sirve para las validaciones actuales. Al agregar más casos de uso, podremos introducir excepciones de validación más específicas. Una excepción de base de datos no se transforma en una respuesta de éxito ni en un perfil vacío.

Los 401 conservan la cabecera WWW-Authenticate: Bearer. Las respuestas 403 se producen tanto desde la política como desde Forbid del controlador. Referencia: [manejo de errores en ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-9.0).

## 9. Probar los endpoints

Después de configurar la conexión y la clave:

```powershell
dotnet build MundoAwoo.sln --no-restore -m:1
dotnet run --project MundoAwoo.Api --launch-profile https --no-build
```

El perfil HTTPS existente usa https://localhost:7018. Si el certificado local no está preparado, usa `dotnet dev-certs https --trust` y sigue la confirmación del sistema.

Puedes comprobar el rechazo sin token llamando a GET https://localhost:7018/api/perfil: devolverá 401, sin consultar perfiles. Para la llamada autenticada en Postman, selecciona Authorization > Bearer Token y usa un token emitido por registro o login, con un sub correspondiente a un usuario guardado.

La actualización será:

```http
PUT /api/perfil
Authorization: Bearer <TOKEN_DE_ACCESO>
Content-Type: application/json

{
  "nombreVisible": "Awoo Fan",
  "biografia": "Me encanta el nuevo álbum."
}
```

Ambos campos se reemplazan; una biografía vacía borra la anterior. El servicio conserva las validaciones y normalización que ya implementamos.

Las pruebas de perfil usan tokens preparados con una clave de pruebas. Las pruebas de autenticación añadidas en el paso siguiente también comprueban el recorrido completo: registro o login, emisión de JWT y acceso al perfil.

## 10. Migraciones y pruebas

No cambiaron las tablas ni las entidades: no hace falta una nueva migración. Como las herramientas EF arrancan el proyecto API, también necesitan la configuración JWT. En Development pueden leer la clave de User Secrets. Para comandos de generación sin servidor, configura una clave temporal aleatoria en esa terminal junto con la conexión ficticia de la guía MySQL:

```powershell
$env:Jwt__SigningKey = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
# Aquí puedes ejecutar migrations add/script/has-pending-model-changes con la conexión adecuada.
Remove-Item Env:Jwt__SigningKey
```

Las pruebas usan WebApplicationFactory para ejecutar el pipeline real, validan JWT firmados y sustituyen solo el almacenamiento por SQLite temporal (salvo la prueba que inyecta un fallo del servicio para comprobar el 500). No son una prueba de MySQL real ni del flujo de login.

Prueban acceso sin token, tokens inválidos, sujetos ausentes/ambiguos, lectura y actualización propias, intentos de cambiar el propietario por query/body, datos inválidos, usuario inexistente y ocultamiento de errores internos.

Verificación del 28 de octubre de 2026: solución compilada sin errores y 94 pruebas aprobadas, incluidas 28 nuevas de API. Persisten las advertencias NU1900 de API e Infrastructure al consultar vulnerabilidades de NuGet.

El usuario confirmó registro y login con ASP.NET Identity; su implementación y validación están en [la guía siguiente](08-registro-login.md).
