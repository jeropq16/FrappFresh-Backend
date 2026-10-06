# Infrastructure

Implementación sencilla de MySQL + EF Core 9, repositorios CRUD y seguridad. Mantiene las seis entidades del dominio.

## Carpetas

| Carpeta | Contenido |
| --- | --- |
| `Options` | Configuración tipada de MySQL, JWT y seeds. |
| `Persistence` | `AppDbContext`, conversión de fechas UTC e inicialización de base de datos. |
| `Persistence/Configurations` | Un mapeo EF por entidad: tablas, relaciones, longitudes, índices y precisión de precios. |
| `Persistence/Seed` | Usuarios, catálogo, pedidos y pagos ficticios para desarrollo. |
| `Migrations` | Migración inicial y snapshot del modelo. |
| `Repositories` | CRUD genérico e implementaciones para usuarios, frappés, pedidos, pagos y refresh tokens. |
| `Security` | Hash de contraseñas, JWT y creación, rotación y revocación de refresh tokens. |

Las carpetas vacías `Data` y `Persistence/Repositories` de la estructura original quedan reservadas; no hay implementaciones duplicadas. El registro de dependencias está en `DependencyInjection.cs`.

## Conexión y ejecución local

La base configurada en este equipo es `FrappFreshDb`, en `localhost:3306`. La conexión está temporalmente en `FrappFresh.Api/appsettings.Development.json`, bajo `ConnectionStrings:DefaultConnection`. La clave JWT, la versión del servidor y las contraseñas de los usuarios de prueba siguen en User Secrets. La conexión se trasladará a variables de entorno más adelante.

```bash
dotnet tool restore
dotnet restore FrappFresh.sln
dotnet run --project FrappFresh.Api
```

El perfil local usa `Development`: aplica las migraciones pendientes y ejecuta los seeds. En otros entornos esas opciones están desactivadas por defecto; los seeds rechazan expresamente cualquier entorno distinto de `Development`.

Para otro equipo, ajusta la conexión de `appsettings.Development.json` y configura estos secretos con valores propios:

```bash
dotnet user-secrets set "Jwt:SigningKey" "<clave-aleatoria-de-al-menos-32-bytes>" --project FrappFresh.Api
dotnet user-secrets set "Seed:AdminPassword" "<clave-de-prueba-de-al-menos-12-caracteres>" --project FrappFresh.Api
dotnet user-secrets set "Seed:CustomerPassword" "<otra-clave-de-prueba-de-al-menos-12-caracteres>" --project FrappFresh.Api
```

`Database:ServerVersion` usa `8.0.36` por defecto. En este equipo está configurada como secreto con la versión detectada `9.6.0`, con la que se verificaron las migraciones y las pruebas. Fuera de desarrollo puedes usar variables de entorno como `ConnectionStrings__DefaultConnection` y `Jwt__SigningKey`.

## Seeds de prueba

- Administrador: `admin@frappfresh.test`.
- Cliente: `cliente@frappfresh.test`.
- Seis frappés del catálogo.
- Pedidos `DEMO-001` para recoger y `DEMO-002` a domicilio, con sus detalles.
- Un pago aprobado de ejemplo y otro pendiente; ninguno realiza un cobro real.
- Un refresh token vencido y revocado para consultar su estructura; no permite iniciar sesión.

Las contraseñas de ambos usuarios se generaron para este equipo. Puedes consultarlas localmente mediante `dotnet user-secrets list --project FrappFresh.Api`, en `Seed:AdminPassword` y `Seed:CustomerPassword`. Esa salida también contiene la clave JWT.

El seed no duplica registros ni sobrescribe contraseñas, precios o cambios existentes. Cambiar una contraseña en la configuración del seed no modifica una cuenta ya creada. Los seis productos usan IDs fijos; los pedidos se identifican por su número y los usuarios por correo normalizado.

## Persistencia y repositorios

Cada método de escritura ejecuta `SaveChangesAsync`. Los listados y consultas usan `AsNoTracking`; para guardar una entidad modificada se llama a `UpdateAsync`. Actualizar un registro inexistente o eliminarlo produce `KeyNotFoundException`.

`OrderRepository` carga detalles y pagos. Al actualizar, sincroniza las líneas recibidas: agrega las nuevas, actualiza las existentes y elimina las omitidas, en una transacción. Los pagos se gestionan por su propio repositorio. Si un futuro caso de uso combina varias escrituras de repositorios, debe compartir una transacción de `AppDbContext`.

Los índices impiden duplicar correo, número de pedido, hash de refresh token y transacción de un mismo proveedor. Las claves foráneas conservan las referencias a usuarios y productos vendidos. Eliminar un pedido elimina sus líneas, pero se rechaza si tiene pagos asociados. Los importes usan `decimal(12,2)`; las fechas se guardan en UTC y las fechas de auditoría se actualizan al guardar.

## Seguridad

- `PasswordService` usa `PasswordHasher<User>` de ASP.NET Core Identity. Si verifica un hash antiguo y lo actualiza, el caso de uso debe guardar el usuario.
- `JwtTokenService` genera JWT HS256 con usuario, correo, nombre y rol. Bearer valida firma, emisor, audiencia y vencimiento; además comprueba que el usuario continúe activo y conserve su rol.
- `RefreshTokenService.CreateAsync` se invoca después de comprobar las credenciales. Genera tokens aleatorios y solo guarda sus hashes SHA-256.
- `RotateAsync` devuelve nuevos tokens y revoca el anterior atómicamente. Devuelve `null` si el token no existe, venció, ya fue consumido o pertenece a un usuario inactivo. La concurrencia sobre `RevokedAt` evita dos renovaciones del mismo token.
- `RevokeAsync` permite cerrar una sesión e invalida futuras renovaciones. Un JWT ya emitido conserva su vigencia hasta su vencimiento; desactivar el usuario o cambiar su rol sí lo invalida en las solicitudes siguientes.

Los contratos de estos servicios están en `Application/Interfaces`, con dos resultados sencillos en `Application/Security`. Faltan los casos de uso y endpoints de registro, login, refresh y logout de las siguientes capas. La integración con una pasarela concreta también queda pendiente.

## Migraciones y pruebas

```bash
dotnet ef migrations add NombreDelCambio --project FrappFresh.Infrastructure --startup-project FrappFresh.Api --output-dir Migrations -- --environment Development
dotnet ef database update --project FrappFresh.Infrastructure --startup-project FrappFresh.Api -- --environment Development
dotnet test FrappFresh.sln
```

Para incluir las pruebas de integración, define `FRAPPFRESH_TEST_MYSQL` con una conexión a MySQL y un usuario autorizado a crear bases de prueba. La fixture crea y elimina exclusivamente una base nueva `FFTests_<identificador>`; no reutiliza los datos de `FrappFreshDb`. Si falta esa variable, las pruebas de MySQL se marcan como omitidas. Se verifican migraciones, CRUD, integridad, seeds, fechas UTC, JWT, roles y rotación concurrente.

Referencias de implementación: [proveedor Pomelo](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql), [validación JWT Bearer](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-9.0) y [migraciones EF Core](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying).
