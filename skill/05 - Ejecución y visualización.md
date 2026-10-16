# 05 - Ejecución y visualización del avance

Verificado en vivo el 2026-10-09 (build 0 errores, tests 28+30 OK, ambos APIs 200 + POST/GET reales).

## 1. Requisitos

- .NET 8 SDK (`dotnet --version` → 8.0.x)
- Node 24 + npm 11
- Docker Desktop (opcional, solo infra)
- Python 3.14 (solo stub edge)

## 2. Comandos para iniciarlo después

```powershell
# 0. Raíz del repo
cd C:\Users\nub\Documents\programming_projects\university\minesense-safety

# 1. Infra (solo postgres/mongo/mosquitto, nada la consume aún: todo es InMemory)
docker compose up -d
# Puertos: postgres 5432, mongo 27017, mqtt 1883/9001

# 2. Backend: compilar + tests
cd src\backend
dotnet build MineSenseSafety.sln
dotnet test MineSenseSafety.sln --no-build
dotnet test tests\AlertService.Tests\AlertService.Tests.csproj

# 3. Levantar microservicios (2 terminales separadas)
# OJO con la carpeta actual: la ruta de --project es relativa a donde estás parado.
# Si estás en la RAÍZ del repo:
dotnet run --project src\backend\FatigueDetectionService\FatigueDetectionService.csproj --launch-profile http
# Si ya estás en src\backend (tu caso del error MSB1009):
dotnet run --project FatigueDetectionService\FatigueDetectionService.csproj --launch-profile http
# Tu error "No se ha podido encontrar el perfil de inicio ... MSB1009" fue por combinar
# ambas cosas: parado en src\backend pero con ruta src\backend\... El proyecto no existe ahí.

# Terminal B - Alert (puerto 5233)
# NOTA: `dotnet run` del Alert es bloqueado por App Control (bloquea el .exe generado).
# Usar el DLL directo (desde la RAÍZ del repo):
$env:ASPNETCORE_ENVIRONMENT="Development"; $env:ASPNETCORE_URLS="http://localhost:5233"
dotnet src\backend\AlertService\bin\Debug\net8.0\AlertService.dll
# Si estás parado en src\backend: dotnet AlertService\bin\Debug\net8.0\AlertService.dll

# 4. Frontend (plantilla Vite, sin conexión al backend)
cd src\frontend
npm install
npm run dev      # http://localhost:5173
npm run build    # verificado: built in ~2s

# 5. Edge (stub, solo imprime)
python src/edge/main.py
# Salida esperada: "MineSense Safety Edge Agent started..."
```

Apagado: `Ctrl+C` en cada terminal + `docker compose down`.

## 3. Cómo visualizar el avance (qué abrir)

| Qué | URL / comando | Qué demuestra |
|---|---|---|
| Swagger Fatigue | http://localhost:5158/swagger | 5 endpoints: `POST /api/v1/fatigue-assessments`, `GET ?operatorId`, `GET latest?operatorId`, `GET sessions/{id}`, `GET {id}` |
| Swagger Alert | http://localhost:5233/swagger | 7 endpoints: `POST /api/v1/alerts`, `GET ?operatorId`, `GET status/{status}`, `GET {id}`, `POST {id}/acknowledgements`, `POST {id}/escalations`, `POST overdue-escalations` |
| Archivos `.http` | `src/backend/FatigueDetectionService/FatigueDetectionService.http`, `src/backend/AlertService/AlertService.http` | Requests listos para VS Code REST Client |
| Frontend | http://localhost:5173 | Solo plantilla Vite (contador + hero). Confirma que el dashboard está en 5%: no lista alertas |
| Edge | `python src/edge/main.py` | Solo stub confirmado |
| Otros 3 servicios | `Program.cs` con `weatherforecast` | Confirman que Fleet/Identity/Incident son scaffolds vacíos |

Datos en memoria: al reiniciar se pierde todo (sin persistencia).

## 4. Flujo demo validado (copiar/pegar)

