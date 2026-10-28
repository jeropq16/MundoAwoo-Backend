# MundoAwoo Backend

Backend de MundoAwoo construido con **ASP.NET Core 9** y una arquitectura basada en **Clean Architecture + DDD**.

## Ramas

- `main`: rama estable del proyecto.
- `develop`: rama de integración con la estructura base y el desarrollo activo.

## Arquitectura

La solución está dividida en cuatro capas principales:

- `MundoAwoo.Domain`: entidades, enumeraciones, interfaces y reglas del dominio.
- `MundoAwoo.Application`: casos de uso, DTOs, validaciones y contratos de aplicación.
- `MundoAwoo.Infrastructure`: persistencia, repositorios e integraciones externas.
- `MundoAwoo.Api`: API HTTP, configuración, middlewares y composición de la aplicación.

También existe `MundoAwoo.Tests` para las pruebas automatizadas.

## Guías paso a paso

- [Paso 1: arquitectura y acuerdos de trabajo](docs/01-arquitectura-backend.md).
- [Paso 2: entidades, propiedades, métodos y ejemplo de uso](docs/02-dominio.md).
- [Paso 3: MySQL, Workbench, relaciones y migraciones](docs/03-mysql.md).
- [Interfaces del dominio: contratos y explicación de cada método](docs/04-interfaces-dominio.md).
- [Repositorios en Infrastructure: consultas, seguimiento y unidad de trabajo](docs/05-repositorios-infrastructure.md).
- [Servicio de perfil en Application: DTOs, consulta y actualización](docs/06-servicio-perfil.md).
- [API de perfil: endpoints protegidos con JWT y manejo de errores](docs/07-api-perfil.md).
- [Registro y login: Identity, JWT y migración de cuentas](docs/08-registro-login.md).

## Requisitos de desarrollo

- .NET SDK 9
- MySQL Community Server 8.4 (y Workbench para consultar visualmente).
- Conexión `ConnectionStrings:Awoorld` en User Secrets (Development) o variables de entorno. Ver la guía del paso 3 antes de iniciar la API.
- Clave aleatoria `Jwt:SigningKey` en User Secrets o `Jwt__SigningKey` en el entorno. Ver la guía de API y aplicar también la migración `AgregarIdentity` antes de usar registro y login.

## Desarrollo

La estructura inicial se encuentra en la rama `develop`.
