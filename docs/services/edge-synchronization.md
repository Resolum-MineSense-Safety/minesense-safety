# Edge Synchronization Service

Edge Synchronization Service implements EP07. It receives the batches of events that the cabin
gateways (edge nodes) captured while offline and keeps each batch complete for later processing
by the analytical bounded contexts.

## API routes

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/v1/synchronization-batches` | Submit a batch: 202 the first time, 200 on a retry of the same `batchId`. |
| `GET` | `/api/v1/synchronization-batches/{batchId}` | Get an accepted batch. |

## Rules

- `batchId` is the idempotency key. A retry returns the batch stored the first time and never
  overwrites it, even if the new body is different.
- `batchId`, `edgeNodeId` and `deviceId` are required; `sequenceNumber` cannot be negative.
- A batch carries between 1 and 500 events. Each event has a unique `eventId` inside the batch,
  `occurredAt`, a non-empty `eventType` and a free JSON `payload`.
- An accepted batch records `receivedAt` (server time) and the status `Accepted`, the only
  status implemented.

## Configuration

`appsettings.json` only sets logging. Locally the `http` launch profile listens on
`http://localhost:5280` (Swagger at `/swagger`); in Docker Compose it is published on port 5280.

## Verification

```powershell
dotnet test src/backend/tests/EdgeSynchronizationService.Tests/EdgeSynchronizationService.Tests.csproj
```

The repository is in memory behind `ISynchronizationBatchRepository`, so PostgreSQL or MongoDB can
replace it without changing the REST contract.
