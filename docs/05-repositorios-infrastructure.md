# Implementaciones de repositorios en Infrastructure

## 1. Qué implementamos

Ahora las interfaces de Domain tienen clases que cumplen sus contratos mediante AwoorldDbContext:

| Interfaz | Implementación | Ubicación |
| --- | --- | --- |
| IUsuarioRepository | UsuarioRepository | Infrastructure/Persistence/Repositories |
| IAvatarRepository | AvatarRepository | Infrastructure/Persistence/Repositories |
| IItemDeAvatarRepository | ItemDeAvatarRepository | Infrastructure/Persistence/Repositories |
| IUnitOfWork (Application) | UnitOfWork | Infrastructure/Persistence |

El recorrido será: servicio de Application → interfaz → repositorio de Infrastructure → contexto → MySQL. Los servicios de Application y los endpoints todavía no se implementan en este paso.

## 2. El constructor recibe el contexto

```csharp
private readonly AwoorldDbContext _contexto;

public UsuarioRepository(AwoorldDbContext contexto)
{
    _contexto = contexto;
}
```

`private` limita el acceso al campo a la propia clase. `readonly` impide reemplazar la referencia después del constructor; el contexto sí puede registrar cambios. La inyección de dependencias entregará el contexto, por lo que el repositorio no necesita crearlo con new ni conocer la contraseña de MySQL.

## 3. Consultar un usuario completo

```csharp
return await _contexto.Usuarios
    .AsTracking()
    .Include(usuario => usuario.Perfil)
    .Include(usuario => usuario.ItemsDesbloqueados)
    .SingleOrDefaultAsync(usuario => usuario.Id == usuarioId, cancellationToken);
```

1. Usuarios selecciona la entidad que consultamos.
2. AsTracking pide a EF que recuerde los objetos cargados para detectar sus modificaciones posteriores.
3. Include carga las relaciones necesarias: perfil e inventario. Sin ellas podríamos tratar un inventario no cargado como si estuviera vacío.
4. La expresión `usuario => usuario.Id == usuarioId` indica el filtro que EF convierte a SQL.
5. SingleOrDefaultAsync ejecuta la consulta y devuelve una coincidencia o null. Si hubiera más de una, fallaría; el identificador es único.
6. await espera el resultado sin bloquear el hilo durante la operación de entrada/salida. cancellationToken se pasa a EF para permitir cancelarla.

AvatarRepository sigue la misma lógica, busca por UsuarioId y carga ItemsEquipados. De esta forma Equipar puede comprobar las ranuras ocupadas y Desequipar puede quitar una selección existente.

## 4. Agregar todavía no guarda

```csharp
public void Agregar(Usuario usuario)
{
    ArgumentNullException.ThrowIfNull(usuario);
    _contexto.Usuarios.Add(usuario);
}
```

ThrowIfNull rechaza un argumento nulo. Add registra como nuevos el usuario, su perfil y los desbloqueos que contiene. La inserción SQL se hace después al confirmar la unidad de trabajo. Es un método para agregados nuevos, no para volver a agregar un usuario existente.

AvatarRepository.Agregar registra el avatar y su equipamiento de la misma forma. Estos métodos no son async porque no realizan entrada/salida hacia la base; los Guid ya se generan en el dominio.

## 5. Consultar el catálogo

ItemDeAvatarRepository usa AsNoTracking: sus objetos son datos de consulta, por lo que no necesitamos que EF guarde cambios sobre ellos. Un ítem consultado se usa como argumento de Avatar.Equipar, pero ese método modifica el avatar, no el catálogo.

- ObtenerPorIdAsync busca un ítem por Id o devuelve null.
- ObtenerPorCategoriaAsync filtra por CategoriaId con Where, ordena por Nombre y después por Id, y ejecuta ToListAsync.
- ObtenerCategoriasAsync devuelve las categorías con el mismo criterio de orden.

ThenBy(Id) hace que el orden sea estable entre nombres iguales. La comparación de nombres sigue la colación configurada en la base. ToListAsync devuelve una lista vacía cuando no hay coincidencias. El catálogo inicial es pequeño; añadiremos paginación cuando exista una necesidad concreta.

## 6. Confirmar mediante UnitOfWork

IUnitOfWork vive en Application porque esta capa coordinará cuándo guardar. UnitOfWork vive en Infrastructure y utiliza el mismo AwoorldDbContext que los repositorios:

```csharp
public async Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
{
    return await _contexto.SaveChangesAsync(cancellationToken);
}
```

SaveChangesAsync detecta y escribe las inserciones, modificaciones y eliminaciones pendientes. Su resultado indica la cantidad de entradas de entidades escritas; no es el identificador de un usuario ni una respuesta HTTP.

