# Fleet Monitoring Service

Fleet Monitoring Service implements EP04, US07, US08 and TS-14. It maintains the control-center
read model for monitored operators and returns the current vehicle, fleet, shift, location, risk
and data freshness. All supplied filters are combined with AND and results are ordered from
Critical to Warning to Normal, then by operator name.

## API routes

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/v1/fleet-operators` | Register one monitored operator. |
| `GET` | `/api/v1/fleet-operators` | Query all current operator states. |
| `GET` | `/api/v1/fleet/status` | Contract alias documented in the architecture report. |
| `GET` | `/api/v1/fleet-monitoring` | TS-14 alias supporting `siteId` and `fleetId`. |
| `GET` | `/api/v1/fleet-operators/{operatorId}` | Get one operator state. |
| `GET` | `/api/v1/operators/{operatorId}/status` | Contract alias for one operator state. |
| `PUT` | `/api/v1/fleet-operators/{operatorId}/risk-level` | Update the current risk level. |
| `PUT` | `/api/v1/fleet-operators/{operatorId}/vehicle` | Update vehicle and location. |

The list routes accept `shift`, `fleet`, `location`, `riskLevel`, `siteId`, `miningUnitId` and
`fleetId`. `siteId` is the report name for the Operations bounded context's `MiningUnitId`; if
both are supplied they must match. Registrations may omit the two operational UUIDs for backwards
compatibility, but when one is supplied both `miningUnitId` and `fleetId` are required.

## Freshness

Every response exposes `lastUpdatedAt`, `freshness` (`Current` or `Stale`) and `isStale`.
The default threshold is five minutes and can be changed with
`FleetMonitoring:StaleAfterMinutes`. A state is still current exactly at the threshold and becomes
stale after it.

## Verification

Run the bounded-context test suite from the repository root:

```powershell
dotnet test src/backend/tests/FleetMonitoringService.Tests/FleetMonitoringService.Tests.csproj
```

The adapter is intentionally in memory, consistent with the other implemented services in this
academic slice. MongoDB persistence, Kafka consumers and JWT authorization remain platform-level
follow-up work described by the architecture ADRs.
