# Incident Management Service

Incident Management Service implements EP05 (US09, US10, US11, TS-16 to TS-19 and TS-27). It
receives the fatigue incidents created from alerts, lets one supervisor take each incident, records
the actions taken to protect the operator, escalates unattended critical incidents and closes them
with a documented resolution.

## API routes

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/v1/incidents` | US09: open an incident from an alert (201); a repeated `alertId` returns the same incident. |
| `GET` | `/api/v1/incidents?status=&operatorId=` | Incident tray, optionally by status (`Pending`, `Assigned`, `Closed`) and operator. |
| `GET` | `/api/v1/incidents/{incidentId}` | Incident detail with its actions and attention time. |
| `PATCH` | `/api/v1/incidents/{incidentId}/assignment` | TS-16: a supervisor (`assigneeId`) takes the incident. |
| `POST` | `/api/v1/incidents/{incidentId}/actions` | TS-17: record an action (201). |
| `PATCH` | `/api/v1/incidents/{incidentId}/closure` | TS-18: close with a `resolution` and optional `closedAt`. |
| `GET` | `/api/v1/incidents/{incidentId}/escalation` | TS-19: escalation level and notified roles. |
| `GET` | `/api/v1/incidents/{incidentId}/history` | TS-27: chronological history of the incident. |
| `POST` | `/api/v1/incidents/overdue-escalations` | Escalate every overdue critical incident now. |

## Rules

- An incident needs an operator and the alert that originated it; severity is `Warning` or
  `Critical`. It starts as `Pending`.
- Assignment: assigning the same supervisor again does nothing; a different supervisor, or a
  closed incident, answers 409.
- Actions: type `OperatorContact`, `PreventivePause`, `OperatorRelief` or `Other`, with a
  description, a result and who performed it (400 if missing). Actions cannot be recorded on a
  closed incident (409) nor dated before the incident was opened.
- Closure needs at least one recorded action (409) and a resolution. `closedAt` defaults to the
  server time and cannot be before the opening. The attention time is closure minus opening.
- Escalation (US11) applies only to `Critical` incidents that are still `Pending`. Every time two
  minutes pass from the opening or the last escalation, the level rises: level 1 notifies
  `ShiftManager`, level 2 adds `SafetyManager` (maximum). The escalation status is
  `NotApplicable`, `NotRequired` or `Escalated`.
- The history keeps the events `Opened`, `Assigned`, `ActionRecorded`, `Escalated` and `Closed`.

## Configuration

| Key | Default | Purpose |
|---|---|---|
| `IncidentEscalation:IntervalSeconds` | `15` | Period of the background worker that escalates overdue incidents. |

The key is read in code and is not present in `appsettings.json`. In Docker Compose the service
listens on port 5208.

## Verification

```powershell
dotnet test src/backend/tests/IncidentManagementService.Tests/IncidentManagementService.Tests.csproj
```

Persistence is in memory, like the other services of this slice.
