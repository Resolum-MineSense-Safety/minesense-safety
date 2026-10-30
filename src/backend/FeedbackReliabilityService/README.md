# Feedback and Reliability Service

EP12 implements US26 (operator feedback), US27 (supervisor review), US28 (reliability metrics), TS-20 and TS-28 from the MineSense Safety report. Cloud service: .NET 8, port **5281** (same host port as `docker-compose.yml`), MongoDB database `minesense_feedback`, collection `alert_evaluations`.

## Responsibilities and architecture

`Domain` owns evaluation, duplicate-report and review rules. `Application` orchestrates commands/queries through repository, current-actor and Alerts ports. `Infrastructure` implements MongoDB, a development/test in-memory store, JWT claims and an HTTP anti-corruption adapter. `Interfaces/REST` owns request validation, resources and assemblers. The only project reference is the shared kernel; Alerts data is obtained through REST, never its database or domain classes.

Operator feedback and successive supervisor reviews are append-only. Each `(alertId, reportedBy)` can be submitted once. A false-alarm report needs a reason; every supervisor review needs an observation. Unclassified alerts remain `PENDING`; reported alerts are `AWAITING_REVIEW`, and reviewed alerts are `REVIEWED`. New reports after a review are rejected, while later supervisor reviews are retained as corrections with full history.

Before review, the first submitted report is the provisional classification. The last supervisor review is authoritative afterwards. Each alert contributes **once** to metrics, irrespective of the number of reporters or reviews. A second reporter cannot overwrite the first report; a supervisor resolves disagreements through the review endpoint. These resolution rules are explicit implementation choices where the report does not define precedence.

## Integration contract

Alerts currently exposes `GET /api/v1/alerts/{alertId}` with `id`, `operatorId`, `issuedAt` (and other fields this service ignores), but no operational scope. Therefore a trusted integration with role `Service` must register the alert using `PUT /api/v1/feedback-alerts/{alertId}` and its verified `operationId`. Feedback independently checks alert existence, operator and issuance time against Alerts. Duplicate identical registrations are idempotent; conflicting context returns 409. AlertService timeouts, invalid responses and errors produce 503; a missing alert produces 404. User input never creates an alert or assigns its ownership.

Registration is required for every alert, including unclassified ones, so the review queue can show pending evaluations. The integration caller must derive operation membership from Operations. A service token is privileged and must not be given to frontend clients. Registering existing alerts and wiring future alert-creation notifications are deployment integration responsibilities; this PR does not modify the other teams' services or assume a Kafka event schema that they have not implemented. There is no background polling or Kafka publisher in this service.

Identity & Access in the current develop is still a scaffold. This service includes real JWT bearer validation and role/operation authorization; **it does not implement identity issuance or accept development headers as authentication**. Configure an HTTPS OIDC authority that issues:

| Claim | Contract |
|---|---|
| `sub` | User/service GUID, matching `reportedBy` or `reviewedBy` for writes |
| `role` | Case-sensitive `Operator`, `Supervisor`, `Manager` or `Service` |
| `operation_id` | One GUID claim per authorized operation |
| `aud` | `minesense-feedback` |

An operator can submit/read only their own alerts. Supervisors can submit feedback and review alerts within their operations. Supervisors and managers can read the queue and metrics within their operations. Only service actors register alert context. Without a configured issuer and valid token, protected routes reject requests. The health route is anonymous. HTTPS termination is expected at the ingress; local HTTP is for development only.

For manual local development, obtain tokens from the configured OIDC issuer; do not commit keys or weaken validation. Automated tests run independently of Identity, configure their own test-only issuer/key and exercise real signature validation.

## REST API

All business routes require `Authorization: Bearer <JWT>`. Swagger at `/swagger` (Development) includes the Bearer authorization field.

| Method and route | Permission | Result |
|---|---|---|
| `PUT /api/v1/feedback-alerts/{alertId}` | Service | 200 verified alert context, idempotent |
| `POST /api/v1/alerts/{alertId}/feedback` | Owner Operator / Supervisor | 201 evaluation and Location header |
| `GET /api/v1/alerts/{alertId}/feedback` | Owner Operator / Supervisor / Manager / Service | 200 context and report/review history |
| `POST /api/v1/alerts/{alertId}/feedback/reviews` | Supervisor | 200 updated evaluation |
| `GET /api/v1/feedback-alerts` | Supervisor / Manager | 200 paged queue |
| `GET /api/v1/analytics/alert-reliability` | Supervisor / Manager | 200 reliability metric |

