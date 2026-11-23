# Analytics Service

Analytics Service implements EP11 (US24, US25, TS-22). It keeps one historical observation per
fatigue alert with its operational context (shift, mine site, location, fleet), groups those
observations into trends and compares the alerts before and after a preventive measure.

## API routes

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/v1/analytics/fatigue-events` | Record a fatigue alert observation (201), until the Kafka consumer exists. |
| `GET` | `/api/v1/analytics/fatigue-trends` | TS-22: alert counts and proportions grouped by one dimension. |
| `POST` | `/api/v1/analytics/preventive-measures` | US25: register a preventive measure (201). |
| `GET` | `/api/v1/analytics/preventive-measures` | List measures, most recently applied first. |
| `GET` | `/api/v1/analytics/preventive-measures/{measureId}` | One measure. |
| `GET` | `/api/v1/analytics/preventive-measures/{measureId}/evaluation?windowDays=` | US25: indicators before and after the measure. |

## Rules

- Observations: `operatorId`, `alertId`, `occurredAt` and the four context values are required;
  severity is `Warning` or `Critical`. Recording is idempotent per `alertId`, so a resent event
  returns the stored observation instead of inflating the trends.
- Trends: `groupBy` is `Shift` (default), `MineSite`, `Location` or `Fleet`; `shift`,
  `mineSite`, `location` and `fleet` filter the observations (case-insensitive). The period is
  `[from, to)`; `to` defaults to now and `from` to 30 days before `to`. Groups are ordered by
  alert count. Each group reports its alerts, critical alerts and proportion of the total.
- Minimum sample (`AnalyticsPolicy.MinimumSampleSize`): 10 observations. Below it the trend is
  not representative and carries a warning; groups below it are marked as indicative only.
- Preventive measures: type `PreventivePause`, `OperatorRelief`, `ShiftReorganization` or
  `Other`; name and `registeredBy` are required. Empty scope fields mean the whole operation.
- Evaluation: compares two equal windows of `windowDays` (30 by default, 1 to 180) before and
  after `appliedAt`. It is not comparable while the "after" window is still running or when the
  "before" window has fewer than 10 observations. Otherwise it returns the variation of alerts
  and of operators with repeated alerts, stating that it is an observed variation and does not
  prove that accidents were avoided.

## Configuration

The thresholds are constants in `AnalyticsPolicy`; `appsettings.json` only sets logging. In
Docker Compose the service listens on port 5209. The gateway sends `/api/v1/analytics/*` here,
except `/api/v1/analytics/alert-reliability`, which belongs to Feedback & Reliability.

## Verification

```powershell
dotnet test src/backend/tests/AnalyticsService.Tests/AnalyticsService.Tests.csproj
```

Persistence is in memory, like the other services of this slice.
