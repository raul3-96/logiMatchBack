# LogiMatch

## Descripción del proyecto

`LogiMatch` es una plataforma logística que conecta clientes y transportistas para gestionar el ciclo completo de un servicio de transporte.

El backend implementa la lógica de negocio para:

* usuarios y autenticación
* empresas y miembros de empresa
* perfiles de transportista
* vehículos y disponibilidad
* solicitudes de transporte y cargas
* ofertas y bookings
* viajes y reserva de capacidad
* matching de vehículos y viajes compatibles
* seguimiento operativo de cargas y servicios
* control de permisos según propietario, empresa y rol

La solución está diseñada como un marketplace logístico, manteniendo separadas las responsabilidades de API, aplicación, dominio y persistencia.

---

## Arquitectura

La solución está organizada en capas:

* `LogiMatch.Api`: exposición HTTP mediante controladores REST, autenticación y configuración de la aplicación.
* `LogiMatch.Application`: casos de uso, handlers, comandos, DTOs, servicios de aplicación y autorización funcional.
* `LogiMatch.Domain`: entidades, enums y reglas de negocio independientes de infraestructura.
* `LogiMatch.Infrastructure`: persistencia mediante Entity Framework Core y PostgreSQL.

Las dependencias siguen el esquema:

```text
LogiMatch.Api
    ├── LogiMatch.Application
    └── LogiMatch.Infrastructure

LogiMatch.Application
    └── LogiMatch.Domain

LogiMatch.Infrastructure
    └── LogiMatch.Domain
```

La lógica de negocio no se concentra en los controladores. Los controladores reciben la petición y delegan en handlers y servicios de aplicación.

---

# Autenticación

La API utiliza autenticación mediante JWT.

El login valida:

1. existencia del usuario
2. estado activo del usuario
3. existencia de contraseña configurada
4. contraseña mediante `PasswordHasher<User>`
5. generación de un JWT con la identidad del usuario

El token contiene identificadores y datos básicos del usuario para que la aplicación pueda determinar el usuario autenticado.

Los endpoints que requieren identidad están protegidos mediante `[Authorize]`. Los recursos explícitamente públicos pueden utilizar `[AllowAnonymous]`.

### Configuración

La configuración JWT debe mantenerse fuera del código fuente cuando se utilice un entorno real.

La clave secreta debe proporcionarse mediante:

* configuración segura
* variables de entorno
* User Secrets

No se debe almacenar una clave JWT real en el repositorio.

---

# Modelo de empresa y permisos

Una empresa tiene un propietario mediante `Company.OwnerUserId`.

Los demás usuarios relacionados con una empresa se gestionan mediante `CompanyMember`.

Cada miembro tiene:

* empresa
* usuario
* rol
* estado activo/inactivo
* fecha de alta

Los roles disponibles son:

* `Admin`
* `Worker`

El propietario de la empresa no necesita ser un `CompanyMember`.

## Propietario

El propietario puede:

* añadir miembros
* activar miembros
* desactivar miembros
* promover miembros a Admin
* degradar Admin a Worker
* gestionar los recursos de la empresa
* consultar la información privada de su empresa

El propietario no puede añadirse a sí mismo como miembro ni gestionarse mediante las operaciones destinadas a miembros.

## Admin

Un Admin activo puede:

* consultar los miembros de la empresa
* activar miembros
* desactivar miembros
* gestionar los viajes y recursos de la empresa que la lógica de negocio le permita

Un Admin no puede:

* añadir nuevos miembros
* promover miembros
* degradar miembros

Las operaciones de estructura de roles quedan reservadas al propietario.

## Worker

Un Worker activo puede utilizar sus propios recursos asociados a la empresa.

Un Worker no puede administrar los recursos pertenecientes a otros miembros de la empresa.

## Miembros inactivos

Un miembro inactivo pierde los permisos derivados de su pertenencia activa a la empresa.

Esto también se aplica cuando el usuario tiene un perfil de transportista asociado a esa empresa.

Un perfil propio asociado a una empresa solo es gestionable si:

* el usuario es propietario de la empresa, o
* el usuario es miembro activo de la empresa

Un perfil propio sin empresa puede seguir siendo gestionado por su propietario.

---

# Datos públicos y privados

La API diferencia entre información pública y privada.

## Empresas

La consulta pública de una empresa expone únicamente información pública como:

* `Id`
* `Name`
* `CreatedAt`

