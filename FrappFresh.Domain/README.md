# Domain

Modelo sencillo en .NET 9: clases con propiedades y relaciones, enums e interfaces CRUD. No contiene configuración de Entity Framework, servicios ni controladores.

## Entidades

Todas heredan de `Entity`, que contiene `Id`, `CreatedAt` y `UpdatedAt`.

| Entidad | Para qué sirve |
| --- | --- |
| `User` | Nombre, apellido, correo, teléfono, dirección, hash de contraseña, rol y estado activo. |
| `Frappe` | Nombre, descripción, sabor, base agua/leche, toppings, precio, imagen, indicador premium y disponibilidad. Tiene un solo tamaño; no hay propiedad ni entidad de tamaño. |
| `Order` | Usuario, número del pedido, datos de contacto, tipo de entrega, dirección, notas, estado y totales. |
| `OrderItem` | Frappé, cantidad, precio unitario y total de una línea del pedido. Conserva nombre, sabor y toppings vendidos aunque el catálogo cambie. |
| `Payment` | Intento de pago: pedido, monto, moneda, método, estado, proveedor, referencia de transacción y fecha de pago. |
| `RefreshToken` | Usuario, hash del token, vencimiento, revocación y hash del token que lo reemplazó. |

Relaciones: un usuario tiene pedidos y refresh tokens; un pedido tiene detalles y puede tener varios intentos de pago. Cada detalle referencia un frappé. Los detalles se guardan junto con el pedido, sin un repositorio adicional.

## Enums

- `UserRole`: cliente o administrador.
- `BeverageBase`: agua o leche. Premium es un atributo del frappé.
- `DeliveryType`: recoger en el local (`Pickup`) o envío a domicilio (`Delivery`).
- `OrderStatus`: pendiente, confirmado, preparando, listo, completado o cancelado.
- `PaymentMethod`: efectivo, tarjeta, transferencia o billetera digital.
- `PaymentStatus`: pendiente, aprobado, rechazado, cancelado o reembolsado.

## Interfaces

`IRepository<T>` define listar, buscar por ID, crear, actualizar y eliminar. `IUserRepository`, `IFrappeRepository`, `IOrderRepository`, `IPaymentRepository` e `IRefreshTokenRepository` heredan ese CRUD y agregan consultas específicas.

## Siguientes capas

Application tendrá las validaciones y cálculos: precios y cantidades válidos, dirección cuando sea domicilio, totales calculados desde el catálogo y permisos por rol. Infrastructure ya implementa persistencia, relaciones, índices únicos y repositorios. Los cambios de pedido y pago que deban ir juntos deben compartir una transacción.

Infrastructure ya incluye emisión y validación JWT Bearer, hash de contraseñas y renovación con rotación de refresh tokens. Faltan los casos de uso y endpoints de autenticación. `PasswordHash` y `TokenHash` almacenan hashes, y la API usará DTOs para no devolverlos. El registro asignará el rol cliente desde el servidor.

La pasarela concreta aún no está elegida. `Payment` permite almacenar su referencia; faltan la integración y la verificación de notificaciones para confirmar pagos desde el servidor. No se almacenan datos de tarjetas.

No se incluyen FreshFamily, ruleta, eventos, merch ni entidades separadas de sabores, categorías o toppings en este modelo.
