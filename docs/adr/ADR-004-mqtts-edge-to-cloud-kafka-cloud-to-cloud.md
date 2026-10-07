# ADR-004: MQTTS para Edge-to-Cloud y Apache Kafka para Cloud-to-Cloud

## Estado

Aceptada.

**Drivers:** QAS 03, QAS 06; AC02, AC03

## Contexto

El enlace con la mina es de ancho de banda restringido e inestable; entre servicios se necesita desacoplar productores y consumidores.

## Decisión

MQTT sobre TLS en tópicos `minesense/{vehicleId}/...`; Kafka para eventos de dominio, con `eventId` y `correlationId` para deduplicar y trazar.

## Alternativas consideradas

- HTTP/REST desde el Edge.
- RabbitMQ o AWS SQS/SNS entre servicios.

## Consecuencias

- (+) Protocolo liviano y consumo múltiple sin pérdida.
- (–) Se opera un broker adicional y los consumidores deben ser independientes.

## Notas

Tópicos conceptuales definidos en la sección 2.1.6: `minesense/{vehicleId}/telemetry`, `/device-status`, `/detections`, `/alerts`, `/sync`. Campos mínimos de cada evento: `eventId`, `eventType`, `occurredAt`, `source`, `correlationId`, `payload`.

---

Fuente: informe del equipo, sección 2.1.4.4 *Architecture Decision Records (ADR)*.