La información privada de empresa se consulta mediante:

```text
GET /api/companies/{id}/private
```

y requiere autorización del propietario o de un Admin activo.

Los datos privados incluyen:

* `TaxId`
* `Email`
* `Phone`
* `OwnerUserId`

## Perfiles de transportista

La consulta pública de perfiles no expone información privada del usuario.

La información pública puede incluir:

* Id
* nombre
* apellidos
* nombre de empresa
* fecha de creación

Los datos privados del usuario y de la empresa se mantienen fuera de las proyecciones públicas.

---

# Entidades principales

## User

Representa al usuario de la plataforma.

Incluye identidad, datos personales, estado y credenciales de autenticación.

Estados:

* `Active`
* `Suspended`
* `Deleted`

## Company

Representa una empresa transportista.

Tiene un propietario mediante `OwnerUserId`.

## CompanyMember

Relaciona usuarios con empresas y determina su rol y estado activo.

## TransporterProfile

Representa el perfil mediante el cual un usuario actúa como transportista.

Puede ser:

* independiente
* asociado a una empresa

Cada usuario puede tener un perfil de transportista.

## Vehicle

Representa un vehículo asociado a un perfil de transportista.

Incluye:

* matrícula
* tipo
* capacidad de peso
* capacidad de volumen
* dimensiones
* refrigeración
* plataforma elevadora

## VehicleAvailability

Define una ventana temporal en la que un vehículo está disponible.

La fecha final debe ser posterior a la fecha inicial.

## Location

Representa una ubicación de recogida o entrega.

## TransportRequest

Representa la necesidad de transporte de un cliente.

Puede contener una o varias cargas.

## Cargo

Representa la mercancía incluida en una solicitud.

Puede indicar requisitos especiales como:

* refrigeración
* plataforma elevadora

## TransportOffer

Representa una oferta realizada por un transportista sobre una solicitud.

Incluye:

* precio
* vehículo
* perfil de transportista
* fecha estimada de recogida
* fecha estimada de entrega

## Booking

Representa la reserva generada cuando el cliente acepta una oferta.

## Trip

Representa un viaje publicado por un transportista.

Incluye:

* perfil de transportista
* vehículo
* origen
* destino
* salida
* llegada estimada
* capacidad inicial
* capacidad disponible
* estado

## TripCargo

Relaciona una solicitud de transporte con un viaje concreto y representa la capacidad reservada.

Mantiene el peso y volumen reservados para controlar la capacidad restante del viaje.

---

# Estados de negocio

## TransportRequest

Estados principales:

```text
Draft
  ↓
Published
  ↓
Matching
  ↓
OffersReceived
  ↓
Accepted
  ↓
InProgress
  ↓
Completed
```

También puede terminar en:

```text
Cancelled
Expired
```

Una solicitud aceptada mediante una oferta utiliza:

```text
Fulfillment = Offer
```

Una solicitud asignada directamente a un viaje utiliza:

```text
Fulfillment = Trip
```

Cuando una reserva de viaje se cancela correctamente, la solicitud puede volver a:

```text
Published
```

## TransportOffer

```text
Pending
  ├── Accepted
  ├── Rejected
  ├── Cancelled
  └── Expired
```

Solo una oferta pendiente puede ser aceptada, rechazada o cancelada.

## Booking

```text
Confirmed
  ↓
InProgress
  ↓
Completed
```

También puede pasar de `Confirmed` a `Cancelled`.

## Trip

```text
Published
  ↓
InProgress
  ↓
Completed
```

Un viaje publicado puede cancelarse.

Un viaje no puede completarse mientras tenga cargas activas en estado:

* `Reserved`
* `InProgress`

## TripCargo

```text
Reserved
  ↓
InProgress
  ↓
Completed
```

Una carga reservada puede cancelarse mientras el viaje continúe publicado.

---

# Capacidad de los viajes

Los viajes mantienen dos conceptos de capacidad:

* capacidad inicial
* capacidad disponible

Cuando una solicitud se reserva en un viaje:

* se descuenta el peso
* se descuenta el volumen

Cuando una reserva se cancela correctamente:

* se libera el peso
* se libera el volumen

La liberación no puede superar la capacidad inicial del viaje.

La reserva comprueba:

* peso disponible
* volumen disponible
* refrigeración
* plataforma elevadora
* origen
* destino
* fechas

---

# Concurrencia en reservas

La reserva de capacidad de un viaje se realiza dentro de una transacción.