Both list and metrics require `operationId`, `from`, `to` (ISO 8601 timestamps with offset). Period means **alert issuance time**, inclusive `from` and exclusive `to`, with a maximum of 366 days. Compare operations by requesting each authorized operation with the same period and threshold. Invalid/missing period returns 400. List supports `status=PENDING|AWAITING_REVIEW|REVIEWED`, `page` (1–1000000), `pageSize` (1–100, default 20), ordered newest alert first and then ID for stable pagination.

Registration body:

```json
{ "operationId": "11111111-1111-4111-8111-111111111111" }
```

Feedback body (the GUID must match the signed token subject):

```json
{
  "classification": "FALSE_ALARM",
  "reason": "The operator reports no fatigue; dust obscured the camera.",
  "reportedBy": "22222222-2222-4222-8222-222222222222"
}
```

Review body:

```json
{
  "classification": "INCONCLUSIVE",
  "observation": "Insufficient evidence to confirm the operator report.",
  "reviewedBy": "33333333-3333-4333-8333-333333333333"
}
```

Only exact `CONFIRMED`, `FALSE_ALARM`, `INCONCLUSIVE` are accepted. JSON missing value-type properties and invalid classification return 400; missing business information (such as a false-alarm reason) returns 422; duplicate evaluations, conflicting alert registrations and concurrent writes return 409. Reload after a concurrent-write conflict before retrying. Authentication/authorization failures return 401/403, missing tracked alerts 404, and unavailable Alerts 503.

Metrics expose counts (`totalAlerts`, `pendingAlerts`, `sampleSize`, `confirmed`, `falseAlarms`, `inconclusive`) and proportions **0–1**, not percentages. `sampleSize` counts evaluated alerts only; pending alerts are excluded from its denominator. Empty samples return zero counts/proportions. `minimumSampleSize` defaults to **30** and is configurable via `Reliability:MinimumSampleSize`. The report does not specify a number: 30 is a provisional operational display threshold, **not a calibrated statistical confidence guarantee**. `insufficientSample` is true below that threshold, including zero. The result measures human-reported agreement, not clinical detection accuracy.

## Run locally

From the repository root:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Authentication__Authority = 'https://your-configured-identity-authority'
$env:ConnectionStrings__MongoDb = 'mongodb://localhost:27017'
dotnet run --project src/backend/FeedbackReliabilityService --launch-profile http
```

Start MongoDB with `docker compose up -d mongodb`. Start AlertService on port 5233 separately. For the container, configure `FEEDBACK_AUTHORITY` and optionally `FEEDBACK_ALERT_SERVICE_URL`, then `docker compose up --build feedback-reliability-service`. Defaults use the host's AlertService through `host.docker.internal:5233`. The new service does not run or change other microservices automatically.

| Setting | Default / behavior |
|---|---|
| `Authentication:Authority` | Required for validating real issuer tokens; HTTPS metadata only |
| `Authentication:Audience` | `minesense-feedback` |
| `Services:AlertServiceUrl` | `http://localhost:5233/`, 5-second timeout |
| `ConnectionStrings:MongoDb` | MongoDB URI; required outside Development/Testing |
| `MongoDb:Database` | `minesense_feedback` |
| `Reliability:MinimumSampleSize` | 30, positive integer |

Only Development/Testing permit in-memory storage when MongoDB is absent. That mode loses data on restart; production fails startup instead of silently falling back. MongoDB initialization creates the operation/time index; `_id` uniquely identifies the original alert. Version-checked atomic replacement prevents duplicate reports and lost updates across instances. Identity fields, query index fields, version and a serialized evaluation snapshot are stored in each document; no cross-service foreign keys. MongoDB at rest uses the deployment's storage configuration; configure volumes, access credentials and backup policies outside this service as appropriate.

The period bounds data retrieval, and responses are paginated. Metrics and queue filtering currently read all evaluations for that operation/period into memory; a future high-volume deployment should move aggregation and pagination into MongoDB. A readiness probe with upstream checks and event delivery are separate future integrations; `/health/live` only reports process liveness.

## Test

```powershell
dotnet test src/backend/tests/FeedbackReliabilityService.Tests --configuration Release
dotnet test src/backend/MineSenseSafety.sln --configuration Release
```

Domain, application authorization, HTTP API with signed JWTs, in-memory concurrency, the Alerts HTTP adapter and BSON snapshot round-trip run without external services. The real MongoDB integration test is explicitly skipped unless configured:

```powershell
$env:FEEDBACK_TEST_MONGODB = 'mongodb://localhost:27017'
dotnet test src/backend/tests/FeedbackReliabilityService.Tests --configuration Release
```

It creates a unique temporary database and drops only that test database in `finally`. It checks index initialization, duplicate registration, concurrent writes and persistence across repository instances. `.github/workflows/feedback-mongodb-ci.yml` always runs it with a MongoDB 7 service on relevant PRs.
