# 06 - FatigueDetectionService: microservicio y endpoints

Base URL local: `http://localhost:5158` · Swagger: `http://localhost:5158/swagger/index.html` · Definición: `http://localhost:5158/swagger/v1/swagger.json` (`"openapi": "3.0.4"`, verificado en vivo).
Código: `src/backend/FatigueDetectionService/` · Tests: `src/backend/tests/FatigueDetectionService.Tests/` (28 tests, en el `.sln`).

## 1. Qué es y cómo funciona

Microservicio EP02: evalúa lecturas biométricas puntuales de un operador y las clasifica en un nivel de riesgo. Agregado inmutable `FatigueAssessment` (`Domain/Model/Aggregates/FatigueAssessment.cs:12-33`): cada `POST` crea una evaluación nueva con `Id` generado en servidor (`AggregateRoot`: `Guid.NewGuid()`) y `AssessedAt` tomado del reloj del servidor (`TimeProvider` en `Application/Internal/CommandServices/FatigueAssessmentCommandService.cs:14`). El cliente **no** puede fijar ni `id` ni `assessedAt`. La clasificación la hace `FatigueRiskPolicy.Classify` (`Domain/Services/FatigueRiskPolicy.cs:18-31`) en el constructor, y `RequiresAlert` deriva de ella (`:21`: `Warning or Critical → true`).

Flujo común de todos los endpoints: Controller (`Interfaces/REST/FatigueAssessmentsController.cs`) → assembler resource→command o query (`Interfaces/REST/Transform/`) → command/query service (`Application/Internal/`) → repositorio in-memory (`Infrastructure/Persistence/InMemory/`, `ConcurrentDictionary`; todo se pierde al reiniciar) → assembler entity→resource → respuesta JSON. Reglas de negocio rotas lanzan `DomainException`, que `DomainExceptionFilter` (`Shared/.../DomainExceptionFilter.cs:12-17`) convierte en **422** con cuerpo `{"message": "..."}`. `[ApiController]` devuelve **400** automático ante JSON malformado o GUID con formato inválido. Sin autenticación, sin paginación, sin idempotencia (reintentar un `POST` crea duplicados).

Rutas: `[Route("api/v1/[controller]")]` + convención kebab-case → `FatigueAssessmentsController` = `/api/v1/fatigue-assessments`. Todos producen/consumen `application/json`.

## 2. Regla de clasificación (aplica a todo `POST`)

`FatigueRiskPolicy.cs:11-16,18-31` (umbrales de referencia, no calibrados):
- **Critical** si `perclos >= 0.40`, o (`perclos >= 0.25` Y `heartRateVariabilityMs < 20`).
- **Warning** si `perclos >= 0.15`, o `blinkRatePerMinute >= 25`, o `heartRateVariabilityMs < 30`.
- Si no, **Normal**. `RequiresAlert = (Warning, Critical)`.

Validaciones de `BiometricSignals` (`Domain/Model/ValueObjects/BiometricSignals.cs:17-22`): `perclos ∈ [0,1]`; `blinkRatePerMinute >= 0`; `heartRateVariabilityMs > 0`. Además `operatorId != Guid.Empty` (`FatigueAssessment.cs:25-26`). Huecos conocidos: `NaN`/`Infinity` pasan la validación y clasifican como `Normal`; `monitoringSessionId == Guid.Empty` se acepta.

## 3. Endpoints

### 3.1 `POST /api/v1/fatigue-assessments` — evaluar fatiga
- **Qué hace**: crea una evaluación y devuelve su clasificación (`FatigueAssessmentsController.cs:17-27` → `AssessFatigueCommandFromResourceAssembler.cs:8-10` → `FatigueAssessmentCommandService.cs:12-17` → `CreatedAtAction(GetFatigueAssessmentById)`).
- **Recibe** (`AssessFatigueResource.cs:5-11`, todo `[JsonRequired]`): `operatorId` (uuid, ≠ empty), `monitoringSessionId` (uuid), `perclos` (double 0–1), `blinkRatePerMinute` (double ≥ 0), `heartRateVariabilityMs` (double > 0).
- **Devuelve 201** `FatigueAssessmentResource.cs:3-12`: `id`, `operatorId`, `monitoringSessionId`, `perclos`, `blinkRatePerMinute`, `heartRateVariabilityMs`, `riskLevel` (`"Normal"|"Warning"|"Critical"` como string), `requiresAlert` (bool), `assessedAt` (date-time UTC). Incluye header `Location: /api/v1/fatigue-assessments/{id}`.
- **Errores**: `422 {"message": "..."}` si `operatorId` vacío (`"A fatigue assessment must belong to an operator."`) o señal fuera de rango (`"PERCLOS must be between 0 and 1."`, `"Blink rate cannot be negative."`, `"Heart rate variability must be greater than zero."`); `400` si falta una propiedad requerida o un GUID está malformado.

