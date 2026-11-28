# Simulador IoT de flota (gateways de cabina)

Un gateway virtual por cada camión de la mina de demostración. Reemplaza el ingreso manual de
lecturas: **ningún valor de sensor se escribe desde el dashboard**; todos salen de este proceso como
caminatas aleatorias con la fisiología de un operador que se cansa, hace pausas y es relevado.

| Cada | Qué hace | Contexto que lo recibe |
|---|---|---|
| 1 s | Muestra de la cámara (PERCLOS, parpadeos, microsueños) y del wearable (pulso, HRV, batería); velocidad del camión | MQTT `minesense/<mina>/cabins/<camión>/telemetry` (Mosquitto) y la API de telemetría |
| 30 s (`ASSESS_SECONDS`) | Promedio de la ventana como evaluación de fatiga | Fatigue Detection → Kafka → Fleet Monitoring, Alerts, Analytics, Feedback, Incident Management |
| 30 s | Estado y batería de la cámara y el wearable | Device Management |
| Al azar | Pérdida y recuperación de señal de la cámara o del wearable | Monitoring |
| 70-160 s | El camión avanza por la ruta Tajo Norte → Rampa 3 → Chancadora 1 → Botadero Este | Fleet Monitoring |
| Al azar, en Tajo Norte o Botadero Este | La cabina pierde cobertura: clasifica en local con los umbrales de `FatigueRiskPolicy`, advierte en cabina y guarda `FatigueReadingClassified` / `CabinWarningIssued`; al volver la red envía el lote y lo reenvía para probar la idempotencia | Edge Synchronization (US13, US14) |

Perfiles: cada cuarta cabina está **cansada** o **somnolienta** y las demás **descansadas**, así la demo
muestra advertencias cada pocos minutos y de vez en cuando un evento crítico con incidente. Tras una
alerta crítica el operador es relevado en uno o dos minutos y su fatiga vuelve a empezar.

Al arrancar espera al gateway, carga la mina con `seed-demo.mjs` si el entorno está vacío (necesita
`DEMO_PASSWORD`) y abre la sesión de monitoreo de cada cabina que pasa el pre-turno y tiene
consentimiento. Si un supervisor finaliza una sesión, esa cabina deja de transmitir hasta que se
inicie otra desde la pantalla Cabina.

## API para el dashboard (solo lectura, token de Identity & Access)

| Ruta | Respuesta |
|---|---|
| `GET /api/v1/telemetry/cabins` | Estado del gateway y de cada cabina con los últimos 180 s (`?history=short`: 60 s) |
| `GET /api/v1/telemetry/cabins/{operatorId}` | Una cabina |
| `GET /api/v1/telemetry/events?operatorId=` | Últimos eventos de los gateways (señales, cobertura, microsueños, relevos) |
| `GET /api/v1/telemetry/edge-batches` | Lotes edge enviados, con el estado del envío y del reintento |
| `GET /api/v1/telemetry/status` | Contadores: MQTT, evaluaciones, alertas, lotes |
| `GET /health/live`, `/health/ready` | Probes |

El token se valida igual que en los servicios .NET (ADR-007): firma RS256 con el JWKS de Identity &
Access, emisor, audiencia y vencimiento.

## Variables

| Variable | Defecto | Uso |
|---|---|---|
| `GATEWAY_URL` | `http://gateway:8080` | Entrada única `/api/v1` |
| `SIM_USERNAME` / `SIM_PASSWORD` | `admin` / perfil Development | Cuenta con la que publica el gateway |
| `DEMO_PASSWORD` | vacío | Contraseña de los usuarios de la demo al cargar la mina |
| `ASSESS_SECONDS` | `30` | Ventana de cada evaluación |
| `SIM_INTENSITY` | `1` | Multiplica la velocidad con que se cansan los operadores |
| `MQTT_HOST` / `MQTT_PORT` | `mosquitto` / `1883` | Broker; vacío lo desactiva |
| `AUTO_SEED` | `true` | Cargar la mina si no hay unidades mineras |

```bash
docker compose up --build -d iot-simulator        # con el resto del entorno
docker compose logs -f iot-simulator
node --test                                        # clasificador local = FatigueRiskPolicy
```

No tiene dependencias npm: Node 20+ y su propio cliente MQTT 3.1.1 (`mqtt.mjs`). El agente Python de
`src/edge` sigue siendo el gateway real de una cabina (perfil `simulator` del compose).
