# Awoorld: paso 1, arquitectura del backend

> **Nota histórica:** este documento describe una etapa anterior. La estructura y autenticación vigentes están en [README](../README.md) y [registro/login actual](08-registro-login.md).

## Contexto y forma de trabajo

- Producto: Awoorld, sitio para el fandom de un artista, con registro, login, perfil y avatar personalizable.
- Conservar por ahora los nombres técnicos MundoAwoo de la solución y sus proyectos.
- El desarrollador es junior con bases en C#, Web API, DDD, Clean Architecture y React.
- Explicar las decisiones con ejemplos y usar funciones y métodos sencillos, código real y comentarios que expliquen la lógica.
- Avanzar un paso a la vez y esperar confirmación antes del siguiente.
- Stack acordado: .NET 9, MySQL, EF Core, JWT e Identity; React para el cliente. En el paso 3 el usuario eligió MySQL en lugar de PostgreSQL. La representación 2D/3D y las herramientas de animación quedan por decidir y validar juntas.
- El chat y la aplicación móvil son objetivos futuros, fuera de la primera implementación.

## Inspección inicial

La solución ya contiene API, Application, Domain, Infrastructure y Tests, todos con destino net9.0. Las referencias entre proyectos respetan la dirección hacia el dominio. Program.cs registra controladores y OpenAPI; aún no hay entidades ni casos de uso implementados. Las carpetas funcionales contienen archivos .gitkeep.

Infrastructure tiene carpetas alternativas Data y Persistence, además de Repositories en la raíz. Usaremos Persistence como ubicación única para EF Core y sus repositorios. Esta propuesta no elimina ni reorganiza todavía las carpetas existentes.

## Estructura objetivo del primer backend

Este árbol es un mapa de archivos por implementar, no una lista de funcionalidades ya construidas. Los .csproj actuales se conservan en sus respectivas carpetas.

```text
MundoAwoo-Backend/
├── MundoAwoo.sln
├── README.md
├── docs/
│   └── 01-arquitectura-backend.md
├── MundoAwoo.Domain/
│   ├── Entities/
│   │   ├── Usuario.cs
│   │   ├── PerfilUsuario.cs
│   │   ├── Avatar.cs
│   │   ├── ItemDeAvatar.cs
│   │   ├── Categoria.cs
│   │   ├── ItemEquipado.cs
│   │   └── ItemDesbloqueado.cs
│   ├── Enums/
│   │   └── RanuraAvatar.cs
│   ├── ValueObjects/
│   │   └── ColorHex.cs
│   └── Interfaces/
│       ├── IUsuarioRepository.cs
│       ├── IAvatarRepository.cs
│       └── IItemDeAvatarRepository.cs
├── MundoAwoo.Application/
│   ├── DTOs/
│   │   ├── Autenticacion/
│   │   ├── Perfiles/
│   │   ├── Avatares/
│   │   └── Catalogo/
│   ├── Interfaces/
│   │   ├── IAutenticacionService.cs
│   │   ├── IPerfilService.cs
│   │   ├── IAvatarService.cs
│   │   ├── ICatalogoService.cs
│   │   └── IUnitOfWork.cs
│   ├── Services/
│   │   ├── PerfilService.cs
│   │   ├── AvatarService.cs
│   │   └── CatalogoService.cs
│   ├── Validators/
│   └── Exceptions/
├── MundoAwoo.Infrastructure/
│   ├── Persistence/
│   │   ├── AwoorldDbContext.cs
│   │   ├── UnitOfWork.cs
│   │   ├── Configurations/
│   │   ├── Repositories/
│   │   │   ├── UsuarioRepository.cs
│   │   │   ├── AvatarRepository.cs
│   │   │   └── ItemDeAvatarRepository.cs
│   │   ├── Migrations/
│   │   └── Seed/
│   ├── Identity/
│   │   ├── UsuarioIdentity.cs
│   │   └── AutenticacionService.cs
│   ├── Security/
│   │   └── JwtTokenService.cs
│   └── Options/
│       └── JwtOptions.cs
├── MundoAwoo.Api/
│   ├── Controllers/
│   │   ├── AutenticacionController.cs
│   │   ├── PerfilController.cs
│   │   ├── AvatarController.cs
│   │   └── CatalogoController.cs
│   ├── Middlewares/
│   │   └── ExceptionMiddleware.cs
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── Program.cs
│   ├── appsettings.json
│   └── appsettings.Development.json
└── MundoAwoo.Tests/
    ├── Domain/
    ├── Services/
    ├── Validators/
    └── Integration/
```

