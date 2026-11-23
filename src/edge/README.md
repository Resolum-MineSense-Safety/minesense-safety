# Agente Edge (gateway de cabina)

Agente Python que corre en el gateway de cada cabina. Genera señales biométricas sintéticas
(PERCLOS, parpadeo, HRV), clasifica el riesgo de fatiga **localmente** con los mismos umbrales que
`FatigueRiskPolicy` del Fatigue Detection Service y advierte al operador en la cabina. Todo evento
se guarda primero en un buffer SQLite durable y se sincroniza por lotes idempotentes con el
[Edge Synchronization Service](https://github.com/Resolum-MineSense-Safety/minesense-safety/tree/develop/src/backend/EdgeSynchronizationService)
(`POST /api/v1/synchronization-batches`).

Cubre **US13** (protección durante interrupciones de comunicación) y **US14** (recuperación del
historial pendiente) del EP06.

## Flujo

```
sensor sintético -> clasificador local -> advertencia en cabina (pantalla, buzzer)
                                       -> buffer SQLite (eventos en orden)
                         hilo de sincronización -> lote sellado (BatchId, SequenceNumber)
                                                -> Edge Synchronization Service (JWT de Identity & Access)
```

| Criterio | Cómo se cumple |
|---|---|
| US13 E1: se pierde la comunicación | La detección y las advertencias no usan la red; la sincronización corre en otro hilo y nunca bloquea una lectura. |
| US13 E2: fatiga sin comunicación | La advertencia se muestra en cabina y el evento `CabinWarningIssued` (más la lectura `FatigueReadingClassified`) queda en el buffer. |
| US13 E3: funciones no disponibles | Al perder la conexión se muestra una vez qué funciones dependen del centro de control. Si falla un medio de advertencia (p. ej. el buzzer), se avisa y se sigue advirtiendo por los demás; si fallan todos, el log CRITICAL es el último recurso. |
| US14 E1: recuperación | Al volver la conexión se envían los lotes pendientes en orden de `SequenceNumber` y se avisa en cabina. |
| US14 E2: envío interrumpido | Un lote se sella (BatchId fijo) antes de enviarse; si el envío falla, se reintenta el mismo lote con backoff exponencial. |
| US14 E3: sin duplicados | Reenviar un BatchId ya guardado devuelve `200 OK` (duplicado) en el servicio; el agente lo da por entregado. |

## Qué pasa después de sincronizar

Con Kafka configurado (`Kafka:BootstrapServers`), el Edge Synchronization Service publica el contenido
de cada lote nuevo, en orden de captura y con las horas originales. Un reintento del mismo lote no publica
nada, y si Kafka está caído el lote queda pendiente y se reintenta. Sin Kafka los lotes solo se guardan.

| Evento en cabina | Dónde termina | Qué no ocurre |
|---|---|---|
| `FatigueReadingClassified` | Historial de Fatigue Detection, como evaluación `Origin = Edge` con la hora de captura. | No publica `FatigueAssessed`: no genera una alerta nueva ni cambia el riesgo actual en Fleet Monitoring. |
| `CabinWarningIssued` | Alerts, como alerta `RecordedFromEdge` con la hora de la advertencia. Analytics y Feedback la registran, y si es `Critical` abre un incidente en el centro de control. | No se le vuelve a advertir al operador y no vence ni se escala por falta de confirmación. |

La advertencia lleva `readingEventId`, el `eventId` de su lectura: la nube usa ese id para la evaluación
y para la alerta, y así una lectura nunca genera dos alertas. Las decisiones y lo que falta están en
[ADR-004](https://github.com/Resolum-MineSense-Safety/minesense-safety/blob/develop/docs/adr/ADR-004-mqtts-edge-to-cloud-kafka-cloud-to-cloud.md).
**Pendiente:** el riesgo actual del panel de flota todavía no se actualiza con estas lecturas.

Un lote rechazado por inválido (400/422) pasa a la tabla `dead_letters` en lugar de perderse. Un
401/403 (token vencido o credenciales erróneas) **no** descarta eventos: se trata como falta de
conexión.

## Configuración (variables de entorno)

| Variable | Defecto | Descripción |
|---|---|---|
| `EDGE_SYNC_SERVICE_URL` | `http://localhost:5280` | URL del Edge Synchronization Service. |
| `IDENTITY_URL` | vacío | URL de Identity & Access para iniciar sesión. |
| `EDGE_USERNAME` / `EDGE_PASSWORD` | vacío | Credenciales del usuario del gateway (se piden a `/api/v1/authentication/sign-in` y se renuevan con el refresh token). |
| `EDGE_ACCESS_TOKEN` | vacío | Alternativa: token ya emitido. Sin token ni credenciales no se envía `Authorization`. |
| `EDGE_NODE_ID` / `EDGE_DEVICE_ID` | generados | GUID del nodo edge y del dispositivo; si no se dan se generan una vez y se guardan en el buffer. |
| `EDGE_OPERATOR_ID` / `EDGE_MONITORING_SESSION_ID` | generados | Operador y sesión de monitoreo de los eventos. |
| `EDGE_PROFILE` | `cycle` | `normal`, `drowsy`, `microsleep` o `cycle` (rota los tres). |
| `EDGE_CYCLE_LENGTH` | `10` | Lecturas por perfil en modo `cycle`. |
| `EDGE_INTERVAL_SECONDS` | `2` | Segundos entre lecturas. |
| `EDGE_MAX_ITERATIONS` | `0` | Lecturas antes de terminar (`0` = sin fin). |
| `EDGE_BUFFER_PATH` | `data/edge-buffer.db` | Archivo SQLite del buffer (`/app/data/edge-buffer.db` en el contenedor). |
| `EDGE_MAX_BUFFERED_EVENTS` | `100000` | Tope del buffer; al llenarse se descartan primero lecturas normales, nunca antes que advertencias. |
| `EDGE_BATCH_SIZE` | `100` | Eventos por lote (1 a 500, límite del servicio). |
| `EDGE_MAX_BATCHES_PER_TICK` | `5` | Lotes por intento de sincronización. |
| `EDGE_RETRY_INITIAL_SECONDS` / `EDGE_RETRY_MAX_SECONDS` | `2` / `60` | Backoff exponencial sin conexión. |
| `EDGE_HTTP_TIMEOUT_SECONDS` | `3` | Timeout HTTP. |
| `EDGE_UNAVAILABLE_WARNING_CHANNELS` | vacío | Simula medios de advertencia caídos: `buzzer`, `display`. |
| `MQTT_HOST` / `MQTT_PORT` / `MQTT_TOPIC` | vacío / `1883` / `minesense/cabins/<device>/signals` | Telemetría opcional por MQTT. |
| `EDGE_RANDOM_SEED` | vacío | Semilla para señales reproducibles. |
| `LOG_LEVEL` | `INFO` | Nivel de log. |

Las credenciales nunca van en el código ni en la imagen: se pasan por entorno (`.env` local,
Secret en Kubernetes).

## Ejecutar

```bash
cd src/edge
python -m venv .venv && . .venv/bin/activate      # Windows: .venv\Scripts\activate
pip install -r requirements-dev.txt
python -m pytest                                   # pruebas unitarias y escenarios offline -> reconexión
EDGE_SYNC_SERVICE_URL=http://localhost:5280 python main.py
```

Con Docker Compose (perfil `simulator`, junto con el resto del entorno local):

```bash
# 1. Registrar el usuario del gateway (rol Operator) en Identity & Access
curl -X POST http://localhost:5202/api/v1/authentication/sign-up -H "Content-Type: application/json" \
  -d '{"username":"edge-cabin-01","fullName":"Gateway cabina 01","password":"<clave>","role":"Operator"}'
# 2. Poner EDGE_USERNAME y EDGE_PASSWORD en .env y levantar el agente
docker compose --profile simulator up --build -d edge-agent
docker compose logs -f edge-agent
```

Para probar US13 se puede detener `edge-synchronization-service`: el agente sigue advirtiendo, avisa
la pérdida de comunicación y acumula eventos; al volver a levantarlo sincroniza los pendientes.

## Pruebas

`tests/` usa pytest y un servidor HTTP falso en proceso (`tests/fake_cloud.py`) que imita a
Identity & Access y al Edge Synchronization Service (202 lote nuevo, 200 duplicado, 400 inválido,
401 sin token) y simula caída (503), puerto cerrado y respuesta perdida después de guardar el lote.
El workflow `edge-agent-ci.yml` las ejecuta en cada cambio de `src/edge/**` y además construye la
imagen, la ejecuta endurecida y sin red, verifica que no corre como root y la escanea con Trivy.
