# Paso 2: entidades y reglas del dominio

> **Nota histórica:** este documento describe una etapa anterior. La estructura y autenticación vigentes están en [README](../README.md) y [registro/login actual](08-registro-login.md).

## Cómo leer el código

Empieza por Enums, Categoria e ItemDeAvatar; continúa con PerfilUsuario y Usuario; termina con Avatar y ColorHex. Las entidades viven en MundoAwoo.Domain, sin dependencias de EF Core, Identity, HTTP o React.

- `Guid` es un identificador. `Guid.NewGuid()` genera uno; `Guid.Empty` representa el valor vacío.
- `get; private set;` permite consultar una propiedad desde fuera, pero solo la propia clase puede asignarla.
- El constructor recibe lo necesario para crear un objeto válido. Si los datos son incorrectos, lanza una excepción antes de entregar el objeto.
- `internal` permite usar un miembro solo desde el proyecto Domain. Por eso Application no puede crear directamente ItemEquipado ni modificar directamente PerfilUsuario.
- `Any` consulta si algún elemento cumple una condición; `FirstOrDefault` devuelve la primera coincidencia o null.
- `IReadOnlyCollection` y `AsReadOnly()` exponen una vista de consulta de la lista privada. Para modificar el contenido hay que usar los métodos del agregado.

## 1. Categoria y los enums

Categoria tiene `Id`, `Nombre` (1 a 50 caracteres) y `Tipo`. TipoCategoria define Ropa, Accesorio, Baile y Gesto. El nombre es visible y el tipo determina las reglas.

RanuraAvatar indica el espacio que usa un ítem: Torso, Piernas, Pies, Cabeza, Ojos, Cuello o Animacion. `PermiteRanura` define la compatibilidad inicial: ropa en torso/piernas/pies; accesorios en cabeza/ojos/cuello; bailes y gestos en animación. Este modelo es una decisión inicial que podremos ampliar.

## 2. ItemDeAvatar: el catálogo

Sus propiedades son `Id`, `Nombre` (1 a 100 caracteres), `CategoriaId`, `Ranura` y `RecursoVisual` (1 a 500 caracteres). La referencia visual puede ser una clave o ruta de un recurso; no se descarga ni se reproduce desde el dominio. No se ha elegido todavía un motor gráfico.

El constructor valida la categoría y la ranura juntas. Así no puede existir una camiseta clasificada como baile. La ranura no tiene un método de modificación: cambiarla sin coordinar los avatares existentes podría dejar equipamientos incoherentes.

## 3. Usuario y PerfilUsuario

Usuario es raíz de agregado: es la entrada para modificar su perfil e inventario.

- `Id`: identidad del usuario dentro del negocio.
- `FechaRegistro`: instante en UTC, usando DateTimeOffset.
- `Perfil`: perfil creado automáticamente por el constructor de Usuario.
- `ItemsDesbloqueados`: inventario consultable, sin permitir cambios directos a la lista.
- `ActualizarPerfil(nombreVisible, biografia)`: delega las validaciones al perfil.
- `TieneItemDesbloqueado(itemId)`: consulta el inventario y devuelve true o false.
- `DesbloquearItem(item)`: agrega el ítem al inventario si todavía no está. Repetir la operación conserva un único registro.

PerfilUsuario tiene su `Id`, `UsuarioId`, `NombreVisible` (1 a 50 caracteres) y `Biografia` (0 a 500). Su método interno `Actualizar` valida todos los datos antes de asignarlos. Si la biografía falla, el nombre anterior también se conserva. El nombre visible no es un nombre de login ni se exige que sea único.

No hay correo ni contraseña en estas entidades. Los gestionará Identity en Infrastructure, vinculado al identificador del usuario de dominio.

## 4. ItemDesbloqueado e ItemEquipado

ItemDesbloqueado tiene `Id`, `UsuarioId`, `ItemDeAvatarId` y `FechaDesbloqueo` en UTC. Representa el permiso de uso, no lo que está puesto.

ItemEquipado tiene `Id`, `AvatarId`, `ItemDeAvatarId` y `Ranura`. Copia la ranura del ítem del catálogo; no permite que el cliente invente otra al equipar.

Sus constructores son internos: Usuario crea los desbloqueos y Avatar crea los equipamientos. Cada entidad tiene identidad, aunque no necesita ser una raíz de agregado ni tener su propio repositorio.

## 5. Avatar: configuración y equipamiento

