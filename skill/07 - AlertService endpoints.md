# 07 - AlertService: microservicio y endpoints

Base URL local: `http://localhost:5233` · Swagger: `http://localhost:5233/swagger/index.html` · Definición: `http://localhost:5233/swagger/v1/swagger.json` (`"openapi": "3.0.4"`, verificado en vivo).
Código: `src/backend/AlertService/` · Tests: `src/backend/tests/AlertService.Tests/` (30 tests, **fuera** del `.sln`: correr con `dotnet test tests\AlertService.Tests\AlertService.Tests.csproj`).

## 1. Qué es y cómo funciona

Microservicio EP03: avisos preventivos en cabina con máquina de estados. Agregado `Alert` (`Domain/Model/Aggregates/Alert.cs:12-56`): nace en `Issued` con `IssuedAt` del reloj del servidor (`TimeProvider` en `Application/Internal/CommandServices/AlertCommandService.cs:13`); el operador lo confirma (`Acknowledge` → `Acknowledged` + `AcknowledgedAt`) o se escala al supervisor (`Escalate` → `Escalated`), solo desde `Issued` — cualquier otra transición lanza `DomainException`. El vencimiento lo define `AlertAcknowledgementPolicy` (`Domain/Services/AlertAcknowledgementPolicy.cs:10-14`): **30 s** para `Critical`, **2 min** para `Warning`; `IsAcknowledgementOverdue(now)` (`Alert.cs:54-55`) es `Status==Issued && now-IssuedAt > deadline` (borde estricto `>`, no `>=`).

Flujo común: Controller (`Interfaces/REST/AlertsController.cs`) → assemblers (`Interfaces/REST/Transform/`) → command/query services (`Application/Internal/`) → repo in-memory (`Infrastructure/Persistence/InMemory/`, `ConcurrentDictionary`; se pierde al reiniciar) → recurso de respuesta. `DomainException` → **422** `{"message": "..."}` vía `DomainExceptionFilter` (`Shared/.../DomainExceptionFilter.cs:12-17`); `null` de repositorio → **404**; `[ApiController]` → **400** ante JSON/GUID malformado. Sin autenticación, sin paginación, sin `BackgroundService` (el escalado por vencimiento es un `POST` manual, no automático), sin `EscalatedAt` (solo existe `AcknowledgedAt`).

Rutas: `[Route("api/v1/[controller]")]` + kebab-case → `AlertsController` = `/api/v1/alerts`. Todo en `application/json`. Enums: `AlertSeverity {Warning, Critical}`, `AlertStatus {Issued, Acknowledged, Escalated}`.

## 2. Endpoints

### 2.1 `POST /api/v1/alerts` — emitir alerta
- **Qué hace**: crea una alerta en `Issued` (`AlertsController.cs:20-29` → `IssueAlertCommandFromResourceAssembler.cs:12-15` → `AlertCommandService.cs:11-16` → `CreatedAtAction(GetAlertById)`).
- **Recibe** (`IssueAlertResource.cs:5-9`): `operatorId` (uuid `[JsonRequired]`, ≠ empty → si no, `422 "An alert must belong to an operator."`), `assessmentId` (uuid `[JsonRequired]`; **no** se valida empty ni existencia en Fatigue — es un id huérfano), `severity` (string **requerido aunque sin `[JsonRequired]`**: se parsea con `Enum.TryParse(ignoreCase)`; valores `"Warning"|"Critical"` en cualquier mayúscula; ausente/nulo/desconocido → `422 "Unknown alert severity '...'."`).
- **Devuelve 201** `AlertResource.cs:3-10`: `id` (nuevo), `operatorId`, `assessmentId`, `severity`, `status: "Issued"`, `issuedAt` (reloj servidor), `acknowledgedAt: null`. Header `Location: /api/v1/alerts/{id}`. Cada llamada crea una alerta nueva (sin deduplicación por `assessmentId`).

### 2.2 `GET /api/v1/alerts/{alertId}` — obtener por id
- **Qué hace**: devuelve una alerta (`:31-39`, `GetAlertByIdQuery` → `FindByIdAsync`).
- **Recibe**: `alertId` en ruta (uuid, `:guid`; malformado → `400`).
- **Devuelve**: `200` con el recurso; `404` si no existe.

### 2.3 `GET /api/v1/alerts?operatorId={uuid}` — alertas por operador
- **Qué hace**: todas las alertas de un operador, **más recientes primero** (`:41-47` → `FindByOperatorIdAsync`, `InMemoryAlertRepository.cs:10-14` `OrderByDescending(IssuedAt)`).
- **Recibe**: `operatorId` en query (uuid). `Guid.Empty`/ausente → `200 []` (no valida).
- **Devuelve**: `200` array (sin paginación: todo).

