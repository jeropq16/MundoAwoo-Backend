# Registro y login actuales: BCrypt y JWT

## Estructura

La API no usa ASP.NET Identity. `Usuario` conserva correo normalizado, hash de contraseña, intentos fallidos y fecha de bloqueo como datos. Los servicios de Application contienen las reglas. El hash nunca sale en un DTO.

| Capa | Archivo | Responsabilidad |
| --- | --- | --- |
| Domain | `MundoAwoo.Domain/Entities/Usuario.cs` | Datos y relaciones del usuario. |
| Domain | `MundoAwoo.Domain/Interfaces/IUsuarioRepository.cs` | Buscar por Id o correo y agregar. |
| Application | `MundoAwoo.Application/Services/AutenticacionService.cs` | Registro, login, validación y coordinación del guardado. |
| Application | `MundoAwoo.Application/Services/UsuarioService.cs` | Crea usuario y perfil; valida cambios de perfil y desbloqueos. |
| Application | `MundoAwoo.Application/Services/AvatarService.cs` | Crea avatar; valida color y equipamiento. |
| Application | `MundoAwoo.Application/Security/IPasswordHasher.cs` | Contrato para crear y verificar hashes. |
| Infrastructure | `MundoAwoo.Infrastructure/Security/BcryptPasswordHasher.cs` | Implementación con BCrypt. |
| Infrastructure | `MundoAwoo.Infrastructure/Persistence/Repositories/UsuarioRepository.cs` | Consultas EF Core. |
| Infrastructure | `MundoAwoo.Infrastructure/Security/JwtTokenService.cs` | Emite JWT firmados. |
| Api | `MundoAwoo.Api/Controllers/AutenticacionController.cs` | Rutas HTTP. |

## Registro

`POST /api/autenticacion/registro` recibe `correo`, `contrasena` y `nombreVisible`. Application normaliza el correo a minúsculas, valida su formato y la contraseña, consulta si ya existe y genera el hash con `IPasswordHasher`. `UsuarioService` crea `Usuario` y `PerfilUsuario`; `AvatarService` crea el `Avatar`. `IUnitOfWork` confirma todo en un solo `SaveChangesAsync`. Si el guardado falla, EF no deja un registro parcial. La respuesta `201` contiene un JWT cuyo `sub` es `Usuario.Id`.

La contraseña nueva debe tener de 12 a 128 caracteres, mayúscula, minúscula, número y símbolo. `Usuarios.Correo` tiene un índice único. Las filas históricas creadas sin cuenta pueden tener correo y hash nulos; el registro normal siempre llena ambos campos.

## Login

`POST /api/autenticacion/login` busca por correo normalizado y verifica el hash con BCrypt. Cinco fallos consecutivos bloquean la cuenta durante cinco minutos; un acceso correcto restablece el contador. Las credenciales desconocidas e incorrectas devuelven el mismo `401`. No se crea cookie. El token se envía después en `Authorization: Bearer <token>` para consultar o actualizar `/api/perfil`.

`Jwt:Issuer`, `Jwt:Audience` y `Jwt:DuracionMinutos` están en la configuración; `Jwt:SigningKey` debe permanecer fuera del repositorio. El token expira y no se guarda en MySQL. El bloqueo de login no revoca tokens ya emitidos.

## Migraciones

Las migraciones se conservan como historial de la base: `InicialMySql`, `AgregarIdentity` y `AutenticacionSinIdentity`. La tercera elimina las tablas `AspNet*` y añade a `Usuarios` correo, hash, contador y bloqueo. La base local estaba vacía al aplicarla. Para una base externa que sí tenga cuentas Identity, hace falta planear la conversión de contraseñas antes de aplicar esa migración; los hashes antiguos no son hashes BCrypt.

```bash
dotnet ef database update --project MundoAwoo.Infrastructure --startup-project MundoAwoo.Api
```

Los archivos `docs/AgregarIdentity.sql` y las migraciones anteriores son historia del esquema, no la implementación de autenticación vigente.
