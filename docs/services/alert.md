# Alert Service

Alert Service implements EP03. It issues the preventive warning shown in the cabin after a fatigue
assessment, waits for the operator to acknowledge it and escalates it to the supervisor when the
acknowledgement does not arrive in time.

## Rules

- An alert must belong to an operator and references the fatigue assessment that triggered it.
  Severity is `Warning` or `Critical` (case-insensitive); an unknown value answers 422.
- Status: a new alert is `Issued`; it becomes `Acknowledged` or `Escalated`. Only an `Issued`
  alert can be acknowledged or escalated; any other transition answers 422.
- Acknowledgement deadline (`AlertAcknowledgementPolicy`): 30 seconds for `Critical`, 2 minutes
  for `Warning`. An issued alert is overdue once that time has passed since `issuedAt`.
- There is no background timer in this service: overdue alerts are escalated when
  `POST /api/v1/alerts/overdue-escalations` is called.

## API routes

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/v1/alerts` | Issue an alert (201) with `operatorId`, `assessmentId` and `severity`. |
| `GET` | `/api/v1/alerts?operatorId=` | Alerts of an operator, newest first. |
| `GET` | `/api/v1/alerts/{alertId}` | One alert. |
| `GET` | `/api/v1/alerts/status/{status}` | Supervisor queue by status (`Issued`, `Acknowledged`, `Escalated`), oldest first. |
| `POST` | `/api/v1/alerts/{alertId}/acknowledgements` | The operator acknowledges the alert. |
| `POST` | `/api/v1/alerts/{alertId}/escalations` | Escalate one issued alert to the supervisor. |
| `POST` | `/api/v1/alerts/overdue-escalations` | Escalate every issued alert whose deadline has passed and return them. |

## Configuration

The service has no business configuration keys; `appsettings.json` only sets logging. In Docker
Compose it listens on port 5206. Feedback & Reliability reads `GET /api/v1/alerts/{alertId}` to
verify the alerts it tracks.

## Verification

```powershell
dotnet test src/backend/tests/AlertService.Tests/AlertService.Tests.csproj
```

Persistence is in memory, like the other services of this slice.
