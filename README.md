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

## Requisitos

- .NET SDK 9

## Desarrollo

La estructura inicial se encuentra en la rama `develop`.