### 2.4 `GET /api/v1/alerts/status/{status}` — cola del supervisor
- **Qué hace**: alertas filtradas por estado, **más antiguas primero** (`:50-60` → `GetAlertsByStatusQuery` → `FindByStatusAsync`, `InMemoryAlertRepository.cs:16-20` `OrderBy(IssuedAt)`). Es la cola de trabajo: `issued` pendientes, `escalated` para el supervisor.
- **Recibe**: `status` en ruta (string, case-insensitive: `issued`, `ISSUED`, `Acknowledged`, `escalated` valen; desconocido → `422 "Unknown alert status '...'."`).
- **Devuelve**: `200` array; `422` si el estado no existe.

### 2.5 `POST /api/v1/alerts/{alertId}/acknowledgements` — confirmar (operador)
- **Qué hace**: `Issued → Acknowledged` + registra `AcknowledgedAt` con el reloj del servidor (`:62-71` → `AcknowledgeAlertCommand` → `AlertCommandService.cs:18-26`).
- **Recibe**: `alertId` en ruta (uuid). Sin body.
- **Devuelve**: `200` con el recurso (`status: "Acknowledged"`, `acknowledgedAt` con hora). **404** si no existe. **422** si ya no está `Issued` (`"Only issued alerts can be acknowledged; current status is ... ."`) — p. ej. doble ack o ack de escalada. Nota: el ack tardío (tras el deadline pero antes del batch) **sí tiene éxito**; nada lo rechaza.

### 2.6 `POST /api/v1/alerts/{alertId}/escalations` — escalar manual (supervisor)
- **Qué hace**: `Issued → Escalated` (`:73-82` → `EscalateAlertCommand` → `AlertCommandService.cs:28-36`). No exige vencimiento: escala a los 0 s si se quiere.
- **Recibe**: `alertId` en ruta (uuid). Sin body.
- **Devuelve**: `200` con el recurso (`status: "Escalated"`). **404** si no existe. **422** si no está `Issued` (`"Only unacknowledged alerts can be escalated; current status is ... ."`).

### 2.7 `POST /api/v1/alerts/overdue-escalations` — escalar vencidas (batch)
- **Qué hace**: escala **todas** las `Issued` cuyo deadline ya pasó y devuelve solo las escaladas (`:85-91` → `EscalateOverdueAlertsCommand` → `AlertCommandService.cs:38-51`: lee `Issued`, filtra `IsAcknowledgementOverdue(now)`, escala+persiste cada una).
- **Recibe**: nada (sin body ni query).
- **Devuelve**: `200` array con las escaladas (vacío `[]` si ninguna venció). Siempre `200`, nunca 404/422. Es **manual**: hay que invocarlo (p. ej. cada 10–30 s); sin scheduler no hay SLA real de 30 s/2 min.

## 3. Ejemplos de validación (PowerShell)

```powershell
$op = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
$as = "7c9e6679-7425-40de-944b-e07fc1f90ae7"
$base = "http://localhost:5233/api/v1/alerts"

# Emitir Critical → 201, status Issued, acknowledgedAt null (+ header Location)
$al = @{ operatorId=$op; assessmentId=$as; severity="Critical" } |
  ConvertTo-Json | Invoke-RestMethod -Uri $base -Method Post -ContentType "application/json"
$id = $al.id

# Lecturas → 200
Invoke-RestMethod "$base/$id"                 # por id
Invoke-RestMethod "$base?operatorId=$op"      # por operador, newest-first
Invoke-RestMethod "$base/status/issued"       # cola, oldest-first
Invoke-RestMethod "$base/status/ESCALATED"    # case-insensitive OK

# Acknowledge → 200 Acknowledged; repetirlo → 422 "Only issued alerts..."
Invoke-RestMethod -Uri "$base/$id/acknowledgements" -Method Post
# Escalar una Acknowledged → 422 "Only unacknowledged alerts..."

# Escalado manual de otra alerta + batch
$al2 = @{ operatorId=$op; assessmentId=$as; severity="Warning" } |
  ConvertTo-Json | Invoke-RestMethod -Uri $base -Method Post -ContentType "application/json"
Invoke-RestMethod -Uri "$base/$($al2.id)/escalations" -Method Post   # 200 Escalated
Invoke-RestMethod -Uri "$base/overdue-escalations" -Method Post     # 200 [] (recién creadas, nada vencido)

# Vencimiento real: emite Critical, espera 35 s, llama overdue-escalations → la verás en status/escalated.
# Warning necesita 2 min + 1 s.

# Errores
# severity "Grave" → 422 {"message":"Unknown alert severity 'Grave'."}
# severity ausente → 422 {"message":"Unknown alert severity ''."}
# operatorId 00000000-... → 422 {"message":"An alert must belong to an operator."}
# GET id inexistente → 404; POST ack/escalate id inexistente → 404
# GET status/pendiente → 422 {"message":"Unknown alert status 'pendiente'."}
```

> Límites a recordar: datos en memoria (reiniciar = perder todo), sin auth, sin paginación, `assessmentId` no se valida contra Fatigue, y el vencimiento solo ocurre si alguien llama al batch.
