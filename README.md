# FrappFresh Backend

Backend de FrappFresh Ghetto construido con **ASP.NET Core 9**, arquitectura **Clean Architecture + DDD**, Entity Framework Core y MySQL.

## Ramas

- `main`: rama estable del proyecto.
- `develop`: rama de integración con la estructura base y el desarrollo activo.

## Arquitectura

La solución está dividida en cuatro capas principales:

- `FrappFresh.Domain`: entidades, enumeraciones, interfaces y reglas del dominio.
- `FrappFresh.Application`: casos de uso, DTOs, validaciones y contratos de aplicación.
- `FrappFresh.Infrastructure`: persistencia con MySQL, `AppDbContext`, repositorios e integraciones externas.
- `FrappFresh.Api`: API HTTP, configuración, middlewares y composición de dependencias.

También existe `FrappFresh.Tests` para pruebas automatizadas de servicios y validadores.

## Requisitos

- .NET SDK 9
- MySQL 8+

## Desarrollo

La estructura inicial y los proyectos compilables se encuentran en la rama `develop`.

```bash
git switch develop
dotnet restore FrappFresh.sln
dotnet build FrappFresh.sln
dotnet run --project FrappFresh.Api
```

La cadena de conexión se configura mediante `ConnectionStrings:DefaultConnection`.
Para desarrollo local puede sobrescribirse sin modificar el repositorio:

```bash
export ConnectionStrings__DefaultConnection="Server=localhost;Port=3306;Database=frappfresh;User=root;Password=TU_PASSWORD;"
```

El endpoint inicial de verificación es `GET /api/health`.