En PostgreSQL se utiliza bloqueo de fila mediante `FOR UPDATE` cuando la infraestructura soporta bloqueo de filas.

Además, existe una restricción única para impedir varias reservas activas de la misma solicitud.

De esta forma se controlan dos problemas diferentes:

* concurrencia sobre la capacidad disponible del viaje
* duplicación de una reserva activa para una misma solicitud

Las reservas canceladas no bloquean una nueva reserva de la misma solicitud.

---

# Matching

El sistema dispone de lógica para encontrar recursos compatibles con una solicitud.

## Matching de vehículos

Se tienen en cuenta las condiciones de la solicitud y las características del vehículo, incluyendo:

* capacidad de peso
* capacidad de volumen
* refrigeración
* plataforma elevadora
* disponibilidad temporal

## Matching de viajes

Los viajes compatibles se filtran utilizando:

* origen
* destino
* capacidad de peso
* capacidad de volumen
* refrigeración
* plataforma elevadora
* fecha de recogida
* fecha estimada de llegada
* fecha límite de entrega cuando existe

El matching devuelve información de capacidad restante para facilitar la selección del viaje.

La parte de geolocalización avanzada, PostGIS y cálculo de rutas queda pendiente de una fase posterior.

---

# Permisos sobre viajes

La gestión de viajes utiliza una política centralizada mediante:

```text
ITripManagementAccessService
```

Este servicio determina qué perfiles de transportista puede gestionar el usuario actual.

La política distingue entre:

* perfil propio independiente
* perfil propio asociado a empresa
* propietario de empresa
* Admin activo
* Worker activo
* miembro inactivo

La autorización se aplica a operaciones como:

* crear viajes
* iniciar viajes
* completar viajes
* cancelar viajes
* gestionar cargas de viaje

La reserva de capacidad de un viaje sigue siendo una operación del cliente que realiza la solicitud y no se convierte en una operación de Owner/Admin.

---

# TripCargo y permisos operativos

Las operaciones de una carga asignada a un viaje se dividen según responsabilidad.

## Gestión del transporte

El propietario de empresa, Admin activo o propietario autorizado del perfil puede:

* iniciar la carga
* completar la carga

## Cliente

El cliente propietario de la solicitud puede:

* cancelar la reserva de su carga mientras el viaje siga publicado

Esto permite separar la responsabilidad operativa del transportista de la capacidad del cliente para cancelar su propia reserva.

---

# Reglas principales de negocio

Entre las validaciones implementadas se encuentran:

* no se puede publicar una solicitud sin cargas
* una oferta solo puede aceptarse si está pendiente
* una reserva solo puede hacerse sobre un viaje publicado
* una solicitud solo puede reservarse si pertenece al usuario autenticado
* una solicitud no puede tener simultáneamente varias reservas activas
* un viaje debe tener capacidad suficiente para la reserva
* el vehículo debe cumplir los requisitos de la carga
* origen y destino del viaje deben coincidir con la solicitud
* las fechas del viaje deben ser compatibles con las fechas solicitadas
* un viaje solo puede iniciarse si está publicado
* un viaje solo puede completarse si está en progreso
* un viaje no puede completarse mientras tenga cargas activas
* una carga de viaje solo puede iniciarse si está reservada
* una carga de viaje solo puede completarse si está en progreso
* una carga de viaje solo puede cancelarse si está reservada
* una reserva solo puede iniciarse si está confirmada
* una reserva solo puede completarse si está en progreso
* una reserva solo puede cancelarse si está confirmada
* un viaje solo puede cancelarse mientras esté publicado
* un miembro inactivo no conserva permisos derivados de la empresa
* un Worker no puede administrar recursos de otros miembros
* un Admin no puede modificar la estructura de roles de la empresa
* el propietario de una empresa es quien controla la gestión de miembros

---

# API

## Usuarios

```text
POST /api/users
```

Crea un usuario.

Endpoint de autenticación/login:

* autentica al usuario
* valida sus credenciales
* devuelve un JWT

---

## Empresas

```text
POST /api/companies
```

Crea una empresa.

```text
GET /api/companies
```

Lista información pública de empresas.

```text
GET /api/companies/{id}
```

Consulta información pública de una empresa.

```text
GET /api/companies/{id}/private
```

Consulta información privada de una empresa.

Solo:

* propietario
* Admin activo

---

## Miembros de empresa

```text
POST /api/companies/{companyId}/members
```

