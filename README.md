# MundoAwoo Backend

API de Awoorld en ASP.NET Core 9 con MySQL, EF Core, JWT y BCrypt. El código está dividido en Domain, Application, Infrastructure, Api y Tests.

## Capas

- **Domain:** entidades con propiedades y relaciones, enumeraciones, objeto de valor de color y contratos de repositorio.
- **Application:** creación y reglas de usuario, perfil, avatar y catálogo; casos de uso, DTOs, contrato de hash y excepciones.
- **Infrastructure:** EF Core, configuraciones, migraciones, repositorios, BCrypt y emisión de JWT.
- **Api:** controladores, autenticación Bearer, middleware de errores y registro de dependencias.
- **Tests:** pruebas de dominio, servicios, persistencia y HTTP.

Flujo: `Controller → Service → Repository → DbContext → MySQL`. Las entidades guardan datos; los servicios de Application validan y cambian su estado. La API no usa ASP.NET Identity. El correo y hash de contraseña viven en `Usuario`; el hasher concreto está en Infrastructure.

## Funcionalidad actual

| Método | Ruta | Función |
| --- | --- | --- |
| POST | `/api/autenticacion/registro` | Crea usuario, perfil y avatar; devuelve JWT. |
| POST | `/api/autenticacion/login` | Verifica contraseña y bloqueo; devuelve JWT. |
| GET | `/api/perfil` | Consulta el perfil del usuario autenticado. |
| PUT | `/api/perfil` | Actualiza su nombre visible y biografía. |
| GET | `/api/avatar` | Devuelve el avatar propio con color y elementos equipados. Requiere JWT. |
| PUT | `/api/avatar/color` | Cambia el color de fondo del avatar propio. Requiere JWT. |
| GET | `/api/catalogo/categorias` | Devuelve las categorías disponibles. Es público. |
| GET | `/api/catalogo/items?categoriaId=...` | Lista los ítems de una categoría. Es público. |
| GET | `/api/inventario` | Devuelve los ítems desbloqueados del usuario autenticado. |
| POST | `/api/avatar/items/{itemId}/equipar` | Equipa un ítem desbloqueado si su ranura está libre. |
| DELETE | `/api/avatar/items/{itemId}/desequipar` | Desequipa un ítem; repetir la petición responde `204`. |
| POST | `/api/autenticacion/refresh` | Rota el refresh token y entrega otro JWT. |
| POST | `/api/autenticacion/cerrar-sesion` | Revoca el refresh token de esa sesión. |
| POST | `/api/admin/catalogo/categorias` | Crea una categoría; solo administradores. |
| POST | `/api/admin/catalogo/items` | Crea un ítem; solo administradores. |
| POST | `/api/inventario/items/{itemId}/desbloquear` | Concede un ítem a un usuario; solo administradores. |

Las reglas para desbloquear y equipar ítems están en `UsuarioService` y `AvatarService`. Solo una cuenta administradora puede conceder ítems. El catálogo no se inserta automáticamente y los ítems se pueden equipar únicamente después de desbloquearlos.

## Desarrollo local

Requisitos: SDK .NET 9 y MySQL. Configura `ConnectionStrings:Awoorld` y `Jwt:SigningKey` en User Secrets del proyecto Api o en variables de entorno. La clave JWT debe tener al menos 32 bytes; la base local se llama `awoorld`.

```bash
dotnet restore MundoAwoo.sln
dotnet build MundoAwoo.sln
dotnet ef database update --project MundoAwoo.Infrastructure --startup-project MundoAwoo.Api
dotnet run --project MundoAwoo.Api
dotnet test MundoAwoo.sln
```

En Development, Swagger UI está en `http://localhost:5064/swagger` con el perfil `http`, o en `https://localhost:7018/swagger` con el perfil `https`. Usa **Authorize** con el JWT devuelto por registro o login para probar `/api/perfil` y `/api/avatar`. El JSON también está en `/swagger/v1/swagger.json` y `/openapi/v1.json`.

