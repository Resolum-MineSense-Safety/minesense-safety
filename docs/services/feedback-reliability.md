# Feedback & Reliability Service

Feedback & Reliability implements EP12 (US26, US27, US28, TS-20, TS-28). Operators and supervisors
classify alerts as confirmed, false alarm or inconclusive, supervisors review those reports, and
the service publishes reliability metrics per operation. The full contract, local setup and
MongoDB tests are described in the
[service README](https://github.com/Resolum-MineSense-Safety/minesense-safety/blob/develop/src/backend/FeedbackReliabilityService/README.md).

## API routes

All business routes require `Authorization: Bearer <JWT>`.

| Method | Route | Access | Purpose |
|---|---|---|---|
| `PUT` | `/api/v1/feedback-alerts/{alertId}` | Service | Register the alert with its `operationId`, verified against the Alert Service (idempotent). |
| `GET` | `/api/v1/feedback-alerts?operationId=&from=&to=` | Supervisor, Manager | Paged review queue; optional `status`, `page`, `pageSize`. |
| `POST` | `/api/v1/alerts/{alertId}/feedback` | Owner Operator, Supervisor | US26 / TS-20: report a classification (201). |
| `GET` | `/api/v1/alerts/{alertId}/feedback` | Owner Operator, Supervisor, Manager, Service | Alert context with the report and review history. |
| `POST` | `/api/v1/alerts/{alertId}/feedback/reviews` | Supervisor | US27: append a supervisor review. |
| `GET` | `/api/v1/analytics/alert-reliability?operationId=&from=&to=` | Supervisor, Manager | US28 / TS-28: reliability metric. |

## Rules

- Classifications are `CONFIRMED`, `FALSE_ALARM` and `INCONCLUSIVE`. A false alarm needs a reason
  and every review needs an observation (1 to 1000 characters).
- Each user can report an alert once; once an alert is reviewed no new reports are accepted.
  Reviews are append-only and the last one is authoritative; before a review, the first report
  is the provisional classification.
- Status: no report → `PENDING`, reported → `AWAITING_REVIEW`, reviewed → `REVIEWED`.
- Queue and metrics need a valid `operationId` and a `[from, to)` period of at most 366 days on the
  alert issuance time; `pageSize` is 1 to 100 (20 by default). Invalid values answer 400.
- Each alert counts once in the metric; proportions are 0 to 1 and `insufficientSample` is true
  below the minimum sample size.
- Concurrent writes are version-checked and answer 409; an unavailable Alert Service answers 503.

## Configuration

| Key | Default | Purpose |
|---|---|---|
| `Authentication:Authority` | none | HTTPS OIDC issuer used to validate tokens. |
| `Authentication:Audience` | `minesense-feedback` | Expected token audience. |
| `Services:AlertServiceUrl` | `http://localhost:5233/` | Base URL of the Alert Service (5-second timeout). |
| `ConnectionStrings:MongoDb` | none | Required outside Development/Testing; without it those two environments use memory. |
| `MongoDb:Database` | `minesense_feedback` | MongoDB database. |
| `Reliability:MinimumSampleSize` | `30` | Positive integer; provisional display threshold, not a statistical guarantee. |

In Docker Compose the service is published on port 5281, uses the `mongodb` container and reaches
the Alert Service through `FEEDBACK_ALERT_SERVICE_URL`. `/health/live` is anonymous.

## Verification

```powershell
dotnet test src/backend/tests/FeedbackReliabilityService.Tests/FeedbackReliabilityService.Tests.csproj
```

The real MongoDB integration test only runs when `FEEDBACK_TEST_MONGODB` is set.
