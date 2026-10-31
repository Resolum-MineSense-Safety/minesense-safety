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

## Actualizado (2026-10-30): eventos Kafka implementados entre servicios cloud

La parte cloud-to-cloud de esta decisión ya está en el código. El tramo MQTT Edge-to-Cloud no cambia.

- **Shared kernel** (`MineSenseSafety.Shared`): sobre `IntegrationEvent` con `eventId`, `type`,
  `occurredAt`, `version`, `source`, `correlationId`, `aggregateId` y `payload` (JSON camelCase);
  `IIntegrationEventPublisher`, productor Kafka idempotente (`acks=all`, clave = id del agregado) y
  un consumidor por servicio (grupo `minesense-<servicio>`, commit manual después de procesar,
  deduplicación por `eventId`, tres intentos con espera creciente y descarte del mensaje venenoso).
  Se activa con `Kafka:BootstrapServers`; vacío, los eventos solo se registran en el log y no arranca
  ningún consumidor, así las pruebas y un `dotnet run` sin broker siguen funcionando.
- **Tópicos**: `minesense.fatigue.assessed`, `minesense.alerts.issued`, `minesense.alerts.acknowledged`,
  `minesense.alerts.escalated`, `minesense.incidents.opened`, `minesense.incidents.closed` y
  `minesense.fleet.operator-updated`.

| Servicio | Publica | Consume |
|---|---|---|
| Fatigue Detection | `FatigueAssessed` (clave = operador, para ordenar sus riesgos) | — |
| Alerts | `AlertIssued`, `AlertAcknowledged`, `AlertEscalated` | `FatigueAssessed`: emite la alerta si `requiresAlert` (una por evaluación) |
| Fleet Monitoring | `FleetOperatorUpdated` (turno, flota, ubicación, unidad minera, riesgo) | `FatigueAssessed`: actualiza el riesgo del operador |
| Incident Management | `IncidentOpened`, `IncidentClosed` | `AlertIssued`: abre incidente solo si es `Critical` (uno por alerta) |
| Analytics | — | `FleetOperatorUpdated` (copia local del contexto) y `AlertIssued`: registra la observación |
| Feedback & Reliability | — | `FleetOperatorUpdated` (operador → operación) y `AlertIssued`: habilita el feedback |
| Privacy & Audit | — | `IncidentOpened`, `IncidentClosed`: pasos de la traza del incidente |

- Los consumidores no tienen token de usuario: llaman a los servicios de aplicación de su propio
  bounded context dentro del proceso, nunca a la API REST de otro servicio.
- Analytics guarda como sitio el id de la unidad minera que reporta Fleet Monitoring; si el operador
  aún no tiene contexto, la observación se registra con `unknown`. Feedback no registra la alerta de
  un operador sin unidad minera conocida, porque la operación es obligatoria.
- Infraestructura: `docker-compose.yml` levanta Kafka en modo KRaft (sin ZooKeeper); en EKS los
  servicios usan MSK Serverless con IAM (`Kafka__Authentication=AwsMskIam`) y `deploy.sh` toma los
  brokers de la salida Terraform `kafka_bootstrap_brokers`.
- Ids procesados: con `ConnectionStrings:MongoDb` cada servicio documental los guarda en la colección
  `processed_events` de su base (índice TTL de 30 días, ADR-005); sin MongoDB siguen en memoria.
- Pendiente: outbox transaccional y el permiso IAM `kafka-cluster:*` para el rol de cada pod.

---

Fuente: informe del equipo, sección 2.1.4.4 *Architecture Decision Records (ADR)*.
