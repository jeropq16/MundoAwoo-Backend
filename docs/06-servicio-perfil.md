# Application: consultar y actualizar el perfil

> **Nota histórica:** este documento describe una etapa anterior. La estructura y autenticación vigentes están en [README](../README.md) y [registro/login actual](08-registro-login.md).

## 1. Qué responsabilidad tiene este servicio

PerfilService coordina dos casos de uso: consultar el perfil de un usuario y actualizar su nombre visible y biografía. Application usa los contratos del repositorio y de la unidad de trabajo; no conoce DbContext, SQL ni MySQL.

```text
Futuro controlador autorizado
    ↓ IPerfilService
PerfilService (Application)
    ├── IUsuarioRepository → carga Usuario y Perfil
    ├── Usuario.ActualizarPerfil → valida y modifica en Domain
    └── IUnitOfWork → confirma cambios en Infrastructure
```

Solo la actualización necesita los tres pasos. La consulta carga al usuario y devuelve una copia de los datos del perfil.

## 2. DTOs de entrada y salida

Un DTO transporta datos entre capas sin exponer las entidades completas.

`DTOs/Perfiles/PerfilUsuarioDto.cs` es la salida:

- Id: identificador del perfil, diferente del identificador del usuario.
- UsuarioId: identificador del propietario.
- NombreVisible: nombre que verá el fandom.
- Biografia: texto de presentación.

`DTOs/Perfiles/ActualizarPerfilDto.cs` es la entrada:

- NombreVisible.
- Biografia, que puede ser una cadena vacía para quitar el texto anterior.

No contiene Id, UsuarioId, inventario ni credenciales. Así la operación recibe únicamente campos editables. Sus propiedades son simples `get; set;` y las cadenas empiezan vacías. Los DTOs transportan datos; el dominio sigue validando que esos datos sean aceptables.

Actualizar reemplaza ambos campos: no es PATCH. Omitir Biografia deja el valor predeterminado vacío y borra la biografía anterior; omitir NombreVisible produce un nombre vacío que el dominio rechaza. Un null explícito también se rechaza según las reglas actuales.

## 3. El contrato IPerfilService

```csharp
Task<PerfilUsuarioDto> ObtenerAsync(
    Guid usuarioId, CancellationToken cancellationToken = default);

Task<PerfilUsuarioDto> ActualizarAsync(
    Guid usuarioId, ActualizarPerfilDto datos,
    CancellationToken cancellationToken = default);
```

Ambos devuelven un DTO. Si no existe el usuario, lanzan UsuarioNoEncontradoException en lugar de devolver un DTO vacío. La cancelación se transmite a las operaciones de consulta y guardado.

El servicio recibe UsuarioId para seleccionar el perfil, pero no autentica al usuario. Cuando implementemos la API, ese identificador deberá proceder de la identidad autenticada y autorizada, no de un campo del cuerpo enviado por el navegador. Los endpoints y JWT no se han implementado aquí.

## 4. El constructor de PerfilService

```csharp
public PerfilService(IUsuarioRepository usuarios, IUnitOfWork unidadDeTrabajo)
{
    _usuarios = usuarios;
    _unidadDeTrabajo = unidadDeTrabajo;
}
```

El servicio pide dos contratos. La inyección de dependencias entrega UsuarioRepository y UnitOfWork en ejecución. En las pruebas podemos entregar implementaciones sencillas de esos contratos para observar las consultas y los guardados.

## 5. Consultar paso a paso

ObtenerAsync:

1. Llama al método privado BuscarUsuarioAsync.
2. Este rechaza Guid.Empty, comprueba cancelación y consulta el repositorio.
3. Si el repositorio devuelve null, lanza UsuarioNoEncontradoException.
4. Si existe, CrearDto copia los datos de su perfil.

No se llama a GuardarCambiosAsync: leer no necesita escribir. Modificar después las propiedades del DTO devuelto tampoco modifica el perfil, porque son objetos diferentes.

BuscarUsuarioAsync es privado porque es una ayuda interna que reutilizan los dos casos de uso. CrearDto también es privado y estático: solo necesita el perfil recibido, sin acceder a los campos del servicio. El mapeo se hace propiedad por propiedad, sin añadir una librería.

