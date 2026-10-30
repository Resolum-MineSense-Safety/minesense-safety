# Monitoring Service

Monitoring Service implements EP01 (US01, US02). It opens a monitoring session for an operator
during the shift, records which cabin signals are available (`EyeTracking` from the camera and
`HeartRate` from the wearable), keeps evaluating them and reports missing signals instead of
showing an incorrect reading.

## Rules

- A session starts only when Device Management confirms that the operator's devices are
  available for the shift (pre-shift availability). Otherwise the request answers 422 with the
  reason given by Device Management.
- An operator can have only one open session at a time.
- Status: both signals → `Active`, one signal → `Partial` (naming the missing one), none →
  `Unavailable`, finished session → `Stopped`.
- Losing a signal that is already missing, restoring one that is already available or acting on
  a finished session is rejected with 422. Every loss and restoration is kept in the session
  history.

## API routes

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/v1/monitoring-sessions` | Start a session (201). |
| `GET` | `/api/v1/monitoring-sessions?status=` | List sessions, optionally by status. |
| `GET` | `/api/v1/monitoring-sessions/{sessionId}` | Session detail with its signal history. |
| `GET` | `/api/v1/monitoring-sessions/current?operatorId=` | Current status and message for the operator dashboard. |
| `POST` | `/api/v1/monitoring-sessions/{sessionId}/signal-losses` | The Edge gateway reports a lost signal. |
| `POST` | `/api/v1/monitoring-sessions/{sessionId}/signal-restorations` | The Edge gateway reports a restored signal. |
| `PATCH` | `/api/v1/monitoring-sessions/{sessionId}/finish` | Finish the session (route of the report, section 2.1.6). |
| `POST` | `/api/v1/monitoring-sessions/{sessionId}/stop` | Same action, kept for existing clients. |

## Configuration

| Key | Default | Purpose |
|---|---|---|
| `Services:DeviceManagementServiceUrl` | `http://localhost:5031` | Base URL of Device Management. |
| `Monitoring:BatteryThreshold` | `20` | Minimum battery percentage for a device to count as available. |

In Docker Compose the service listens on port 5201 and reaches Device Management through
`MONITORING_DEVICE_SERVICE_URL` (by default the host on port 5031).

## Verification

```powershell
dotnet test src/backend/tests/MonitoringService.Tests/MonitoringService.Tests.csproj
```

Persistence is in memory, like the other services of this slice; PostgreSQL is pending.
