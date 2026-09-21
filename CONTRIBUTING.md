# CONTRIBUTING.md

## Objetivo
Mantener consistencia funcional y técnica en la API de LogiMatch.

## Guías generales
- Seguir la arquitectura y convenciones ya existentes en la solución.
- Mantener los `Controllers` ligeros y delegar la lógica en `Handlers` de aplicación.
- Priorizar respuestas pensadas para consumo directo desde el front cuando el caso de uso sea de pantalla, bandeja o detalle.

## Convenciones de API	
- Usar rutas REST consistentes y nombres alineados con los recursos del dominio.
- Para operaciones de lectura, preferir filtros opcionales por `query string` antes que proliferar rutas específicas.
- Devolver `404 NotFound` cuando el recurso solicitado no exista.
- Mantener respuestas homogéneas entre detalle y listado, reduciendo campos solo cuando aporte valor claro a rendimiento o UX.
- Diseñar respuestas de lectura con estructura clara y estable para el front, evitando obligarle a recomponer información básica a partir de múltiples IDs cuando pueda entregarse directamente en la respuesta.
- Mantener un control de errores consistente entre endpoints, con respuestas previsibles para validaciones, recursos inexistentes y operaciones no permitidas por estado.

## Requisitos para endpoints orientados al front
- En endpoints orientados al front, añadir paginación en listados y búsquedas donde el volumen pueda crecer o la UI consuma colecciones.
- Aplicar especialmente a recursos como `transport-requests`, `trips`, `bookings`, `transport-offers`, `vehicles`, `trip-cargos`, `users` y `companies`.
- Evitar traer tablas completas cuando el escenario sea de pantalla, bandeja o listado navegable.
- Diseñar los filtros para que combinen bien con paginación y ordenación.
- Mantener contratos simples para el front, evitando obligarle a resolver relaciones básicas solo con IDs cuando pueda devolverse información útil en la respuesta.
- En listados orientados al front, devolver una estructura paginada consistente, incluyendo al menos `items`, `page`, `pageSize`, `totalItems` y `totalPages`.
- Asegurar que las respuestas de error sean adecuadas para el front, con mensajes claros y formato homogéneo para facilitar su tratamiento en la UI.

## Estilo de implementación
- Preferir métodos de lectura claros y específicos en los `Handlers`.
- Mantener nombres explícitos como `Handle`, `HandleAll` o equivalentes consistentes con el patrón ya existente.
- Ordenar los listados de forma determinista.

## Validación
- Compilar tras cada bloque de cambios.
- Revisar conflictos de rutas y compatibilidad de firma al ampliar `Controllers` existentes.
- Verificar que filtros, paginación y serialización cubren los casos de uso del front.
- Verificar que los casos de error relevantes para el front quedan cubiertos y responden de forma consistente.