Añade un miembro.

Solo el propietario puede realizar esta operación.

```text
GET /api/companies/{companyId}/members
```

Consulta los miembros de la empresa.

```text
POST /api/companies/{companyId}/members/{memberId}/activate
```

Activa un miembro.

```text
POST /api/companies/{companyId}/members/{memberId}/deactivate
```

Desactiva un miembro.

```text
POST /api/companies/{companyId}/members/{memberId}/promote
```

Promueve un Worker a Admin.

Solo el propietario.

```text
POST /api/companies/{companyId}/members/{memberId}/demote
```

Degrada un Admin a Worker.

Solo el propietario.

---

## Ubicaciones

```text
POST /api/locations
```

Crea una ubicación.

---

## Perfiles de transportista

```text
POST /api/transporter-profiles
```

Crea un perfil de transportista.

```text
GET /api/transporter-profiles
```

Lista perfiles públicos.

```text
GET /api/transporter-profiles/{id}
```

Consulta un perfil respetando la separación entre información pública y privada.

---

## Vehículos

```text
POST /api/vehicles
```

Crea un vehículo.

---

## Disponibilidad

```text
POST /api/vehicle-availabilities
```

Registra una ventana de disponibilidad.

---

## Cargas

```text
POST /api/cargos
```

Añade una carga a una solicitud.

---

## Solicitudes de transporte

```text
POST /api/transport-requests
```

Crea una solicitud.

```text
GET /api/transport-requests/{id}
```

Consulta una solicitud.

```text
POST /api/transport-requests/{id}/publish
```

Publica una solicitud.

```text
POST /api/transport-requests/{id}/cancel
```

Cancela una solicitud cuando el estado lo permite.

---

## Ofertas

```text
POST /api/transport-offers
```

Crea una oferta.

```text
GET /api/transport-offers/request/{transportRequestId}
```

Lista las ofertas de una solicitud.

```text
POST /api/transport-offers/{offerId}/accept
```

Acepta una oferta y genera un booking.

---

## Bookings

```text
GET /api/bookings/{id}
```

Consulta una reserva.

```text
POST /api/bookings/{id}/start
```

Inicia una reserva.

```text
POST /api/bookings/{id}/complete
```

Completa una reserva.

```text
POST /api/bookings/{id}/cancel
```

Cancela una reserva.

---

## Trips

```text
POST /api/trips
```

Crea un viaje.

```text
GET /api/trips/{id}
```

Consulta un viaje.

```text
POST /api/trips/{id}/start
```

Inicia un viaje.

```text
POST /api/trips/{id}/complete
```

Completa un viaje.

```text
POST /api/trips/{id}/cancel
```

Cancela un viaje.

---

## Matching

```text
POST /api/matching/vehicles
```

Busca vehículos compatibles con una solicitud.

El sistema también dispone de lógica de matching de viajes para encontrar viajes compatibles con una solicitud.

---

## TripCargo

Las operaciones de las cargas asociadas a viajes permiten:

* consultar cargas
* iniciar cargas
* completar cargas
* cancelar reservas de cargas

---

# Flujos principales

## Flujo 1: transportista independiente

1. Crear usuario.
2. Crear perfil de transportista.
3. Crear vehículo.
4. Crear disponibilidad.
5. Crear un viaje.
6. Publicar el viaje.
7. Buscar solicitudes compatibles.
8. Reservar capacidad.
9. Iniciar el viaje.
10. Iniciar las cargas.
11. Completar las cargas.
12. Completar el viaje.

---

## Flujo 2: empresa

1. Crear usuario propietario.
2. Crear empresa.
3. Añadir trabajadores.
4. Activar o desactivar miembros cuando sea necesario.
5. Promover o degradar miembros.
6. Cada transportista crea su perfil asociado a la empresa.
7. Cada perfil puede tener sus vehículos.
8. El propietario o un Admin activo puede gestionar viajes de la empresa.
9. Un Worker activo puede gestionar sus propios recursos.
10. Un miembro inactivo pierde los permisos derivados de la empresa.

---

## Flujo 3: solicitud mediante oferta

1. Crear usuario cliente.
2. Crear solicitud.
3. Añadir una o varias cargas.
4. Publicar la solicitud.
5. Un transportista crea una oferta.
6. El cliente consulta las ofertas.
7. El cliente acepta una oferta.
8. Se crea un booking.
9. El booking pasa de `Confirmed` a `InProgress`.
10. El booking pasa a `Completed`.