## 6. Actualizar paso a paso

La parte central del método es:

```csharp
Usuario usuario = await BuscarUsuarioAsync(usuarioId, cancellationToken);
usuario.ActualizarPerfil(datos.NombreVisible, datos.Biografia);
await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
return CrearDto(usuario.Perfil);
```

Antes de este bloque, ArgumentNullException.ThrowIfNull(datos) comprueba que exista el DTO.

El dominio valida nombre obligatorio de hasta 50 caracteres y biografía de hasta 500, después de quitar espacios exteriores. No repetimos esas reglas en el servicio. Si falla una validación, el perfil conserva sus valores anteriores y el servicio no llama a guardar.

UsuarioRepository ya carga al usuario con seguimiento. Por eso llamar al método del dominio es suficiente para que EF detecte la modificación al guardar: no hace falta agregar un método Actualizar al repositorio.

El DTO se crea después de esperar el guardado. Si este falla, el error se propaga y el servicio no devuelve un resultado de éxito. El objeto en memoria puede haber cambiado antes del fallo: no debe reutilizarse ese contexto como si el guardado hubiese terminado bien.

## 7. Qué errores distinguimos

| Situación | Resultado |
| --- | --- |
| Identificador vacío | ArgumentException, antes de consultar |
| DTO nulo | ArgumentNullException, antes de consultar |
| Usuario inexistente | UsuarioNoEncontradoException |
| Nombre o biografía inválidos | Excepción de validación del dominio, sin guardar |
| Operación cancelada | OperationCanceledException |
| Error del repositorio o del guardado | Se propaga el error original |

UsuarioNoEncontradoException es una excepción simple de Application. No contiene dependencias HTTP; más adelante el middleware/controlador la traducirá a una respuesta 404. Esta entrega no implementa todavía esa traducción.

## 8. Registro en la API

Program.cs incluye:

```csharp
builder.Services.AddScoped<IPerfilService, PerfilService>();
```

Cuando un controlador solicite IPerfilService, recibirá PerfilService. El servicio utilizará los repositorios y la unidad de trabajo del mismo alcance de la solicitud, que comparten AwoorldDbContext.

## 9. Ejemplo de uso

Este fragmento muestra el consumo desde una clase que ya recibe `IPerfilService servicio` y un `Guid usuarioId` autorizado:

```csharp
// Consulta el perfil actual.
PerfilUsuarioDto actual = await servicio.ObtenerAsync(usuarioId);

// Prepara únicamente los campos editables.
var cambios = new ActualizarPerfilDto
{
    NombreVisible = "  Awoo Fan  ",
    Biografia = "  Me encanta el nuevo álbum.  "
};

// Valida con Domain, guarda y devuelve los textos sin espacios exteriores.
PerfilUsuarioDto actualizado = await servicio.ActualizarAsync(usuarioId, cambios);
```

Los tipos DTO pertenecen a `MundoAwoo.Application.DTOs.Perfiles`. La conexión HTTP se implementó en el paso posterior: consultar [API de perfil](07-api-perfil.md).

## 10. Verificación y siguiente paso

PerfilServiceTests comprueba consulta sin escritura, independencia del DTO, actualización y normalización, guardado único, usuario inexistente, identificadores vacíos, datos inválidos, cancelación y propagación de fallos de persistencia. Una prueba adicional utiliza SQLite temporal con el repositorio y UnitOfWork reales: guarda, limpia el seguimiento, vuelve a consultar y verifica que otro usuario conserve su perfil.

SQLite solo se usa en pruebas. No se añadieron paquetes ni se modificaron tablas; no hace falta una migración nueva. La lectura/escritura contra MySQL real sigue pendiente de disponer de un servidor configurado.

Verificación del 27 de octubre de 2026: solución compilada sin errores y 66 pruebas aprobadas, incluidas 17 nuevas del servicio de perfil. Persisten dos advertencias NU1900 en API e Infrastructure por la consulta de vulnerabilidades de NuGet.

El usuario confirmó después la conexión con la API, la autorización del propietario y la traducción de errores. Los detalles están en [la guía de API de perfil](07-api-perfil.md).
