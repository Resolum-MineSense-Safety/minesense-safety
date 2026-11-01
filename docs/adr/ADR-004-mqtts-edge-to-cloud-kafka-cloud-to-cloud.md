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

## Actualizado (2026-11-01): los lotes del Edge llegan al resto del sistema (US14)

Lo que el agente de cabina sincroniza por `POST /api/v1/synchronization-batches` ya no se queda solo
guardado. El tramo cabina-nube sigue siendo HTTP con lotes idempotentes; lo nuevo es el reenvío por Kafka.

**Publicación (Edge Synchronization).**

- Tópicos: `minesense.edge.fatigue-readings` (`FatigueReadingSynchronized`) y
  `minesense.edge.cabin-warnings` (`CabinWarningSynchronized`). Otros tipos de evento, o un `payload`
  que no sea un objeto JSON, quedan guardados en el lote pero no se publican.
- Sigue sin haber outbox transaccional general, así que el lote hace de outbox: `PublicationPending`
  se guarda en el mismo documento que el lote (escritura atómica) y un servicio en segundo plano publica
  los pendientes, del más antiguo al más nuevo. El lote deja de estar pendiente solo cuando el broker
  confirmó todos sus eventos. Por eso existe `IConfirmedIntegrationEventPublisher`: el `PublishAsync` de
  siempre solo registra en el log los fallos de entrega y, con él, Kafka caído perdería eventos.
- Orden: la clave es el dispositivo, el envío espera cada confirmación y el primer fallo corta la pasada,
  de modo que un lote posterior nunca adelanta a uno anterior. El orden se garantiza por dispositivo
  dentro de cada tópico; ningún consumidor necesita el orden entre los dos tópicos, porque cada servicio
  lee uno solo y la advertencia nombra su lectura.
- Tiempos: el `occurredAt` del sobre y el `capturedAt` del payload son el momento del evento en cabina,
  no el sellado (`CapturedAt` del lote) ni la llegada (`ReceivedAt`).
- Sin duplicados: un reintento del mismo lote no vuelve a publicar nada. `eventId` se deriva de
  `batchId` + posición (UUID con nombre sobre SHA-256), así que republicar un lote tras un fallo o un
  reinicio se descarta en `processed_events`. Si ese registro ya expiró, lo absorbe la clave de negocio.
- Sin `Kafka:BootstrapServers` no corre el publicador y todo funciona como antes. Los lotes quedan
  pendientes y salen cuando se active Kafka. Los lotes guardados antes de este cambio no tienen la marca
  y no se publican.

**Lecturas (Fatigue Detection).** `FatigueReadingSynchronized` se guarda como evaluación con
`Origin = Edge`, `AssessedAt` = hora de captura y `SyncedAt` = llegada. Su id es el `eventId` de la lectura
en el Edge, que es la clave de idempotencia. **No publica `FatigueAssessed`**: ese evento significa "este es
el riesgo del operador ahora"; con él Alerts volvería a advertir y Fleet Monitoring pondría como actual un
riesgo de hace horas.

**Advertencias (Alerts).** `CabinWarningSynchronized` se guarda como alerta con `Origin = Edge`, `IssuedAt` =
hora de la advertencia en cabina y el estado nuevo `RecordedFromEdge`:

- No se le pide confirmación al operador y no vence: la cabina ya advirtió, y como `Issued` con horas de
  antigüedad se escalaría de inmediato al supervisor por un plazo que nunca pudo cumplirse.
- Una lectura produce como máximo una alerta. `AssessmentId` es el id de la lectura (`readingEventId` en
  el payload de la advertencia), igual al id de su evaluación, y la regla existente y el índice único
  impiden una segunda alerta aunque lleguen la advertencia y un `FatigueAssessed` de la misma lectura.

**Lo que sí sigue el flujo normal.** La alerta publica `AlertIssued` con la hora original, porque el hecho
es cierto aunque sea pasado:

| Consumidor | Decisión para un evento pasado | Motivo |
|---|---|---|
| Incident Management | Una advertencia `Critical` abre incidente | Durante el corte nadie en el centro de control supo del posible microsueño, y el supervisor debe revisarlo aunque sea tarde. El incidente va al centro de control; nada vuelve al operador. |
| Incident Management | `OpenedAt` = recepción, y la hora de la cabina queda en el historial del incidente | Los tiempos de atención y de escalamiento miden al centro de control, que no pudo actuar antes. Con la hora original se escalaría al instante. El escalamiento sigue activo si nadie atiende. |
| Analytics | Registra la observación con la hora original | Las tendencias deben reflejar cuándo ocurrió. El contexto (turno, ubicación) es el que se conoce al llegar, no necesariamente el de ese momento. |
| Feedback & Reliability | Habilita el feedback con la hora original | El operador o el supervisor aún pueden valorar la advertencia. |

Los paneles de Alertas e historial de fatiga leen esos servicios y muestran estos registros con su hora
original; Alertas tiene una pestaña propia para `RecordedFromEdge`.

**Pendiente.**

- Fleet Monitoring no recibe estas lecturas, así que el riesgo actual del panel de flota no cambia con
  ellas. Una cabina real conectada también envía todo por lotes, de modo que hace falta una regla de "gana
  el más reciente" (por hora de captura) para que ese riesgo refleje a esas cabinas.
- Con varias réplicas del servicio, cada una publica los pendientes: los consumidores descartan las
  repeticiones por `eventId`, pero se envían de más. Un bloqueo por lote lo evitaría.
- Los consumidores conservan el comportamiento de #72: tras tres intentos fallidos un evento se descarta.
  Si un consumidor falla así, el evento sigue guardado en su lote, pero aún no hay forma de reenviarlo.
- No se probó contra un broker real.

---

Fuente: informe del equipo, sección 2.1.4.4 *Architecture Decision Records (ADR)*.
