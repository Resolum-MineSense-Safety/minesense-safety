# Reporting Service

Reporting Service implements EP10 (US22, TS-21). It keeps versioned report indicator definitions,
receives safety report generation requests as asynchronous jobs and exposes the metadata of the
generated file. Heavy PDF rendering is delegated to Lambda (ADR-008); this service only tracks the
job lifecycle.

## API routes

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/v1/report-definitions?onlyActive=` | List definitions ordered by code (`onlyActive` defaults to `true`). |
| `POST` | `/api/v1/report-definitions` | Define an indicator (201); the same code and version answers 409 with the existing one. |
| `POST` | `/api/v1/reports/safety` | Request a report generation job (201, status `Requested`). |
| `GET` | `/api/v1/reports` | List jobs by `siteId`, `fleetId`, `from`, `to`, `page`, `pageSize`, newest first. |
| `GET` | `/api/v1/reports/{reportJobId}` | Get a job. |
| `GET` | `/api/v1/reports/{reportJobId}/download` | `uri`, `checksum` and `expiresAt` of the generated file (404 if none). |

## Rules

- A definition needs a code, name, version and at least one data source (422). A new version of
  an existing code is stored as another active definition.
- A job needs `from` not after `to`, a `definitionCode` that exists (422 otherwise) and a format
  `Pdf` or `Csv`. `siteId` and `fleetId` are optional.
- Job statuses are `Requested`, `Rendering`, `Ready`, `Empty` and `Failed`; in this slice jobs
  stay `Requested` and no artifact is created yet, so the download route answers 404.
- The jobs list filters by request time and accepts `pageSize` from 1 to 100 (20 by default).

## Configuration

`appsettings.json` only sets logging. Locally the `http` launch profile listens on
`http://localhost:5270`, the same port published by Docker Compose.

## Verification

```powershell
dotnet test src/backend/tests/ReportingService.Tests/ReportingService.Tests.csproj
```

Persistence is in memory; the code comments assign MongoDB (ADR-005) to this context.