Avatar es otra raíz de agregado. Tiene `Id`, `UsuarioId`, `ColorFondo` e `ItemsEquipados`. Recibe el UsuarioId en su constructor; empieza vacío y con fondo blanco.

`Equipar(item, propietario)`:

1. Comprueba que ambos argumentos existan.
2. Comprueba que el propietario corresponda al UsuarioId del avatar.
3. Comprueba que ese usuario tenga el ítem desbloqueado.
4. Si el mismo ítem ya está equipado, termina sin duplicarlo.
5. Si otro ítem ocupa la ranura, rechaza la operación sin modificar el avatar.
6. Crea un ItemEquipado y lo agrega a la lista privada.

Application cargará el propietario y el catálogo desde fuentes confiables. El parámetro propietario permite consultar otro agregado, pero el método solo modifica Avatar. Esta comprobación no sustituye la autorización HTTP: Application/API deberán verificar al usuario autenticado para todas las operaciones, incluido desequipar y cambiar el fondo.

`Desequipar(itemId)` elimina la selección, sin borrar el desbloqueo. Si ya no está equipado, no hace nada. Un Guid vacío se rechaza.

`CambiarColorFondo(color)` reemplaza el color de fondo de la vista del avatar. Lo incluimos como personalización mínima para dar un uso concreto al objeto de valor ColorHex; no representa tintes por prenda.

Un baile o gesto equipado significa una animación seleccionada. Este modelo no implementa reproducción, duración, transiciones ni estados transitorios de animación.

## 6. ColorHex: objeto de valor

ColorHex acepta exactamente el formato #RRGGBB, normaliza espacios exteriores y mayúsculas, y rechaza caracteres que no sean hexadecimales. Su propiedad `Valor` solo tiene getter.

Se declara como `sealed record`: C# proporciona igualdad por contenido. Dos instancias con #AABBCC son iguales aunque sean objetos distintos. No tiene Id, porque importa el color en sí. Para cambiarlo creamos otro ColorHex; el anterior permanece intacto.

## Ejemplo real de uso

```csharp
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Enums;
using MundoAwoo.Domain.ValueObjects;

// Crear el usuario también crea su perfil, pero no guarda nada en una base de datos.
var usuario = new Usuario("Awoo Fan");
var avatar = new Avatar(usuario.Id);

var categoria = new Categoria("Ropa del álbum", TipoCategoria.Ropa);
var camiseta = new ItemDeAvatar(
    "Camiseta de la gira",
    categoria,
    RanuraAvatar.Torso,
    "avatares/ropa/camiseta-gira.png");

usuario.ActualizarPerfil("Fan de Awoorld", "Mi álbum favorito es...");
usuario.DesbloquearItem(camiseta);
avatar.Equipar(camiseta, usuario);
avatar.CambiarColorFondo(new ColorHex("#AABBCC"));

// Quitarla del avatar no elimina el derecho a volver a usarla.
avatar.Desequipar(camiseta.Id);
```

Las rutas del ejemplo son ilustrativas: no se han creado archivos gráficos.

## Qué se verifica y qué sigue pendiente

Las pruebas de MundoAwoo.Tests/Domain cubren bloqueos, propietario incorrecto, conflictos de ranura, repetición de equipamiento/desbloqueo, convivencia de prendas y accesorios, desequipamiento, validación de catálogo, actualización atómica del perfil, colecciones protegidas e igualdad/validación de colores.

Validación del 27 de octubre de 2026: `dotnet test MundoAwoo.Tests/MundoAwoo.Tests.csproj -m:1 --verbosity minimal` completó 38 pruebas, sin fallos ni omisiones. NuGet emitió NU1900 al no poder consultar los datos de vulnerabilidad en api.nuget.org. Domain, Application, Infrastructure y Tests compilaron durante esta ejecución.

Usamos ArgumentException para datos inválidos e InvalidOperationException para operaciones incompatibles con el estado actual. Más adelante la API traducirá estas situaciones en respuestas HTTP apropiadas.

El dominio trabaja en memoria. En el paso 3 definiremos relaciones, mapeos de EF Core y restricciones únicas (avatar por usuario, perfil por usuario, desbloqueo por usuario/ítem y equipamiento por avatar/ranura). Esas restricciones también protegerán frente a solicitudes concurrentes. Al crear registro habrá que coordinar la creación de Identity, usuario y avatar en una transacción.

Los repositorios y sus interfaces se implementarán cuando existan los casos de acceso a datos. No se han añadido paquetes al dominio ni implementado los pasos de base de datos, endpoints o frontend.
