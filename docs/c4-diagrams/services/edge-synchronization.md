# Edge Synchronization Service

Servicio responsable de recibir eventos generados por los gateways Edge, conservarlos
temporalmente cuando la conectividad es intermitente y publicarlos hacia el broker MQTT
cuando se solicita una sincronización.

## Contrato REST

- `POST /api/v1/edge-sync/events`: agrega un evento al outbox. El `eventId` funciona
  como clave de idempotencia; repetirlo devuelve el evento existente.
- `POST /api/v1/edge-sync/sync?vehicleId={id}&batchSize={n}`: publica un lote de
  eventos pendientes en MQTT.
- `GET /api/v1/edge-sync/vehicles/{vehicleId}/events`: consulta el historial temporal
  del vehículo.

El cuerpo de un evento contiene `eventId`, `vehicleId`, `eventType`, `occurredAt` y
un `payload` JSON. Los tamaños de lote válidos son de 1 a 500.

## MQTT

- Suscripción de entrada: `minesense/+/sync`.
- Publicación de salida: `minesense/{vehicleId}/{eventType}`.
- QoS de publicación: at-least-once.

La implementación actual usa un repositorio en memoria, igual que los bounded contexts
existentes. El repositorio es una frontera explícita para sustituirlo posteriormente por
persistencia durable sin cambiar el contrato REST ni el dominio.