Las carpetas reservadas actuales (por ejemplo Hubs) pueden permanecer vacías hasta que exista una necesidad concreta. Los archivos de configuración no deben contener contraseñas ni claves JWT reales.

## Responsabilidades y dependencias

- Domain: entidades, objetos de valor y reglas de negocio. No conoce HTTP, EF Core, Identity ni React.
- Application: coordina casos de uso mediante servicios sencillos. Define DTOs y contratos para capacidades externas.
- Infrastructure: implementa almacenamiento, autenticación y generación de tokens.
- API: recibe HTTP, identifica al usuario autenticado, llama a los servicios y devuelve respuestas. Program.cs conecta contratos e implementaciones mediante inyección de dependencias.
- Tests: verifica las reglas y los casos de uso; no es una quinta capa de negocio.

Referencias existentes: Application -> Domain; Infrastructure -> Application y Domain; API -> Application e Infrastructure. La referencia API -> Infrastructure permite registrar implementaciones al arrancar, no justifica usar DbContext directamente desde controladores.

Ejemplo: AvatarController recibe una solicitud de equipar; AvatarService carga el avatar y comprueba acceso al ítem; Avatar.Equipar aplica las reglas; el repositorio y UnitOfWork guardan el cambio.

## Límites iniciales del dominio

- Usuario es raíz de agregado y contiene PerfilUsuario y su inventario de ItemDesbloqueado. Las credenciales se gestionan con Identity en Infrastructure. UsuarioIdentity estará vinculado al usuario de dominio mediante un identificador único.
- Avatar es otra raíz de agregado, vinculada por UsuarioId, y controla sus ItemEquipado. Un agregado es un conjunto de objetos que se modifica a través de una entidad principal para proteger sus reglas.
- ItemDeAvatar es la definición compartida de una pieza del catálogo; no se duplica para cada usuario.
- Categoria clasifica los ítems: ropa, accesorio, baile o gesto. Inicialmente se consulta como parte del catálogo, sin un servicio administrativo propio.
- ItemDesbloqueado registra qué ítems puede usar un usuario; ItemEquipado registra cuáles usa ahora su avatar. Se incorporarán cuando implementemos esas reglas.
- RanuraAvatar distingue posiciones como cabeza o torso: la categoría ropa por sí sola no evita equipar dos prendas incompatibles.
- ColorHex es un objeto de valor candidato para colores personalizables: su identidad es su valor y centraliza la validación. Solo lo implementaremos si el modelo inicial permite elegir colores.

La unicidad de Avatar.UsuarioId se reforzará en la base de datos. El registro deberá crear coherentemente la cuenta, el usuario, su perfil y su avatar. Definiremos la transacción al implementar registro y persistencia.

## Decisiones para aprender sin complicar el proyecto

Usar servicios, interfaces específicas, constructores y métodos explícitos. No introducir de entrada MediatR, CQRS, repositorios genéricos, clases base genéricas o eventos de dominio sin un caso que los necesite. IUnitOfWork tendrá el propósito concreto de confirmar juntos los cambios de una operación.

Los contratos de repositorios del dominio pertenecen a Domain; los contratos de autenticación y coordinación de la aplicación pertenecen a Application. Esta convención debe mantenerse de forma consistente.

El backend guarda identidades de ítems y configuración del avatar; el cliente dibuja y reproduce animaciones. La elección gráfica se decidirá con un prototipo y no condiciona estas cuatro capas.

Paso 2 confirmado e implementado: consultar [la guía del dominio](02-dominio.md). La inspección y el árbol anteriores documentan el punto de partida; la guía del paso 2 describe los archivos y decisiones implementados.
