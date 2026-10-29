# Interfaces del dominio: contratos de repositorios

> **Nota histórica:** este documento describe una etapa anterior. La estructura y autenticación vigentes están en [README](../README.md) y [registro/login actual](08-registro-login.md).

Este es un paso intermedio solicitado después de preparar MySQL. No implementa todavía los endpoints del paso 4 del plan original.

## 1. Qué es una interfaz

Una interfaz declara qué métodos debe ofrecer una clase que la implemente. No contiene aquí consultas SQL ni acceso a MySQL. Es un contrato entre quien solicita datos y quien sabe obtenerlos.

Por ejemplo, IUsuarioRepository promete buscar un usuario por su identificador. Más adelante UsuarioRepository, en Infrastructure, usará AwoorldDbContext para cumplir esa promesa. Application podrá trabajar con la interfaz sin conocer los detalles de EF Core.

```text
Application solicita un usuario mediante IUsuarioRepository (Domain)
    ↓
UsuarioRepository (Infrastructure) implementará la consulta
    ↓
AwoorldDbContext → EF Core + Pomelo → MySQL
```

Crear estas interfaces no modifica tablas y no requiere una nueva migración.

## 2. Los tres contratos creados

### IUsuarioRepository

- `ObtenerPorIdAsync(usuarioId, cancellationToken)`: devuelve el Usuario con Perfil e ItemsDesbloqueados cargados, o null si no existe.
- `Agregar(usuario)`: registra el usuario, su perfil y su inventario como nuevos objetos pendientes de insertar. No confirma cambios en MySQL.

Es importante cargar el perfil y el inventario: ActualizarPerfil necesita el primero y TieneItemDesbloqueado necesita el segundo. Una lista no cargada no debe confundirse con un inventario vacío.

### IAvatarRepository

- `ObtenerPorUsuarioIdAsync(usuarioId, cancellationToken)`: devuelve el avatar del usuario con todos sus ItemsEquipados, o null si no existe.
- `Agregar(avatar)`: registra el nuevo avatar y sus equipamientos como pendientes de insertar, sin confirmar cambios.

Buscamos por UsuarioId porque nuestro flujo principal es obtener el avatar propio del usuario autenticado. Application tendrá que obtener esa identidad de una fuente confiable y autorizar la operación.

### IItemDeAvatarRepository

- `ObtenerPorIdAsync(itemId, cancellationToken)`: busca una definición del catálogo o devuelve null.
- `ObtenerPorCategoriaAsync(categoriaId, cancellationToken)`: devuelve los ítems de una categoría. Una categoría sin ítems o inexistente devuelve una lista vacía.
- `ObtenerCategoriasAsync(cancellationToken)`: devuelve las categorías disponibles para construir el menú.

CategoriaId es el Guid de una categoría concreta. TipoCategoria es el enum Ropa/Accesorio/Baile/Gesto: no son intercambiables. Puede haber varias categorías del mismo tipo.

Como acordamos inicialmente, las categorías se consultan desde el catálogo. Por eso mantenemos los tres repositorios previstos y no creamos por ahora un repositorio administrativo de categorías. Tampoco agregamos operaciones para editar el catálogo mientras no exista un caso de uso que las necesite.

## 3. Cómo leer las firmas

```csharp
Task<Usuario?> ObtenerPorIdAsync(
    Guid usuarioId,
    CancellationToken cancellationToken = default);
```

- `Task<Usuario?>`: la operación es asíncrona y entregará un usuario o null. Cuando exista la implementación, usaremos await para esperar el resultado sin bloquear el hilo durante la consulta.
- `?`: obliga a considerar que el usuario puede no existir.
- `Guid usuarioId`: identificador que buscamos.
- `CancellationToken`: permite cancelar la operación, por ejemplo si se cancela la solicitud HTTP. Es opcional gracias a `= default`.
- `Async`: convención de nombre que avisa que el método se espera con await. En la interfaz no se necesita escribir la palabra async.
- `;`: termina la declaración. El cuerpo del método estará en la clase que implemente la interfaz.

`Task<IReadOnlyList<ItemDeAvatar>>` devuelve una lista consultable: podemos recorrerla y leer sus elementos, pero no agregar ni quitar elementos desde esa interfaz. Esto no convierte automáticamente cada objeto contenido en inmutable. Las búsquedas de listas devuelven una lista vacía, nunca null.

`void Agregar(...)` es síncrono porque solamente deja el objeto pendiente dentro de la unidad de trabajo; no hace una escritura inmediata. Nuestros identificadores ya se generan en el dominio con Guid.NewGuid().

## 4. Consultar, modificar y confirmar

Mantendremos una unidad de trabajo compartida por los repositorios utilizados en cada operación. Posteriormente definiremos IUnitOfWork en Application y su implementación en Infrastructure, como indica el plan de arquitectura.

En el futuro, el flujo será:

1. Cargar Usuario e inventario mediante IUsuarioRepository.
2. Cargar Avatar y equipamientos mediante IAvatarRepository.
3. Cargar ItemDeAvatar mediante el repositorio de catálogo.
4. Ejecutar `avatar.Equipar(item, usuario)`, que protege las reglas del negocio.
5. Confirmar los cambios una sola vez mediante la unidad de trabajo.

No agregamos un método Actualizar a los repositorios: sus consultas de agregados tienen el contrato de devolver objetos cuyos cambios se pueden confirmar dentro de la misma operación. La futura implementación con EF Core deberá usar consultas con seguimiento para Usuario y Avatar y cargar sus relaciones con Include; todos compartirán el mismo contexto. No debe usar AsNoTracking para esas consultas de modificación.

Agregar tampoco debe llamar internamente a SaveChangesAsync. Así podremos registrar usuario y avatar y confirmar ambas inserciones juntas, en lugar de guardar un objeto y fallar al guardar el siguiente. La coordinación con Identity se resolverá cuando implementemos autenticación.

## 5. Ejemplo C# de consumo del contrato

Este método sirve como ejemplo para estudiar la interfaz; no se añadió todavía como servicio de Application:

```csharp
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Interfaces;

public static async Task<Usuario> BuscarUsuarioAsync(
    IUsuarioRepository usuarios,
    Guid usuarioId,
    CancellationToken cancellationToken = default)
{
    // Quien llama no necesita saber cómo el repositorio consulta MySQL.
    Usuario? usuario = await usuarios.ObtenerPorIdAsync(usuarioId, cancellationToken);

    if (usuario == null)
        throw new InvalidOperationException("El usuario no existe.");

    return usuario;
}
```

No se puede crear una interfaz con `new IUsuarioRepository()`. Necesitaremos una clase UsuarioRepository que la implemente y registrarla en la inyección de dependencias de la API.

## 6. Por qué no hay un repositorio por tabla

PerfilUsuario e ItemDesbloqueado se modifican mediante Usuario. ItemEquipado se modifica mediante Avatar. Crear repositorios independientes para esas entidades permitiría saltarse las reglas de las raíces de agregado.

Los repositorios devuelven entidades del dominio, no DTOs, DbSet ni IQueryable. Application definirá los DTOs para el cliente. Infrastructure elegirá cómo ejecutar las consultas y guardar los cambios.

Este paso creó los contratos y su documentación. El usuario confirmó después la implementación: consultar [repositorios de Infrastructure y unidad de trabajo](05-repositorios-infrastructure.md) para ver las clases, el registro de dependencias y las pruebas añadidas.
