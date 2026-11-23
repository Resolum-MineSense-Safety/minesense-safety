# Fatigue Detection Service

Fatigue Detection Service implements EP02. It receives the biometric signals of an operator during
a monitoring session, classifies the fatigue risk and keeps every assessment so the latest one and
the history of a session can be queried.

## Risk classification

`FatigueRiskPolicy` derives the risk level from three signals. The thresholds are reference
values to be calibrated with the Edge inference model.

| Level | Condition |
|---|---|
| `Critical` | PERCLOS ≥ 0.40, or PERCLOS ≥ 0.25 with HRV < 20 ms. |
| `Warning` | PERCLOS ≥ 0.15, or blink rate ≥ 25 per minute, or HRV < 30 ms. |
| `Normal` | None of the above. |

`Warning` and `Critical` assessments are the ones that require a preventive alert in the Alert
Service. PERCLOS must be between 0 and 1, the blink rate cannot be negative and HRV must be greater
than zero; otherwise the request answers 422.

## API routes

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/v1/fatigue-assessments` | Assess fatigue (201) from `operatorId`, `monitoringSessionId`, `perclos`, `blinkRatePerMinute` and `heartRateVariabilityMs`. |
| `GET` | `/api/v1/fatigue-assessments?operatorId=` | Assessments of an operator, newest first. |
| `GET` | `/api/v1/fatigue-assessments/latest?operatorId=` | Latest assessment of the operator (404 if none). |
| `GET` | `/api/v1/fatigue-assessments/{fatigueAssessmentId}` | One assessment. |
| `GET` | `/api/v1/fatigue-assessments/sessions/{monitoringSessionId}` | Assessments of a session in chronological order. |

## Configuration

`appsettings.json` only sets logging. In Docker Compose the service listens on port 5205.

## Verification

```powershell
dotnet test src/backend/tests/FatigueDetectionService.Tests/FatigueDetectionService.Tests.csproj
```

Persistence is in memory, like the other services of this slice.