---

## Flujo 4: solicitud asignada a un viaje

1. Crear solicitud.
2. Añadir cargas.
3. Publicar la solicitud.
4. Buscar viajes compatibles.
5. Seleccionar un viaje compatible.
6. Reservar capacidad.
7. La solicitud pasa a `Accepted` con `Fulfillment = Trip`.
8. El viaje puede iniciarse.
9. Las cargas pasan a `InProgress`.
10. Las cargas se completan.
11. El viaje se completa.

---

## Flujo 5: cancelación de una reserva de viaje

1. Una solicitud está asignada a un viaje.
2. El viaje sigue en estado `Published`.
3. El cliente cancela el `TripCargo`.
4. Se libera peso y volumen.
5. La solicitud vuelve a `Published`.
6. La capacidad liberada vuelve a estar disponible.
7. La solicitud puede volver a reservarse en otro viaje compatible.

---

# Persistencia

La persistencia utiliza:

* Entity Framework Core
* PostgreSQL
* migraciones EF Core

El contexto de infraestructura implementa:

```text
IApplicationDbContext
```

permitiendo que la capa Application trabaje contra una abstracción.

Existen restricciones e índices de base de datos para reforzar reglas importantes, entre ellas:

* email de usuario único
* TaxId de empresa único
* un miembro por usuario y empresa
* una reserva activa por solicitud
* una reserva activa única para evitar duplicidades

---

# Concurrencia

Las operaciones críticas de reserva utilizan transacciones.

La reserva de capacidad:

1. obtiene el viaje
2. comprueba estado y capacidad
3. valida la solicitud
4. valida vehículo y requisitos
5. reserva peso y volumen
6. crea el `TripCargo`
7. asigna la solicitud
8. guarda los cambios
9. confirma la transacción

En PostgreSQL se utiliza bloqueo de fila cuando está disponible.

Esto permite proteger la capacidad del viaje frente a peticiones concurrentes.

---

# Testing

El proyecto dispone de tests para validar las reglas de dominio y aplicación.

La estrategia contempla:

* reglas de entidades
* transiciones de estado
* permisos de empresa
* permisos de Owner/Admin/Worker
* miembros activos e inactivos
* reservas de capacidad
* concurrencia
* matching
* casos de error

Los tests de aplicación utilizan una infraestructura de pruebas independiente de la base de datos PostgreSQL de producción.

---

# Tecnologías

* C#
* .NET
* ASP.NET Core
* Entity Framework Core
* PostgreSQL
* JWT
* xUnit
* Swagger / OpenAPI

El frontend React se desarrolla como proyecto separado y consume esta API.

---

# Desarrollo y pruebas

Para probar la API durante el desarrollo:

1. configurar la base de datos
2. aplicar las migraciones
3. configurar JWT
4. iniciar `LogiMatch.Api`
5. utilizar Swagger para ejecutar las operaciones

Es recomendable respetar el orden lógico de creación de entidades y las transiciones de estado definidas por el dominio.

---

# Estado actual del proyecto

La lógica principal del backend está estructurada alrededor de:

* autenticación de usuarios
* marketplace de transporte
* empresas y permisos por rol
* perfiles de transportista
* vehículos
* solicitudes y cargas
* ofertas
* bookings
* viajes
* reserva de capacidad
* TripCargo
* matching
* control de concurrencia

La lógica de negocio principal se mantiene en `Domain` y `Application`.

Los permisos de gestión de viajes se centralizan mediante:

```text
ITripManagementAccessService
```

La geolocalización avanzada mediante PostGIS y funcionalidades de cálculo de rutas quedan reservadas para una fase posterior.

---

# Instalación

Como mínimo se requiere:

* SDK de .NET compatible con el proyecto
* PostgreSQL
* configuración de conexión a base de datos
* configuración JWT

Las migraciones de Entity Framework Core deben aplicarse antes de utilizar la base de datos.

---

# Contribuciones

Antes de realizar cambios en el backend:

1. respetar la separación por capas
2. mantener las reglas de negocio en Domain/Application
3. evitar introducir lógica de negocio en los controladores
4. actualizar los tests cuando cambie una regla
5. mantener las migraciones sincronizadas con los cambios de persistencia
6. revisar permisos y concurrencia en operaciones que modifiquen recursos compartidos

---

# Licencia

Información sobre la licencia del proyecto.