### 3.2 `GET /api/v1/fatigue-assessments/{fatigueAssessmentId}` — obtener por id
- **Qué hace**: devuelve una evaluación (`:29-38`, `GetFatigueAssessmentByIdQuery` → `FindByIdAsync`).
- **Recibe**: `fatigueAssessmentId` en ruta (uuid, constraint `:guid`; formato inválido → `400` automático).
- **Devuelve**: `200` con el recurso; `404` (vacío/`ProblemDetails`) si no existe.

### 3.3 `GET /api/v1/fatigue-assessments?operatorId={uuid}` — historial por operador
- **Qué hace**: historial completo del operador (`:40-47` → `FindByOperatorIdAsync`).
- **Recibe**: `operatorId` en query (uuid). Ojo: `Guid.Empty` o ausente devuelve `200 []`, no error.
- **Devuelve**: `200` con **array** ordenado **más reciente primero** (`InMemoryFatigueAssessmentRepository.cs:10-14`, `OrderByDescending(AssessedAt)`). Sin paginación: trae todo.

### 3.4 `GET /api/v1/fatigue-assessments/latest?operatorId={uuid}` — última evaluación
- **Qué hace**: la evaluación más reciente del operador (`:49-58` → `FindByOperatorIdAsync(...)` + `FirstOrDefault()` en `FatigueAssessmentQueryService.cs:17-21`; el "latest" depende del orden del repositorio).
- **Recibe**: `operatorId` en query (uuid).
- **Devuelve**: `200` con el recurso; `404` si el operador no tiene evaluaciones.

### 3.5 `GET /api/v1/fatigue-assessments/sessions/{monitoringSessionId}` — serie de una sesión
- **Qué hace**: todas las evaluaciones de una sesión de monitoreo, en orden temporal (`:60-67` → `FindByMonitoringSessionIdAsync`; `Guid.Empty` cortocircuita a `[]` en `FatigueAssessmentQueryService.cs:23-27`).
- **Recibe**: `monitoringSessionId` en ruta (uuid).
- **Devuelve**: `200` con **array** ordenado **más antiguo primero** (`InMemoryFatigueAssessmentRepository.cs:16-20`, `OrderBy(AssessedAt)`). `[]` si la sesión no existe o es `Guid.Empty`.

## 4. Ejemplos de validación (PowerShell)

```powershell
$op = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
$ses = "7c9e6679-7425-40de-944b-e07fc1f90ae7"
$base = "http://localhost:5158/api/v1/fatigue-assessments"

# Normal: perclos bajo, blink normal, hrv alta → 201, riskLevel Normal, requiresAlert false
@{ operatorId=$op; monitoringSessionId=$ses; perclos=0.05; blinkRatePerMinute=12; heartRateVariabilityMs=60 } |
  ConvertTo-Json | Invoke-RestMethod -Uri $base -Method Post -ContentType "application/json"

# Warning por PERCLOS (0.20 ≥ 0.15) → 201, Warning, requiresAlert true
@{ operatorId=$op; monitoringSessionId=$ses; perclos=0.20; blinkRatePerMinute=15; heartRateVariabilityMs=50 } |
  ConvertTo-Json | Invoke-RestMethod -Uri $base -Method Post -ContentType "application/json"

# Critical combinado (0.28 ≥ 0.25 y hrv 18 < 20) → 201, Critical, requiresAlert true
@{ operatorId=$op; monitoringSessionId=$ses; perclos=0.28; blinkRatePerMinute=22; heartRateVariabilityMs=18 } |
  ConvertTo-Json | Invoke-RestMethod -Uri $base -Method Post -ContentType "application/json"

# Borde exacto Warning: perclos 0.15 → Warning; 0.149 → Normal
# Borde exacto Critical: perclos 0.40 → Critical; 0.399 (con hrv alta y blink bajo) → Warning

# Inválido: perclos 1.7 → 422 {"message":"PERCLOS must be between 0 and 1."}
# Inválido: operatorId 00000000-0000-0000-0000-000000000000 → 422 {"message":"A fatigue assessment must belong to an operator."}
# Inválido: hrv 0 → 422; blink -1 → 422

# Lecturas
Invoke-RestMethod "$base?operatorId=$op"            # 200 array, newest-first
Invoke-RestMethod "$base/latest?operatorId=$op"    # 200 última
Invoke-RestMethod "$base/sessions/$ses"            # 200 array, oldest-first
Invoke-RestMethod "$base/latest?operatorId=11111111-1111-1111-1111-111111111111"  # 404 (sin datos)
```

> Nota: `RequiresAlert=true` hoy **no** crea ninguna alerta (ver `07 - AlertService`): son dos silos sin integración; la emisión es manual.
