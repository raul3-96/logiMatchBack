# LogiMatch

## Descripción del proyecto

`LogiMatch` es una plataforma logística para gestionar el ciclo completo de transporte: alta de usuarios y transportistas, creación de solicitudes, publicación de cargas, generación de ofertas, reservas, viajes y seguimiento operativo hasta la finalización.

## Arquitectura

La solución está organizada en capas:

- `LogiMatch.Api`: exposición HTTP mediante controladores REST.
- `LogiMatch.Application`: casos de uso y lógica de negocio.
- `LogiMatch.Domain`: entidades y reglas del negocio.
- `LogiMatch.Infrastructure`: persistencia con EF Core y PostgreSQL.

## Exposición de la API

La API se expone mediante controladores bajo el prefijo `api/`.  
Los controladores reciben peticiones HTTP y delegan la lógica en la capa `LogiMatch.Application`.

### Responsabilidad de los controladores

Los controladores se encargan de:

- recibir peticiones HTTP
- mapear datos de entrada
- invocar handlers de aplicación
- devolver respuestas HTTP coherentes
- propagar errores de negocio cuando una validación no se cumple

## Endpoints de la API

### Usuarios
- `POST /api/users`
  - Crea un usuario.

### Empresas
- `POST /api/companies`
  - Crea una empresa.

### Ubicaciones
- `POST /api/locations`
  - Crea una ubicación.

### Perfiles de transportista
- `POST /api/transporter-profiles`
  - Crea un perfil de transportista.

### Vehículos
- `POST /api/vehicles`
  - Crea un vehículo.

### Disponibilidad de vehículos
- `POST /api/vehicle-availabilities`
  - Registra una ventana de disponibilidad para un vehículo.

### Cargas
- `POST /api/cargos`
  - Añade una carga a una solicitud de transporte.

### Solicitudes de transporte
- `POST /api/transport-requests`
  - Crea una solicitud de transporte.
- `GET /api/transport-requests/{id}`
  - Consulta el detalle de una solicitud.
- `POST /api/transport-requests/{id}/publish`
  - Publica una solicitud.
- `POST /api/transport-requests/{id}/cancel`
  - Cancela una solicitud.

### Ofertas de transporte
- `POST /api/transport-offers`
  - Crea una oferta para una solicitud.
- `GET /api/transport-offers/request/{transportRequestId}`
  - Lista las ofertas de una solicitud.
- `POST /api/transport-offers/{offerId}/accept`
  - Acepta una oferta y genera un booking.

### Bookings
- `GET /api/bookings/{id}`
  - Consulta el detalle de una reserva.
- `POST /api/bookings/{id}/start`
  - Inicia una reserva.
- `POST /api/bookings/{id}/complete`
  - Completa una reserva.
- `POST /api/bookings/{id}/cancel`
  - Cancela una reserva.

### Trips
- `POST /api/trips`
  - Crea un viaje.
- `GET /api/trips/{id}`
  - Consulta el detalle de un viaje.
- `POST /api/trips/{id}/start`
  - Inicia un viaje.
- `POST /api/trips/{id}/complete`
  - Completa un viaje.
- `POST /api/trips/{id}/cancel`
  - Cancela un viaje.

### Matching
- `POST /api/matching/vehicles`
  - Busca vehículos compatibles con una solicitud.

## Flujos normales de uso

### Flujo 1: alta básica de datos
1. Crear `User`.
2. Crear `Company` si aplica.
3. Crear `Location` para origen y destino.
4. Crear `TransporterProfile`.
5. Crear `Vehicle`.
6. Crear `VehicleAvailability`.

### Flujo 2: solicitud con oferta
1. Crear `TransportRequest`.
2. Añadir una o varias `Cargo`.
3. Publicar la solicitud con `POST /api/transport-requests/{id}/publish`.
4. Un transportista crea una `TransportOffer`.
5. El cliente consulta las ofertas.
6. El cliente acepta una oferta.
7. Se genera un `Booking`.
8. El booking puede iniciarse, completarse o cancelarse según su estado.

### Flujo 3: solicitud asignada a un viaje
1. Crear `TransportRequest`.
2. Añadir `Cargo`.
3. Publicar la solicitud.
4. Crear un `Trip`.
5. Buscar vehículos compatibles con `POST /api/matching/vehicles`.
6. Reservar capacidad sobre el viaje mediante `TripCargo`.
7. Iniciar el viaje.
8. Iniciar las cargas asociadas.
9. Completar las cargas.
10. Completar el viaje.

- `UsersController`: gestión de usuarios.
- `CompaniesController`: gestión de empresas.
- `LocationsController`: alta y consulta de ubicaciones.
- `TransportRequestsController`: creación, publicación, consulta y cancelación de solicitudes de transporte.
- `CargosController`: creación de mercancías asociadas a una solicitud.
- `TransporterProfilesController`: gestión de perfiles de transportista.
- `VehiclesController`: alta y consulta de vehículos.
- `VehicleAvailabilitiesController`: disponibilidad temporal de vehículos.
- `TransportOffersController`: creación, consulta y aceptación de ofertas.
- `BookingsController`: consulta y ciclo de vida de reservas.
- `TripsController`: creación, consulta y ciclo de vida de viajes.
- `MatchingController`: búsqueda de vehículos y viajes compatibles.
- `TripCargoController` o equivalente de matching: reserva, inicio, completado y cancelación de cargas asociadas a viajes.

### Flujo 4: cancelaciones
- Una solicitud puede cancelarse solo si su estado lo permite.
- Un booking solo puede cancelarse cuando está confirmado.
- Un trip solo puede cancelarse cuando está publicado.
- Una carga de viaje solo puede cancelarse cuando el viaje sigue publicado.

## Capacidades de negocio

La aplicación permite:

- registrar usuarios y empresas
- dar de alta transportistas y vehículos
- definir ubicaciones de recogida y entrega
- crear solicitudes de transporte con varias cargas
- publicar solicitudes para que puedan ser cubiertas
- crear ofertas de transporte con precio, vehículo y fechas estimadas
- aceptar ofertas y generar un booking
- crear viajes de transporte y asignar solicitudes a esos viajes
- reservar capacidad de un viaje para una solicitud concreta
- iniciar, completar o cancelar bookings, viajes y cargas según su estado
- buscar automáticamente vehículos compatibles con una solicitud

## Reglas importantes

La API aplica validaciones estrictas de estado. Algunos ejemplos:

- una carga de viaje solo puede completarse si está en progreso
- una solicitud solo puede publicarse si tiene al menos una carga
- una oferta solo puede aceptarse si está pendiente
- un booking solo puede iniciarse si está confirmado
- un trip solo puede completarse si está en progreso y no tiene carga activa

## Uso recomendado

Para probar la API:

- usar Swagger en desarrollo
- invocar los endpoints con Postman o cualquier cliente HTTP
- respetar el orden lógico de creación de entidades

## Resumen para cliente

`LogiMatch` centraliza todo el proceso de transporte: publicación de solicitudes, oferta de transporte, reserva, matching y seguimiento operativo hasta la finalización del servicio.

## Instalación

Instrucciones sobre cómo instalar y configurar el proyecto.

## Contribuciones

Guía para contribuir al proyecto.

## Licencia

Información sobre la licencia del proyecto.