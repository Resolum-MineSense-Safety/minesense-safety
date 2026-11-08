# SLIs y SLOs de MineSense Safety

Sección 4.3 del informe. Los SLO salen de los escenarios de atributos de calidad de la sección 2.1.1
(QAS 02, QAS 04 y QAS 06). Las reglas que los calculan y las alertas que los vigilan están en
[`infrastructure/observability/rules/minesense-slo.rules.yml`](../../infrastructure/observability/rules/minesense-slo.rules.yml).
El dashboard que los muestra es
[`infrastructure/observability/dashboards/minesense-slo.json`](../../infrastructure/observability/dashboards/minesense-slo.json).

## Fuente de los datos

Cada microservicio .NET publica `/metrics` en formato Prometheus (`prometheus-net`, registrado una sola
vez en `Shared/Interfaces/ASP/Configuration/MetricsStartupFilter.cs`). El histograma
`http_request_duration_seconds{job, code, method}` es la base de todos los SLI.

- **En EKS:** el chart `minesense-service` crea un `ServiceMonitor` por servicio. `kube-prometheus-stack`
  lo raspa cada 30 s. Los logs de los contenedores van a CloudWatch Logs con el add-on
  `amazon-cloudwatch-observability`.
- **En local:** `docker-compose.observability.yml` levanta Prometheus y Grafana con las mismas reglas y
  el mismo dashboard.

## SLIs y SLOs

| ID | SLI | SLO (ventana de 30 días) | Origen | Alerta |
|---|---|---|---|---|
| SLO-1 | % de peticiones de la API sin respuesta 5xx | ≥ 99.5 % (presupuesto: 0.5 %, unas 3.6 h al mes) | Disponibilidad | `MineSenseErrorBudgetFastBurn` (crítica), `MineSenseErrorBudgetSlowBurn` (advertencia) |
| SLO-2 | P95 de la latencia de `fatigue-detection-service` y `alert-service` | ≤ 500 ms | QAS 02 | `MineSenseCriticalPathSlow` (crítica) |
| SLO-3 | P95 de la latencia del resto de la API | ≤ 2 s | QAS 06 | `MineSenseApiSlow` (advertencia) |
| SLO-4 | % de peticiones sin token válido que se rechazan (401/403) | 100 % | QAS 04 | Se prueba en CI (`AuthenticationTests`, `NFR02`). `MineSenseUnauthorizedSpike` avisa de un posible ataque |
| — | Targets arriba (`up`) | 13 de 13 | Disponibilidad | `MineSenseServiceDown` (crítica) |
| — | Reinicios y saturación en Kubernetes | Sin CrashLoop; HPA por debajo de su máximo | Operación | `MineSensePodCrashLooping`, `MineSenseHpaAtMaximum` |

### Por qué alertas de *burn rate*

Avisar cuando «hay un 5xx» genera ruido. Las alertas del SLO-1 siguen el modelo multi-ventana del
*SRE Workbook* de Google:

- **Quema rápida** (`14.4×` el presupuesto, ventanas de 1 h y 5 min): si sigue así, el presupuesto del mes
  se agota en unos dos días. Avisa al responsable de guardia.
- **Quema lenta** (`6×`, ventanas de 6 h y 30 min): si sigue así, se agota en unos cinco días. Va al canal
  del equipo.

## Rutas de notificación

Alertmanager (valores en `infrastructure/observability/kube-prometheus-stack.values.yaml`):

| Severidad | Receptor | Repetición |
|---|---|---|
| `critical` | `oncall`: webhook del responsable de guardia | cada 30 min mientras siga activa |
| `warning` | `team`: webhook del canal del equipo | cada 4 h |

Las URL de los webhooks están en el Secret `monitoring/alertmanager-webhook`, nunca en Git. Toda alerta
trae un `runbook_url` hacia [runbook.md](runbook.md).
