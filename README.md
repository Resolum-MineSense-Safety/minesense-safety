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
- `src/edge/`: Local cabin processing and inference agent developed in Python.
- `src/frontend/`: Web-based monitoring dashboard built with React, Vite, and Tailwind CSS.
- `infrastructure/`: Provisioning code using Terraform and Kubernetes/Helm manifests.
- `docs/`: C4 architecture documentation and Architecture Decision Records (ADRs).
- `docker-compose.yml`: Local orchestration environment.

## Edge Synchronization Service

El servicio recibe lotes desde el agente de cabina y conserva el lote completo para
su posterior procesamiento por los bounded contexts analíticos. El `batchId` funciona
como clave de idempotencia: el primer envío responde `202 Accepted` y un reintento
del mismo lote responde `200 OK` sin sobrescribir el contenido original.

- `POST /api/v1/synchronization-batches`: registra un lote con entre 1 y 500 eventos.
- `GET /api/v1/synchronization-batches/{batchId}`: consulta el lote aceptado.
- Swagger local: `http://localhost:5280/swagger`.

Cada evento contiene `eventId`, `occurredAt`, `eventType` y un `payload` JSON libre.
La persistencia actual es en memoria, igual que los servicios que aún están en fase
de scaffold; el repositorio queda aislado detrás de `ISynchronizationBatchRepository`
para sustituirlo por PostgreSQL/MongoDB sin cambiar el contrato REST.