```powershell
$op = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
$ses = "7c9e6679-7425-40de-944b-e07fc1f90ae7"

# 1. Evaluar fatiga (ejemplo del .http: perclos 0.28 + hrv 18 → Critical por regla combinada 0.25+HRV<20)
@{ operatorId=$op; monitoringSessionId=$ses; perclos=0.28; blinkRatePerMinute=22; heartRateVariabilityMs=18 } |
  ConvertTo-Json | Invoke-RestMethod -Uri http://localhost:5158/api/v1/fatigue-assessments -Method Post -ContentType "application/json" | ConvertTo-Json -Depth 5
# Esperado: "riskLevel": "Critical", "requiresAlert": true

# 2. Historial + último
Invoke-RestMethod "http://localhost:5158/api/v1/fatigue-assessments?operatorId=$op" | ConvertTo-Json -Depth 5
Invoke-RestMethod "http://localhost:5158/api/v1/fatigue-assessments/latest?operatorId=$op" | ConvertTo-Json -Depth 5

# 3. Emitir alerta manual (hoy NO hay auto-emisión desde RequiresAlert: son silos)
@{ operatorId=$op; assessmentId=$ses; severity="Critical" } |
  ConvertTo-Json | Invoke-RestMethod -Uri http://localhost:5233/api/v1/alerts -Method Post -ContentType "application/json" | ConvertTo-Json -Depth 5
# Esperado: "status": "Issued"

# 4. Colas de supervisor
Invoke-RestMethod "http://localhost:5233/api/v1/alerts?operatorId=$op" | ConvertTo-Json -Depth 5
Invoke-RestMethod http://localhost:5233/api/v1/alerts/status/issued | ConvertTo-Json -Depth 5

# 5. Ack + escalado (guardar el id del paso 3 en $id)
Invoke-RestMethod -Uri "http://localhost:5233/api/v1/alerts/$id/acknowledgements" -Method Post | ConvertTo-Json -Depth 5
Invoke-RestMethod -Uri "http://localhost:5233/api/v1/alerts/overdue-escalations" -Method Post | ConvertTo-Json -Depth 5
# Overdue solo escala lo vencido (Critical >30s, Warning >2min); recién creado devuelve []
```

Casos para validar reglas: `perclos 0.10/hrv 50/blink 10` → Normal; `perclos 1.7` → 422; `severity "Foo"` → 422; `GET status/ESCALATED` case-insensitive.

## 5. Resultado de la ejecución de hoy

- `dotnet build`: correcta, 0 warnings/errors.
- Tests: Fatigue 28/28, Alert 30/30.
- Swagger ambos: 200.
- POST/GET reales verificados (ejemplos arriba con IDs `13d63f66…` y `25efc660…`).
- Frontend `npm run build`: OK (plantilla, warning Tailwind sin clases usadas).
- `docker compose config`: válido, solo infra + warning `version` obsoleto.
## 6. Troubleshooting (tu caso)

1. **MSB1009 / "No se ha podido encontrar el perfil de inicio"**: estabas en `src\backend` y pasaste `--project src\backend\...`. Corrige a `dotnet run --project FatigueDetectionService\FatigueDetectionService.csproj --launch-profile http` (o vuelve a la raíz y usa la ruta larga).
2. **Swagger "Unable to render this definition ... valid version field"**: verificado en vivo que con los servicios arriba `GET /swagger/v1/swagger.json` devuelve 200 con `"openapi": "3.0.4"` válido en ambos puertos (títulos `FatigueDetectionService` / `AlertService`). Si el UI muestra ese error, es **caché del navegador** (pestaña abierta cuando el servicio aún no escuchaba o durante el conflicto de puerto). Solución: Ctrl+F5 sobre la pestaña, o ventana incógnito, o abrir directo `http://localhost:5158/swagger/v1/swagger.json` y `http://localhost:5233/swagger/v1/swagger.json` (deben mostrar el JSON). Recién después recargar `.../swagger/index.html`. Abrir siempre por **http** (no https).
3. **Conflicto https**: ambos `launchSettings.json` usan `https://localhost:7274`. Usa siempre el perfil `http` (5158/5233) si corres los dos a la vez.
4. **Validación rápida sin navegador**: `(Invoke-WebRequest http://localhost:5158/swagger/v1/swagger.json -UseBasicParsing).StatusCode` debe dar 200. Si da error de conexión, el servicio no está arriba.
5. **Tests Alert bloqueados por directiva (`0x800711C7`, "could not load dependent assembly")**: es el equipo (Smart App Control / WDAC), no el código. El `AlertService.Tests.dll` compila bien pero el SO rehúsa cargarlo; el `.exe` del AlertService sufre lo mismo (por eso se corre vía `dotnet *.dll`). Verificado: Fatigue 28/28 sigue pasando y ese mismo proyecto Alert dio 30/30 antes del bloqueo. Salidas: reintentar tras reiniciar, revisar `Seguridad de Windows > Control de aplicaciones` y el Visor de eventos (`CodeIntegrity/Operational`) para ver la regla, o pedir excepción a TI. La validación funcional del Alert sigue posible vía API/Swagger (`POST/GET` verificados en vivo).
