# Operations Service

Operations Service implements the operational structure of EP08 (US17, US19, TS-05 to TS-08):
mining units with their locations, fleets, vehicles, heavy machinery operators and the
assignments that tie an operator to a shift and a vehicle. Monitoring sessions, devices and
alerts refer to these resources by id (logical references, no cross-service foreign keys).

## API routes

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/v1/mining-units` | Register a mining unit (`/api/v1/mining-sites` is the TS-05 alias). |
| `GET` | `/api/v1/mining-units` | List mining units. |
| `GET` / `PUT` | `/api/v1/mining-units/{miningUnitId}` | Get or update a mining unit. |
| `POST` | `/api/v1/mining-units/{miningUnitId}/locations` | Add a pit, dump, workshop or other location. |
| `POST` / `GET` | `/api/v1/fleets` | Register fleets or list them by `miningUnitId`. |
| `POST` | `/api/v1/fleets/{fleetId}/vehicles` | TS-06: register a vehicle inside a fleet. |
| `POST` / `GET` | `/api/v1/vehicles` | Register vehicles or list them by `fleetId`. |
| `GET` / `PUT` | `/api/v1/vehicles/{vehicleId}` | Get or update a vehicle (`Active`, `InMaintenance`). |
| `POST` / `GET` | `/api/v1/operators` | TS-07: register operators or list them by `miningUnitId`. |
| `GET` / `PUT` | `/api/v1/operators/{operatorId}` | Get or update an operator (`Active`, `Inactive`). |
| `POST` | `/api/v1/operator-assignments` | TS-08: assign an operator to a shift and a vehicle (`/api/v1/shift-assignments` is the alias). |
| `GET` | `/api/v1/operator-assignments?operatorId=` | Assignment history, including superseded and ended ones. |
| `GET` | `/api/v1/operator-assignments/active?operatorId=&date=` | Assignment in force on a date. |
| `POST` | `/api/v1/operator-assignments/{assignmentId}/vehicle-change` | Change vehicle, keeping the previous context in the history. |
| `POST` | `/api/v1/operator-assignments/{assignmentId}/end` | End an assignment. |
| `GET` | `/api/v1/operator-assignments/context` | Tell Monitoring whether an operator may run a session on a vehicle. |

## Status codes

| Code | When |
|---|---|
| `201 Created` | A resource was registered; `Location` points to its `GET` route. |
| `400 Bad Request` | Mandatory data is missing or an enum value is unknown; the message lists every missing field. |
| `404 Not Found` | The resource, or a referenced mining unit, fleet, vehicle or operator, does not exist. |
| `409 Conflict` | Duplicated business code, employee code, fleet name, vehicle code or location, and overlapping assignments of the same operator or of the same vehicle in the same shift. |
| `422 Unprocessable Entity` | Other broken rules, such as assigning an inactive operator or a vehicle in maintenance. |

## Notes

- Assignments are per shift day: `validFrom`/`validTo` are dates. Clients that follow TS-08 may send
  `startsAt`/`endsAt` (ISO 8601) instead; only their date part is kept.
- The shift is sent by name (`Day`, `Night`) instead of a `shiftId`, because shifts are a value
  object of the context and not a separate resource.
- Persistence is in memory for now; the report assigns PostgreSQL with EF Core to this context.

## Verification

```powershell
dotnet test src/backend/tests/OperationsService.Tests/OperationsService.Tests.csproj
```
