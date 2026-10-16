# 03 - FatigueDetectionService (detectfatigue)

Ubicación: `src/backend/FatigueDetectionService/` + tests en `src/backend/tests/FatigueDetectionService.Tests/`

## 1. Arquitectura — correcta y consistente con Alert (~95%)

Mismo esqueleto DDD/CQRS, mismo `Program.cs` línea por línea (salvo nombres), mismo `Shared` (`AggregateRoot`, `InMemoryRepository`, `DomainException→422`). `csproj` solo depende de Shared: sin EF, MQTT, MassTransit.

## 2. Correctitud por clase — verificado

- `Aggregates/FatigueAssessment.cs:12-33`: correcto. Clasifica en ctor (invariante), `RequiresAlert => Warning or Critical`, `AssessedAt` vía `TimeProvider` inyectado (`CommandService.cs:14`). Inmutable (evento point-in-time). **Bug: `MonitoringSessionId` no validado (`:28-29` solo valida `OperatorId`).**
- `ValueObjects/BiometricSignals.cs:15-27`: parcialmente correcto. Valida `perclos∈[0,1]`, `blink≥0`, `hrv>0`. **Bug crítico: `NaN`/`Infinity` bypasean todo** (`NaN is <0 or >1 == false`), luego `new(NaN,NaN,NaN)` no lanza y `Classify` devuelve `Normal` (todas las `>=`/`<` son false con NaN). Fatiga con sensor roto = "descansado". Falta `double.IsFinite` + cotas superiores (`blink=1000`, `hrv=10000` hoy pasan). Sin `[Range]` en `AssessFatigueResource.cs:5-11`.
- `ValueObjects/RiskLevel.cs:4-8`: correcto como enum, pero serializado `ToString()` en `FatigueAssessmentResourceFromEntityAssembler.cs:11` (acoplamiento stringly-typed). Comentario dice "shared with Alerts" pero no hay mapper `RiskLevel→AlertSeverity`: riesgo de divergencia.
- `Domain/Services/FatigueRiskPolicy.cs:9-31`: determinista, orden correcto (critical antes que warning). Bordes exactos testeados.
- `Commands/AssessFatigueCommand` + 4 Queries: records correctos, sin validación (delega al agregado, aceptable).
- `CommandService.cs:12-17`: `new + AddAsync` mínimo correcto. Falta idempotencia (doble POST duplica) y publicación de evento si `RequiresAlert`.
- `QueryService.cs:11-27`: **defecto: `GetLatest` hace `FindByOperatorId(...).FirstOrDefault()` (`:17-21`) y depende del orden del repo** (`InMemory...:10-14 OrderByDescending`). Con EF sin OrderBy es indeterminado. Ordenar en el query. **Inconsistencia: `GetBySession` cortocircuita `Empty→[]` (`:25`), pero `GetByOperator/Latest` no**; controller (`:42-58`) devuelve `200 []` ante `operatorId` vacío en vez de 400/422.
- `InMemoryFatigueAssessmentRepository.cs:10-20`: correcto (newest-first por operador, oldest-first por sesión). Sin paginación (`Where().ToList()` todo en memoria).
- `Interfaces/REST/FatigueAssessmentsController.cs:17-67`: 4 endpoints correctos (`POST→201 CreatedAtAction`, `GET {id}`, `GET ?operatorId`, `GET latest`, `GET sessions/{id}`). `422` en POST inválido verificado. Falta: `[Authorize]`, paginación, filtro `?riskLevel/desde/hasta`, validación `Empty`, `ProblemDetails`.
- Resources/Assemblers: mapeos 1:1 correctos. `AssessFatigueResource` usa `[JsonRequired]` (bien, pero `double` ausente puede llegar como `0`; la defensa real sigue en dominio).

## 3. Lógica de riesgo — placeholder, no calibrada

El propio comentario lo admite: `FatigueRiskPolicy.cs:7` "Thresholds are reference values to be calibrated".

| Umbral | Valor | Juicio |
|---|---|---|
| Critical PERCLOS ≥0.40 | microsueño | Defendible (lit. 0.3-0.4) pero sin cita ni config |
| Critical combinado 0.25 + HRV<20 | heurística | Plausible, arbitraria (¿por qué 0.25/20?) |
| Warning PERCLOS ≥0.15 | somnolencia leve | Razonable, no justificado |
| Warning blink ≥25/min | parpadeo alto | Normal 15-20; plausible. Pero blink **bajo** (<10, mirada fija) también indica fatiga y se ignora |
| Warning HRV<30 / Critical<20 | RMSSD | Orden correcto, pero HRV es individual (edad/fitness/estrés). Umbral absoluto sin baseline por operador → falsos +/− |

Problemas estructurales:
1. Sin ML real ni edge: regla `if/or`, sin ONNX, gRPC, MQTT.
2. Evaluación puntual sin historia: sin ventana, tendencia, suavizado. Un pico dispara `Critical`.
3. `BlinkRate` irrelevante para Critical; sin interacción `blink+hrv`.
4. Sin score/confianza ni calidad de señal (`sensorOccluded/lowLight`). Agravado por `NaN→Normal`.
5. Umbrales `private const`, no `appsettings`/`IOptions`: recalibrar exige recompilar.

Para EP02 como referencia: aceptable. Como detector productivo: no justificable.

## 4. Tests — ~20 casos, buena cobertura de referencia

- `FatigueAssessmentTests.cs`: `RequiresAlert` N/W/C, `OperatorId.Empty→DomainException`, 3 señales inválidas. Falta: `MonitoringSessionId.Empty`, `NaN`, cotas altas.
- `FatigueRiskPolicyTests.cs`: bordes exactos. Falta justo-por-debajo (`0.149`, `24.9`, `30.0`).
- `FatigueQueriesTests.cs`: trivial (guardan Guid), suficiente.
- `FatigueAssessmentServicesTests.cs` + `FakeFatigueAssessmentRepository.cs`: persiste, historia+latest, filtro sesión, guard Empty. Falta test orden `latest` con timestamps controlados.
- `InMemory...Tests.cs`: único que verifica ordenamientos opuestos. Bien.
- `FatigueAssessmentsApiTests.cs` (E2E `WebApplicationFactory`): `POST Critical→201`, inválido→422, `GET 200/404`, historia+latest, sesión+`RequiresAlert`, `latest vacío→404`. Falta: `Warning→RequiresAlert true` (solo N/C), `operatorId vacío→422`, assemblers.
- Sin tests: DI, concurrencia, serialización `DateTimeOffset`, OpenAPI, paginación (inexistente).
- Deuda de test: repo singleton compartido entre tests (pasan por `Guid.NewGuid()` fresco, frágil); `TimeProvider.System` en tests de app puede empatar `AssessedAt` en mismo tick.

## 5. ¿Completo al 100%? No

- Prototipo académico EP02: **~70%** (dominio + API + tests referencia).
- Producción: no. Falta: EF Core/Postgres/Mongo + índices + TTL; **integración `RequiresAlert→AlertService`** (sin eventos MassTransit/RabbitMQ/Kafka, sin `HttpClient POST /alerts`, sin mapeo `RiskLevel→Severity`, sin reintentos/DLQ — hoy son dos silos); ingesta MQTT + buffer/batch + auth de dispositivo; modelo real ONNX + versionado + deriva; JWT/RBAC + rate-limit + FluentValidation + paginación + `GET ?riskLevel/desde/hasta` + health + OTel + Dockerfile/compose; baseline HRV por operador + ventana temporal + debounce + idempotencia.
