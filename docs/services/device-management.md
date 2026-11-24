# Device Management Service

Device Management Service implements EP06. It registers the cabin devices, assigns them to an
operator or a vehicle, receives their status reports (status, battery and available components)
and answers the pre-shift availability check that Monitoring calls before opening a session.

## API routes

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/v1/devices` | Register a device (201) with `deviceCode`, `deviceType` and `organizationId`. |
| `GET` | `/api/v1/devices` | List all devices. |
| `PUT` | `/api/v1/devices/{deviceId}/assignment` | Assign the device to an `operatorId` and/or a `vehicleId`. |
| `POST` | `/api/v1/devices/{deviceId}/status` | Report status, battery, time and components (202). |
| `GET` | `/api/v1/devices/{deviceId}/status` | Current device state. |
| `GET` | `/api/v1/devices/pre-shift-availability?operatorId=&batteryThreshold=` | Pre-shift check for an operator. |

## Rules

- The device code must be unique (409); code, type and organization are required (422).
- An assignment needs an operator or a vehicle, non-empty ids, and the device must not already
  have an active assignment (422). There is no unassignment route yet.
- Status is `Available`, `Limited` or `Interrupted`. Battery and threshold must be between 0 and
  100. Component names cannot be empty; duplicates are removed.

## Pre-shift availability

Only the devices assigned to the operator whose type is `camera` (case-insensitive) are
evaluated:

| Result | Recommendation | When |
|---|---|---|
| `ProtectionIncomplete` | `DoNotStartMonitoring` | No camera, or a camera that is not `Available` or has no reported battery. |
| `Warning` | `ReviewBatteryBeforeStarting` | Some camera battery is below `batteryThreshold`. |
| `Available` | `StartMonitoring` | Every camera is available with enough battery. |

The threshold is a query parameter with no default in this service; Monitoring sends its own
`Monitoring:BatteryThreshold` (20 by default).

## Configuration

`appsettings.json` only sets logging. In Docker Compose the service listens on port 5204; the
`http` launch profile uses `http://localhost:5031`, which is Monitoring's default URL for it.

## Verification

```powershell
dotnet test src/backend/tests/DeviceManagementService.Tests/DeviceManagementService.Tests.csproj
```

Persistence is in memory, like the other services of this slice.