`PUT /api/avatar/color` recibe `{ "colorFondo": "#A1B2C3" }`. El valor acepta espacios exteriores y letras minúsculas; se guarda en mayúsculas. Responde `200` con el avatar actualizado; un color inválido devuelve `400`. Las rutas del avatar devuelven `401` sin JWT y `404` si el usuario autenticado no tiene avatar. `GET /api/catalogo/categorias` responde una lista ordenada por nombre, incluso `[]` si el catálogo está vacío. Sus objetos contienen `id`, `nombre` y `tipo` (por ejemplo, `Ropa`). No se inserta catálogo automáticamente.

`GET /api/catalogo/items` exige `categoriaId` como GUID y responde `[]` si esa categoría no tiene ítems. `GET /api/inventario` responde una lista de `{ item, fechaDesbloqueo }` del usuario del JWT. Equipar responde `200` con el avatar; un ítem inexistente responde `404`, y uno bloqueado o una ranura ocupada responden `409`. Desequipar responde `204`.

Registro y login ahora devuelven `refreshToken` y `sesionExpiraEnUtc`, además del JWT de acceso. Cada sesión dura **una hora fija**; `POST /api/autenticacion/refresh` recibe `{ "refreshToken": "..." }`, devuelve un JWT nuevo y un refresh token nuevo, e invalida el anterior. No prolonga la hora de vencimiento. `POST /api/autenticacion/cerrar-sesion` recibe el mismo cuerpo y revoca esa sesión con respuesta `204`. El servidor guarda únicamente el hash del refresh token. Los JWT ya emitidos siguen válidos hasta su vencimiento normal (configurado en 15 minutos por defecto); al cerrar sesión se revoca la renovación. Guarda el refresh token de forma segura y reemplázalo por el nuevo en cada renovación.

## Administración del catálogo e inventario

Las rutas administrativas requieren un JWT de una cuenta con `Usuarios.EsAdministrador = true`. La API consulta esa marca en la base en cada petición, así que retirar el acceso tiene efecto inmediato. El registro público siempre crea cuentas sin acceso administrativo. Para habilitar una cuenta existente en otro entorno, hazlo directamente en la base con una operación administrada, por ejemplo `UPDATE Usuarios SET EsAdministrador = TRUE WHERE Correo = 'correo-del-admin';`.

Los cuerpos son:

```text
POST /api/admin/catalogo/categorias
{ "nombre": "Ropa", "tipo": "Ropa" }

POST /api/admin/catalogo/items
{ "nombre": "Camisa", "categoriaId": "GUID-DE-CATEGORIA", "ranura": "Torso", "recursoVisual": "camisa.svg" }

POST /api/inventario/items/GUID-DEL-ITEM/desbloquear
{ "usuarioId": "GUID-DEL-USUARIO" }
```

Los tipos admitidos son `Ropa`, `Accesorio`, `Baile` y `Gesto`. Las ranuras admitidas son `Torso`, `Piernas`, `Pies`, `Cabeza`, `Ojos`, `Cuello` y `Animacion`; el servicio comprueba su compatibilidad con el tipo de categoría. Crear devuelve `201`; conceder devuelve `200` con el ítem del inventario y se puede repetir sin duplicarlo. Un usuario normal recibe `403`, los datos inválidos `400`, una categoría/usuario/ítem inexistente `404` y un nombre duplicado `409`.

Las migraciones existentes se aplican en orden. `AgregarIdentity` es una migración histórica; `AutenticacionSinIdentity` retira esas tablas y deja el esquema actual. No edites migraciones ya aplicadas. Ver [autenticación actual](docs/08-registro-login.md) y [guía de MySQL](docs/03-mysql.md). Los demás documentos en `docs/` cuentan etapas anteriores y pueden contener ejemplos históricos.
