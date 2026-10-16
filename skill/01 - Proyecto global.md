# 01 - Proyecto global MineSense Safety

## 1. Visión declarada
`src/backend/../README.md:1-13`: sistema integrado microservicios + IoT para detección y mitigación preventiva de fatiga en operadores de maquinaria pesada minera.

Estructura pretendida:
- `src/backend/`: microservicios C# .NET 8
- `src/edge/`: agente Python en cabina
- `src/frontend/`: dashboard React+Vite+Tailwind
- `infrastructure/`: Terraform + K8s/Helm
- `docs/`: C4 + ADRs
- `docker-compose.yml`: orquestación local

## 2. Estado real por componente

| Componente | Estado | % |
|---|---|---|
| FatigueDetectionService | Implementado slice in-memory (DDD/CQRS completo) | 70% |
| AlertService | Implementado slice in-memory (máquina estados + overdue) | 70% |
| FleetMonitoringService | Scaffold `dotnet new webapi` (weatherforecast en `Program.cs:24-37`). Sin referencia a Shared | 5% |
| IdentityAccessService | Igual, weatherforecast intacto | 5% |
| IncidentManagementService | Igual, weatherforecast intacto | 5% |
| MonitoringService / OperationsService | Solo carpetas `bin/obj`, sin `.csproj` ni `Program.cs`, fuera de `MineSenseSafety.sln` | 0% |
| DeviceManagementService | No existe como servicio; solo carpeta vacía en `tests/` | 0% |
| Shared kernel | 6 ficheros: `AggregateRoot`, `DomainException`, `IBaseRepository`, `InMemoryRepository (ConcurrentDictionary)`, `AddMineSenseWebApi()`, `KebabCaseRouteNamingConvention`, `DomainExceptionFilter → 422`. Lo mejor: unifica convenciones web. Falta: auth, eventos, EF, validación, OTel, healthchecks | 40% |
| tests/ | 8 carpetas, solo 2 con código: `AlertService.Tests`, `FatigueDetectionService.Tests`. Resto vacías | 25% |
| Edge `src/edge/main.py:1-3` | Stub 3 líneas `print(...)`. `requirements.txt` declara `paho-mqtt, scikit-learn, numpy, pytest` sin usarlos | 5% |
| Frontend `src/frontend/src/App.jsx:1-122` | Plantilla Vite (contador + hero). `package.json` solo `react, react-dom`. Sin router, HTTP client, charts, proxy Vite, ni llamadas API | 5% |
| `docker-compose.yml:1-25` | Solo `postgres:16-alpine`, `mongo:7.0`, `mosquitto:2.0`. Sin microservicios, volúmenes, networks, healthchecks. Password en claro `SecretPassword123!`. `version: '3.8'` obsoleto. Nada lo consume (todo es InMemory) | 15% |
| `infrastructure/helm,k8s,terraform` | Solo `.gitkeep` | 2% |
| `docs/adr,c4-diagrams` | Solo `.gitkeep`, pese a que README los anuncia | 2% |
| CI `.github/workflows/` | 3 workflows correctos: `dotnet-build-and-xunit`, `react-vite-build`, `sonarqube-analysis`. Gaps: solo corren en `PR→main/develop` y `push→develop` (push a main no dispara), sin CD (sin build/push imágenes, sin deploy), Sonar exige secrets | CI 60% / CD 0% |

**Global ponderado: ~20-25%.**

## 3. Gaps de integración (ninguno existe en código)

1. **API Gateway/BFF**: sin YARP/Ocelot. Cada servicio expondría puerto/Swagger propio.
2. **Eventos/mensajería**: sin broker, sin MassTransit. `RequiresAlert → IssueAlert` solo posible por HTTP manual. `POST overdue-escalations` es manual, no scheduler.
3. **MQTT Edge↔Cloud**: Mosquitto corre pero nadie se conecta. Sin tópicos, sin `MonitoringSession`.
4. **Auth/AuthZ**: sin JWT, sin `[Authorize]`, sin roles operador/supervisor. `IdentityAccessService` vacío.
5. **Persistencia**: 100% `InMemoryRepository` (`Shared/Infrastructure/Persistence/InMemory/InMemoryRepository.cs:14-32`). Pérdida al reiniciar. Sin EF Core/Npgsql, migraciones, índices `(OperatorId,IssuedAt)`. `appsettings.json` solo Logging.
6. **Observabilidad/ops**: sin Serilog, OTel, `/health`, CORS, rate-limit, `Dockerfile` por servicio, secrets management.
7. **Contratos entre BCs**: `OperatorId`, `AssessmentId`, `MonitoringSessionId` son `Guid` huérfanos sin FK ni validación cross-context. Sin idempotencia, sin paginación (`ListAsync` trae todo).

Grafo real: `Fatigue→Shared`, `Alert→Shared`, resto→nada. Frontend→nada. Edge→nada.

## 4. Deuda y riesgos

- **Alta**: persistencia ficticia; scaffolds divergentes sin Shared; sin auth; ML edge ausente (riesgo de alcance).
- **Media-Alta**: secreto en claro en compose; proyectos fantasma + `.sln` desincronizado; frontend desconectado (sin tiempo real SignalR/SSE); `overdue` sin SLA real.
- **Media**: `bin/obj` commiteados (`.gitignore` insuficiente); CI no corre en push a main + cobertura engañosa (solo 2 servicios); docs/IaC vacíos (decisiones PG vs Mongo, tópicos MQTT no registradas).
- **Baja-Media**: `AllowedHosts:*`, `UseHttpsRedirection` sin certs, sin CORS/health → fricción al contenerizar.