Para modificar un objeto existente, lo cargamos con el repositorio, llamamos al método del dominio y guardamos. No necesitamos un método Actualizar ni llamar a DbSet.Update para esos objetos con seguimiento.

Por ejemplo, al quitar un ItemEquipado de la lista del avatar cargado, EF detecta que perdió su relación obligatoria con el avatar y elimina ese equipamiento al guardar. El desbloqueo del usuario permanece.

EF utiliza la transacción necesaria para confirmar esta llamada a SaveChangesAsync: si falla una escritura, no deben quedar las otras parcialmente aplicadas. Esto no crea una transacción que abarque llamadas anteriores, otros contextos, correos o servicios externos. Si un guardado falla, no se debe continuar usando ese contexto como si la operación hubiese tenido éxito. La coordinación con Identity se resolverá en el paso de autenticación.

## 7. Registrar la inyección de dependencias

Program.cs contiene:

```csharp
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IAvatarRepository, AvatarRepository>();
builder.Services.AddScoped<IItemDeAvatarRepository, ItemDeAvatarRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

AddScoped significa una instancia por alcance, normalmente una solicitud HTTP. AddDbContext también usa ese alcance. Los repositorios y UnitOfWork reciben el mismo contexto, por lo que un único guardado puede confirmar sus cambios juntos. Otra solicitud recibe otro contexto.

No usaremos repositorios singleton, contextos creados manualmente dentro de cada repositorio ni operaciones paralelas sobre el contexto compartido. EF no admite varias consultas simultáneas sobre la misma instancia; esperamos cada una antes de comenzar la siguiente.

## 8. Ejemplo del siguiente paso: un caso de uso

Este código es un ejemplo compilable de cómo se consumirán los contratos; todavía no se agregó como servicio ni endpoint:

```csharp
using MundoAwoo.Application.Interfaces;
using MundoAwoo.Domain.Interfaces;

public static class EjemploEquipamiento
{
    public static async Task EquiparAsync(
        Guid usuarioId,
        Guid itemId,
        IUsuarioRepository usuarios,
        IAvatarRepository avatares,
        IItemDeAvatarRepository catalogo,
        IUnitOfWork unidad,
        CancellationToken cancellationToken = default)
    {
        // usuarioId deberá venir de la identidad autenticada y autorizada.
        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, cancellationToken);
        if (usuario == null)
            throw new InvalidOperationException("El usuario no existe.");

        var avatar = await avatares.ObtenerPorUsuarioIdAsync(usuarioId, cancellationToken);
        if (avatar == null)
            throw new InvalidOperationException("El usuario no tiene avatar.");

        var item = await catalogo.ObtenerPorIdAsync(itemId, cancellationToken);
        if (item == null)
            throw new InvalidOperationException("El ítem no existe.");

        // Domain valida propiedad, desbloqueo y disponibilidad de la ranura.
        avatar.Equipar(item, usuario);
        await unidad.GuardarCambiosAsync(cancellationToken);
    }
}
```

Los errores de consulta, restricciones o cancelación se propagan. No se atrapan aquí para devolver una lista vacía o aparentar éxito: una consulta sin resultados es diferente de una conexión fallida. Definiremos su traducción a HTTP cuando implementemos los casos de uso y endpoints.

## 9. Pruebas y alcance

RepositoriosTests usa SQLite en memoria exclusivamente en el proyecto Tests. Abre una conexión temporal, crea el esquema de prueba, guarda datos, limpia el seguimiento y vuelve a cargarlos mediante los repositorios. Limpiar el seguimiento evita que la prueba parezca funcionar solo porque EF conserva los objetos originales en memoria.

Se verifica el guardado diferido, la carga de perfil/inventario/equipamiento, los cambios del dominio persistidos, el desequipamiento, los filtros y orden del catálogo, las consultas sin resultados, la cancelación y la ausencia de datos parciales ante un fallo de integridad.

SQLite es un sustituto para estas pruebas: puede diferir de MySQL en SQL, colaciones y otros comportamientos. Estas pruebas no ejecutan InicialMySql ni validan su aplicación a un servidor real. La API conserva Pomelo y MySQL. [Referencia de Microsoft sobre pruebas con SQLite en memoria](https://learn.microsoft.com/en-us/ef/core/testing/testing-without-the-database#sqlite-in-memory).

No se modificaron entidades, mapeos ni tablas, por lo que no hace falta una migración nueva. La migración inicial y la conexión a MySQL siguen siendo necesarias para utilizar la API contra una base real.

Validación del 27 de octubre de 2026: compilación de la solución sin errores y 49 pruebas aprobadas, incluidas las 6 nuevas de repositorios. Persisten advertencias NU1900 en API e Infrastructure por la consulta de vulnerabilidades de NuGet. No se ha realizado una prueba de lectura/escritura contra un servidor MySQL real.
