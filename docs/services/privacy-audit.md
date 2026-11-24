# Privacy & Audit Service

Privacy & Audit Service implements EP09 and part of EP10 (US20, US21, US22, US23, TS-21). It keeps
the catalog of data-processing purposes, the consent decisions of each operator, a tamper-evident
log of every access to sensitive data and the requests for safety compliance reports.

## API routes

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/v1/processing-purposes` | US20: current policy version and its purposes. |
| `POST` | `/api/v1/consents` | US20: register a consent decision (201, or 200 if an identical decision already exists). |
| `GET` | `/api/v1/consents/operator/{operatorId}/current` | Latest decision of the operator (404 if none). |
| `GET` | `/api/v1/consents/operator/{operatorId}/history` | Every decision, newest first. |
| `POST` | `/api/v1/access-log/entries` | US21: append an access attempt (201). |
| `GET` | `/api/v1/access-log` | US21: query the log by `operatorId`, `userId`, `resourceType`, `from`, `to`, `decision`, `page`, `pageSize`. |
| `GET` | `/api/v1/incidents/{incidentId}/history` | US23: access log entries of an incident in chronological order. |
| `POST` | `/api/v1/reports/safety` | US22 / TS-21: request a compliance report (201, status `Requested`). |
| `GET` | `/api/v1/reports/{reportId}` | Get a report request. |

The last three routes repeat paths of Incident Management (`/api/v1/incidents/{id}/history`) and
Reporting (`/api/v1/reports/safety`, `/api/v1/reports/{id}`). The gateway sends `/api/v1/incidents/`
and `/api/v1/reports/` to those services, so here they are only reachable on this service's port.

## Rules

- Policy catalog (`ProcessingPolicyCatalog`): version `v1.0` with the purposes
  `fatigue-monitoring` and `safety-alerts` (mandatory) and `operational-analytics` (optional).
  An unknown version or purpose answers 422.
- Consent status is `Accepted`, `Declined` or `Revoked`. A decision blocks monitoring
  (`blocksMonitoring`) when it is declined or revoked, or when a mandatory purpose is missing.
- The access log is append-only. Each entry stores user, resource type and id, fields, purpose,
  decision (`Granted` or `Denied`) and server time, and is sealed with a SHA-256 hash of its data
  plus the previous entry's hash (`GENESIS` for the first one).
- In the log query, `operatorId` matches the entry's `resourceId`; results are newest first and
  `pageSize` is 1 to 100 (20 by default).
- A report request needs `from` not after `to`; `siteId`, `fleetId` and `requestedBy` are
  optional. Report generation is not implemented in this slice.

## Configuration

`appsettings.json` only sets logging. Locally the `http` launch profile listens on
`http://localhost:5260`, the same port published by Docker Compose.

## Verification

```powershell
dotnet test src/backend/tests/PrivacyAuditService.Tests/PrivacyAuditService.Tests.csproj
```

Persistence is in memory; the code comments assign MongoDB (ADR-005) to this context.
