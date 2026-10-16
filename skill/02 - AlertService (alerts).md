# 02 - AlertService (alerts)

Ubicación: `src/backend/AlertService/` + tests en `src/backend/tests/AlertService.Tests/`

## 1. Arquitectura — correcta

Patrón DDD ligero + CQRS por convención, idéntico a Fatigue:
`Domain/{Aggregates,Commands(4),Queries(3),ValueObjects,Services,Repositories}` → `Application/Internal/{CommandServices,QueryServices}` → `Infrastructure/Persistence/InMemory/` → `Interfaces/REST/{Resources,Transform,AlertsController}`.

- `Program.cs:11-17`: `AddMineSenseWebApi()` + `TimeProvider.System` singleton + repo singleton + servicios scoped. Correcto y testeable.
- `IAlertCommandService.Handle(...)` / `IAlertQueryService.Handle(...)` sobrecargados por tipo. Correcto.

## 2. Correctitud por clase — verificado

| Clase | Veredicto |
|---|---|
| `Domain/Model/Aggregates/Alert.cs:12-56` | Correcta. Ctor valida `OperatorId==Empty→DomainException`, inicia `Issued`. `Acknowledge`/`Escalate` solo desde `Issued`. `IsAcknowledgementOverdue = Issued && now-IssuedAt > DeadlineFor(Severity)` (borde `>` explícito). |
| `ValueObjects/AlertSeverity.cs`, `AlertStatus.cs` | Correctos. Sin `Unknown`, obliga validar parseo (se hace). |
| `Domain/Services/AlertAcknowledgementPolicy.cs:10-14` | Correcta: 30s Critical, 2min Warning. |
| Commands `Issue/Acknowledge/Escalate/Overdue` + Queries `GetById/ByOperator/ByStatus` | Correctos como `record`. |
| `Application/Internal/CommandServices/AlertCommandService.cs:11-51` | Correcto. `Issue→Add`, `Ack/Escalate→404 si null`, muta + `Update`. `Overdue` filtra solo `Issued+IsOverdue` y escala. Propaga `DomainException→422`. |
| `QueryServices/AlertQueryService.cs:10-17` | Passthrough correcto. |
| `Infrastructure/.../InMemoryAlertRepository.cs:10-20` | Correcto. `FindByOperatorId OrderByDescending`, `FindByStatus OrderBy` (cumple contrato "oldest first" de `IAlertRepository.cs:9-12`). |
| `Interfaces/REST/AlertsController.cs:20-91` | Correcto. `POST /api/v1/alerts→201 CreatedAtAction`, `GET {id}`, `GET ?operatorId`, `GET status/{status}` con `Enum.TryParse(ignoreCase)→422`, `POST {id}/acknowledgements\|escalations`, `POST overdue-escalations`. Sin try/catch (delegado a `DomainExceptionFilter`). |
| Assemblers/Resources | Mapeos 1:1 correctos. Parseo Severity case-insensitive → 422. |
| `AlertService.csproj` / `appsettings` | `net8.0`, Nullable on. Sin EF/MQTT/Auth/HealthChecks. Solo logging. |

## 3. Reglas de negocio EP03

Implementadas: emitir→`Issued`; ack solo si `Issued` + `AcknowledgedAt`; escalado manual + batch overdue por severidad; bordes verificados (Critical 30s→false/31s→true, Warning 60s→false/121s→true).

No implementadas / ambiguas:
- Ack tardío no se rechaza: si vence pero el batch aún no corrió, el ack tiene éxito. Sin definir quién gana.
- Escalado manual a los 0s permitido (`EscalateAlertCommand` no exige overdue). Si la intención era solo automático, el endpoint lo viola.
- Sin `EscalatedAt` (sí hay `AcknowledgedAt`). La cola de supervisor no sabe cuándo se escaló.
- Sin deduplicación por `AssessmentId`: cada `Issue` crea un Alert nuevo.
- Transiciones terminales no testeadas: doble `Acknowledge`, doble `Escalate`.

## 4. Bugs / huecos concretos

1. `Alert.cs:23-24` no valida `AssessmentId==Empty`. Mismo defecto que `FatigueAssessment` con `MonitoringSessionId`.
2. Sin ctor privado sin parámetros → EF Core fallará al materializar cuando se agregue persistencia real. Necesita `private Alert(){}` + config.
3. `IssueAlertResource.cs:8` `Severity` no-nullable sin `[Required]`; JSON sin propiedad → `null!` → 422 por accidente (`Enum.TryParse(null)` false). Añadir guarda `IsNullOrWhiteSpace`.
4. `AlertsController.cs:43-47` `GET ?operatorId=Empty` devuelve `200 []` en vez de 400/422.
5. Sin paginación: controller expone lista completa. OK in-memory, inaceptable en prod.
6. Overdue es `POST` manual (`AlertsController.cs:87-91`). Sin `BackgroundService` con timer, sin eventos, sin MQTT.
7. Repo singleton (`Program.cs:15`) = estado global + pérdida al reiniciar. Tests E2E se aíslan solo por usar `Guid.NewGuid()` fresco.
8. `AlertService.http:2` usa `operatorId` con último grupo de 11 chars (inválido) → 400/404 al probar manual. Además faltan ejemplos `GET {id}`, `acknowledgements`, `escalations`.

## 5. Tests — cobertura alta para in-memory (~85-90% líneas)

- Dominio `AlertTests.cs` (9 métodos): ctor, ack, escalado, overdue por severidad + borde. Falta: `AssessmentId.Empty`, doble ack/escalate, `IsOverdue(Escalated)`.
- Aplicación `AlertServicesTests.cs` (5 tests + `FakeAlertRepository`): persistencia, ack+escalate, null en desconocido, overdue solo escala vencido (con `FakeTimeProvider`). Falta: propagación `DomainException`, overdue vacío.
- Infra `InMemoryAlertRepositoryTests.cs` (3 tests): ordenamientos newest/oldest-first. Suficiente.
- API `AlertsApiTests.cs` (10 tests, `WebApplicationFactory`): 201, 422 severidad desconocida, filtro operador, 200/404, ack→escalado posterior→422, cola `status/escalated`, overdue en-tiempo no escala. Falta: `POST OperatorId.Empty→422`, `Severity` ausente, `status` case-insensitive, doble ack vía API, **overdue positivo vía API imposible** (usa `TimeProvider.System` real, no se puede avanzar el reloj — hay que permitir sustituirlo en la factory).

## 6. ¿Completo al 100%? No

- Como slice académico EP03 + in-memory: **~90%**.
- Para producción: **no apto**. Falta: EF Core + Postgres/Mongo + migraciones + índices; `BackgroundService` para overdue (o 2 timers 30s/2min) con idempotencia; suscriptor MQTT/evento desde Fatigue (`RequiresAlert→IssueAlert`, hoy `AssessmentId` huérfano); auth JWT + CORS + healthchecks + paginación (`page/pageSize`) + filtro combinado + `EscalatedAt` + `Idempotency-Key` + Serilog/OTel + FluentValidation + rate-limit + `Dockerfile` + compose de app + secrets.
