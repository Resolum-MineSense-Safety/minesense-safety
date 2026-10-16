
Orden sugerido de ejecución.

## P0 — Integridad y persistencia
- [ ] Validar `AssessmentId` en `Alert.cs:23-24` y `MonitoringSessionId` en `FatigueAssessment.cs:25-26` (`Guid.Empty→DomainException`).
- [ ] `BiometricSignals.cs:17-22`: rechazar `NaN/Infinity` (`!double.IsFinite→DomainException`) + cotas superiores realistas.
- [ ] `FatigueAssessmentQueryService.cs:17-21`: ordenar explícitamente en `GetLatest` (no confiar en repo).
- [ ] Validar `operatorId` query vacío en ambos controllers (`400/422` en vez de `200 []`).
- [ ] Añadir `EscalatedAt` a `Alert` + exponerlo en `AlertResource`.
- [ ] Definir semántica ack-tardío vs escalado y escalado manual temprano (¿permitido o solo automático?).
- [ ] EF Core + Postgres/Mongo: `DbContext`, migraciones, ctor privado para materialización, índices `(OperatorId,IssuedAt)`, `(Status,IssuedAt)`. Reemplazar `InMemoryRepository` (ya abstraído en `IBaseRepository`).
- [ ] Paginación en `List/FindBy*` + `page/pageSize` en controllers + filtro combinado `operatorId+status` / `riskLevel+fechas`.

## P1 — Integración entre BCs y edge
- [ ] Contrato `RiskLevel→AlertSeverity` + `RequiresAlert→IssueAlertCommand(AssessmentId)`. Decidir sync (HttpClient+reintentos) o async (outbox + RabbitMQ/Kafka/MassTransit + DLQ).
- [ ] `BackgroundService` para `EscalateOverdueAlerts` (timers 30s/2min) con idempotencia.
- [ ] MQTT: tópicos, subscriber en backend, buffer/batch telemetría, auth de dispositivo. Conectar `Mosquitto` de compose con C# y `src/edge/main.py`.
- [ ] Edge MVP: bucle + publicación telemetría simulada + inferencia local (scikit-learn/ONNX) + config.
- [ ] Idempotencia: `Idempotency-Key` o dedup `(sessionId,timestamp)` / `(OperatorId+AssessmentId)`.

## P2 — Prod mínimo
- [ ] Auth JWT + `[Authorize]` + roles operador/supervisor (desbloquea `IdentityAccessService`).
- [ ] `TimeProvider` sustituible en `WebApplicationFactory` para testear overdue-positivo vía API.
- [ ] Assemblers con tests aislados + casos borde (Severity ausente/null, status case-insensitive, doble ack/escalate vía API).
- [ ] `Dockerfile` por servicio + compose de app (servicios + volúmenes PG/Mongo + networks + healthchecks). Quitar password en claro, `version` obsoleto.
- [ ] Health `/health/live|ready`, CORS, Serilog/OTel, FluentValidation, rate-limit, `ProducesResponseType`, `ProblemDetails`.
- [ ] Eliminar/registrar proyectos fantasma (`MonitoringService`, `OperationsService`, `DeviceManagement`), sincronizar `.sln`, sacar `bin/obj` del repo y endurecer `.gitignore`.
- [ ] Infra real: Helm/K8s/Terraform + CD (build/push imágenes, deploy AKS/ACR, migraciones). Arreglar CI (disparar en push a main).
- [ ] Docs: ADRs (PG vs Mongo por BC, tópicos MQTT, estrategia IDs) + C4.
- [ ] Frontend: cliente API + proxy Vite + listado alertas/assessments + tiempo real (SignalR/SSE) + charts.
- [ ] Calibración: baseline HRV por operador, ventana temporal + suavizado + debounce críticos, `confidence`/`signalQuality`, versionado modelo.

## Definición de "100%"
- Académico EP02+EP03: P0 completo + overdue-positivo testeado E2E + docs mínimos.
- Producción: P0+P1+P2 + SLO overdue real (30s/2min) medido + sin secretos en claro + persistencia + auth + observabilidad.
