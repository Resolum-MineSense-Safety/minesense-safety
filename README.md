# MineSense Safety - Monorepo

Integrated microservices and IoT system for the preventive detection and mitigation of fatigue in heavy machinery operators.

## Project Structure

- `.github/workflows/`: CI/CD pipelines as code using GitHub Actions.
- `src/backend/`: Microservices developed in C# with .NET 8.
- `src/backend/PrivacyAuditService/`: Privacy & Audit bounded context (EP09/EP10, scaffold).
- `src/backend/ReportingService/`: Reporting bounded context (EP10, scaffold).
- `src/backend/EdgeSynchronizationService/`: Recepción idempotente de lotes de eventos
  generados offline por los nodos edge (puerto local `5280`).
- `src/backend/FeedbackReliabilityService/`: Feedback & Reliability (EP12, US26–US28); [setup, contracts and testing](src/backend/FeedbackReliabilityService/README.md).
- `src/edge/`: Python cabin gateway agent: local fatigue classification and warnings, durable offline buffer and idempotent batch sync (US13, US14); [README](https://github.com/Resolum-MineSense-Safety/minesense-safety/blob/develop/src/edge/README.md).
- `src/frontend/`: Web-based monitoring dashboard built with React, Vite, and Tailwind CSS.
- `infrastructure/`: Provisioning code using Terraform and Kubernetes/Helm manifests.
- `docs/`: C4 architecture documentation and Architecture Decision Records (ADRs).
- `docker-compose.yml`: Local orchestration environment.

## Edge Synchronization Service

El servicio recibe lotes desde el agente de cabina y conserva el lote completo. El `batchId`
funciona como clave de idempotencia: el primer envío responde `202 Accepted` y un reintento
del mismo lote responde `200 OK` sin sobrescribir el contenido original.

Con `Kafka:BootstrapServers` configurado, un servicio en segundo plano publica el contenido de cada lote
nuevo en orden de captura y con las horas originales: las lecturas llegan al historial de Fatigue Detection
y las advertencias a Alerts, sin volver a advertir al operador. Si Kafka está caído, el lote queda
pendiente (`publicationPending`) y se reintenta. Sin Kafka solo se guarda. Las decisiones están en
[ADR-004](https://github.com/Resolum-MineSense-Safety/minesense-safety/blob/develop/docs/adr/ADR-004-mqtts-edge-to-cloud-kafka-cloud-to-cloud.md).

- `POST /api/v1/synchronization-batches`: registra un lote con entre 1 y 500 eventos.
- `GET /api/v1/synchronization-batches/{batchId}`: consulta el lote aceptado.
- Swagger local: `http://localhost:5280/swagger`.

Cada evento contiene `eventId`, `occurredAt`, `eventType` y un `payload` JSON libre.
Los lotes se guardan en MongoDB con `ConnectionStrings:MongoDb`, y en memoria si no está configurado.
