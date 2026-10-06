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

Abre Swagger en http://localhost:5248/swagger usando el perfil HTTP:

```bash
dotnet run --project FrappFresh.Api --launch-profile http
```

La API necesita una conexión MySQL válida y los secretos locales descritos abajo para arrancar.
El endpoint de verificación es `GET /api/health`; indica que la API responde, no comprueba la conexión a MySQL.

## Controllers y lógica

El flujo se conserva: **Controller → Service → Repository → MySQL**.
Los controllers reciben DTOs, llaman al servicio y devuelven `Ok`, `CreatedAtAction` o `NoContent`.
Los servicios existentes siguen validando los datos, calculando los totales y comprobando permisos.
No se agregaron reglas de negocio a los controllers.

| Controller | Ruta | Funciones |
| --- | --- | --- |
| Auth | `/api/auth` | `POST register`, `login`, `refresh`, `logout`. |
| Users | `/api/users` | Listar, consultar, crear, editar y desactivar; `GET me`, `PUT me/password`, `PATCH {id}/role` y `{id}/status`. |
| Frappes | `/api/frappes` | Catálogo público en `GET catalog`, detalle público de productos activos, administración CRUD. |
| Orders | `/api/orders` | Listar, `GET mine`, consultar, crear, editar, `PATCH {id}/status` y `{id}/cancel`. |
| Payments | `/api/payments` | Listar, consultar, `GET order/{orderId}`, crear, `PATCH {id}/confirm-cash` y `{id}/cancel`. |

Los listados generales de usuarios, pedidos y pagos son de administrador.
Cada cliente solo consulta o modifica sus propios datos y pedidos, según las reglas del servicio.
Eliminar usuarios y frappés los desactiva; cancelar pedidos usa su endpoint específico.
Crear un pago devuelve `200` porque el servicio puede devolver un intento pendiente existente.

`ExceptionMiddleware` convierte validaciones y reglas de negocio en `400`, falta de sesión en
`401`, permisos insuficientes en `403`, registros inexistentes en `404` y conflictos en `409`.
Los errores inesperados devuelven `500` sin detalles internos. Los enums se muestran como texto en Swagger,
por ejemplo `Pickup`, `Delivery` y `Cash`.

## Probar en Swagger

1. Ejecuta `POST /api/auth/register` para crear un cliente o `POST /api/auth/login` para iniciar sesión.
2. Copia `tokens.accessToken` de la respuesta y pégalo en **Authorize**, sin escribir el prefijo Bearer.
3. Consulta `GET /api/frappes/catalog` y copia el ID de un frappé disponible.
4. Crea un pedido con `POST /api/orders`:

```json
{
  "customerName": "Cliente de prueba",
  "customerPhone": "3001234567",
  "deliveryType": "Pickup",
  "items": [
    { "frappeId": "REEMPLAZAR-POR-ID-DEL-CATALOGO", "quantity": 1 }
  ]
}
```

5. Crea un pago en `POST /api/payments` con `{ "orderId": "ID-DEL-PEDIDO", "method": "Cash" }`.
6. Inicia sesión como administrador, reemplaza el token en **Authorize** y confirma el efectivo con
   `PATCH /api/payments/{id}/confirm-cash`. Esta acción registra una confirmación; no cobra dinero.
7. Cambia el estado del pedido con `PATCH /api/orders/{id}/status`, usando
   `{ "status": "Confirmed" }`, luego `Preparing`, `Ready` y `Completed`.

Para desarrollo, los usuarios del seed son `admin@frappfresh.test` y `cliente@frappfresh.test`.
Consulta solo sus contraseñas localmente en User Secrets:

```powershell
dotnet user-secrets list --project FrappFresh.Api | Select-String '^Seed:'
```

Las pruebas HTTP verifican rutas, documentación Swagger, rechazo de peticiones anónimas y respuestas de error.
Las pruebas que requieren MySQL se omiten si no se configura `FRAPPFRESH_TEST_MYSQL`.

## Infrastructure

MySQL, las migraciones, los seeds, los repositorios CRUD, los servicios de seguridad y los controllers están implementados. La API registra estas dependencias y configura JWT Bearer y Swagger en Development.

La conexión completa, la clave JWT y las contraseñas de prueba se guardan en User Secrets de este equipo.
`appsettings.Development.json` conserva una conexión de referencia sin contraseña.
Consulta [la guía de Infrastructure](FrappFresh.Infrastructure/README.md) para configurar otro equipo y ejecutar migraciones y pruebas contra MySQL.
