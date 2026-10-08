# Sprint 1 — Base operacional de MineSense Safety

**Sprint Goal:** iniciar y verificar el monitoreo de fatiga, asegurar la disponibilidad de los dispositivos
asignados y registrar la estructura operacional minera (sección 1.3.5.1 del informe). 20 Story Points.

Historias: US15, US16 (Device Management, EP07), US01, US02 (Monitoring, EP01), US17, US19 (Operations, EP08).

## Estado de partida

Los tres microservicios del Sprint ya existen con su base funcionando: dominio, casos de uso, API REST,
repositorios en memoria y pruebas (unitarias y HTTP). Cada clase que corresponde a una tarea lleva un
comentario `// Sprint 1 · TXX (Responsable)` con lo que ya cubre y **lo que falta**. Busque su tarea con:

```bash
grep -rn "Sprint 1 · T02" src/backend
```

| Servicio | Puerto local | Ruta base |
|---|---|---|
| `MonitoringService` | 5101 | `/api/v1/monitoring-sessions` |
| `DeviceManagementService` | 5102 | `/api/v1/devices`, `/api/v1/pre-shift-checks` |
| `OperationsService` | 5103 | `/api/v1/mining-units`, `/api/v1/fleets`, `/api/v1/vehicles`, `/api/v1/operator-assignments` |

`MonitoringService` valida el contexto operacional llamando a
`OperationsService` (`GET /api/v1/operator-assignments/context`), configurado en `Services:OperationsServiceUrl`.

## Tablero de tareas

| Tarea | US | Responsable | Dónde empezar | Pendiente |
|---|---|---|---|---|
| T01 Diseñar checklist de verificación | US15 | Jhosep Argomedo | `DeviceManagementService/Domain/Services/PreShiftCheckPolicy.cs` | Confirmar con el equipo los dispositivos indispensables y el umbral de batería. |
| T02 Implementar prueba de dispositivos | US15 | Andreow Santiago | `DeviceManagementService/Application/Internal/CommandServices/PreShiftCheckCommandService.cs` | Probar cada dispositivo a través del gateway Edge; hoy usa el último estado reportado. |
| T03 Mostrar resultado de verificación | US15 | Carlos Onofre | `DeviceManagementService/Interfaces/REST/PreShiftChecksController.cs` | Vista del resultado en `src/frontend`. |
| T04 Implementar inicio de monitoreo | US01 | Farid Coronel | `MonitoringService/Application/Internal/CommandServices/MonitoringSessionCommandService.cs` | Exigir una verificación previa satisfactoria (T02) antes de iniciar. |
| T05 Integrar gateway Edge | US01 | Ian Santisteban | `MonitoringService/Interfaces/REST/MonitoringSessionsController.cs` | Ingesta MQTT de la disponibilidad de señales desde `src/edge` (`TODO(T05)`). |
| T06 Registrar sesión de monitoreo | US01 | Joseph Huamani | `MonitoringService/Infrastructure/Persistence/InMemory/InMemoryMonitoringSessionRepository.cs` | Adaptador PostgreSQL en lugar del repositorio en memoria. |
| T07 Diseñar indicador de estado | US02 | Nicolas Juarez | `MonitoringService/Domain/Services/MonitoringStatusPolicy.cs` | Acordar con T09 la representación visual del estado. |
| T08 Implementar consulta de estado | US02 | Renato Calvo | `MonitoringService/Application/Internal/QueryServices/MonitoringSessionQueryService.cs` | Revisar los mensajes al operador y el historial por turno. |
| T09 Integrar estado con dashboard | US02 | Jhosep Argomedo | `MonitoringService/Interfaces/REST/Resources/MonitoringStatusResource.cs` | Indicador de estado y consulta periódica en `src/frontend`. |
| T10 Modelo de disponibilidad | US16 | Andreow Santiago | `DeviceManagementService/Domain/Model/Aggregates/MonitoringDevice.cs` | Umbral de batería y tiempo máximo sin heartbeat. |
| T11 Registro de fallas | US16 | Carlos Onofre | `DeviceManagementService/Domain/Model/ValueObjects/DeviceFailure.cs` | Persistir el registro y avisar a Monitoring de desconexiones y recuperaciones. |
| T12 Vista de disponibilidad | US16 | Farid Coronel | `DeviceManagementService/Interfaces/REST/DevicesController.cs` | Vista del supervisor en `src/frontend`. |
| T13 Modelo de estructura minera | US17 | Ian Santisteban | `OperationsService/Domain/Model/Aggregates/` | Desactivación de unidades y adaptador de persistencia. |
| T14 CRUD operacional | US17 | Joseph Huamani | `OperationsService/Application/Internal/CommandServices/` | Baja de unidades, flotas y vehículos, y vistas de administración. |
| T15 Validación de relaciones | US17 | Nicolas Juarez | `OperationsService/Domain/Services/OperationalStructureValidator.cs` | Validar el operador contra Identity & Access. |
| T16 Modelo de asignación | US19 | Renato Calvo | `OperationsService/Domain/Services/AssignmentConflictPolicy.cs` | Revisar reglas de solapamiento con el equipo. |
| T17 Gestión de asignaciones | US19 | Jhosep Argomedo | `OperationsService/Application/Internal/CommandServices/OperatorAssignmentCommandService.cs` | Vista de administración de asignaciones. |
| T18 Validar contexto de monitoreo | US19 | Andreow Santiago | `MonitoringService/Infrastructure/Operations/HttpOperationalContextService.cs` | Validar el turno contra la hora de inicio y añadir reintentos. |

## Cómo trabajar una tarea

1. Crear la rama `feature/<tarea>-<descripcion>` desde `develop` (por ejemplo `feature/t02-ping-dispositivos`).
2. Completar lo pendiente de la tarea y actualizar el comentario `Sprint 1 · TXX`.
3. Agregar o ajustar las pruebas del servicio (`src/backend/tests/<Servicio>.Tests`), patrón AAA.
4. Abrir un Pull Request hacia `develop` con la plantilla y cerrar el issue de la tarea con `Closes #N`.
