# 09 - Ejemplos para probar en Swagger (copiar y pegar)

Requisito: ambos servicios corriendo. Swagger: `http://localhost:5158/swagger/index.html` (chequeo) y `http://localhost:5233/swagger/index.html` (alarma). En cada endpoint: **Try it out → pegar → Execute**. Abajo de cada prueba Swagger muestra el código y la respuesta.

Datos base para pegar (usa siempre los mismos para ver el historial crecer):
- `operatorId`: `3fa85f64-5717-4562-b3fc-2c963f66afa6`
- `monitoringSessionId` / `assessmentId`: `7c9e6679-7425-40de-944b-e07fc1f90ae7`

---

## PARTE A — Chequeo de cansancio (puerto 5158)

### A1. `POST /api/v1/fatigue-assessments` — caso Normal 🟢
Body:
```json
{
  "operatorId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "monitoringSessionId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "perclos": 0.05,
  "blinkRatePerMinute": 12,
  "heartRateVariabilityMs": 60
}
```
Esperado: **201**, `"riskLevel": "Normal"`, `"requiresAlert": false`. **Copia el `id`** (lo usarás en A4).

### A2. `POST /api/v1/fatigue-assessments` — caso Warning 🟡
Body: igual que A1 pero `"perclos": 0.20`. Esperado: **201**, `"riskLevel": "Warning"`, `"requiresAlert": true`.

### A3. `POST /api/v1/fatigue-assessments` — caso Critical 🔴
Body: igual que A1 pero `"perclos": 0.28, "blinkRatePerMinute": 22, "heartRateVariabilityMs": 18`. Esperado: **201**, `"riskLevel": "Critical"`, `"requiresAlert": true`. **Copia este `id`** también.

### A4. `GET /api/v1/fatigue-assessments/{fatigueAssessmentId}` — ver uno
Parámetro: pega el `id` copiado en A1. Esperado: **200** con ese chequeo. Prueba con `00000000-0000-0000-0000-000000000000` → **404**.

### A5. `GET /api/v1/fatigue-assessments?operatorId=...` — historial
Parámetro `operatorId`: `3fa85f64-5717-4562-b3fc-2c963f66afa6`. Esperado: **200**, array con tus 3 chequeos (el más nuevo primero).

### A6. `GET /api/v1/fatigue-assessments/latest?operatorId=...` — el último
Mismo `operatorId`. Esperado: **200** con el Critical de A3. Con un operador inventado (`11111111-1111-1111-1111-111111111111`) → **404**.

### A7. `GET /api/v1/fatigue-assessments/sessions/{monitoringSessionId}` — por turno
Parámetro: `7c9e6679-7425-40de-944b-e07fc1f90ae7`. Esperado: **200**, tus 3 chequeos en orden de hora.

### A8. `POST` inválido — debe fallar con 422
Body: igual que A1 pero `"perclos": 1.7`. Esperado: **422** `{"message":"PERCLOS must be between 0 and 1."}`. Prueba también `"operatorId": "00000000-0000-0000-0000-000000000000"` → **422** `{"message":"A fatigue assessment must belong to an operator."}`.

---

## PARTE B — Alarma en cabina (puerto 5233)

### B1. `POST /api/v1/alerts` — crear alarma grave
Body:
```json
{
  "operatorId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "assessmentId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "severity": "Critical"
}
```
Esperado: **201**, `"status": "Issued"`, `"acknowledgedAt": null`. **Copia el `id`** (lo usarás en B4–B6).

### B2. `POST /api/v1/alerts` — crear alarma leve
Mismo body pero `"severity": "Warning"`. Esperado: **201** `Issued`. **Copia este `id`** (para B7).

### B3. Lecturas — ver lo creado
- `GET /api/v1/alerts/{alertId}` con el id de B1 → **200**.
- `GET /api/v1/alerts?operatorId=3fa85f64-5717-4562-b3fc-2c963f66afa6` → **200**, tus alarmas (nueva primero).
- `GET /api/v1/alerts/status/issued` → **200**, las que siguen sonando. Prueba `status/ESCALATED` (mayúsculas valen) y `status/pendiente` → **422** `{"message":"Unknown alert status 'pendiente'."}`.

### B4. `POST /api/v1/alerts/{alertId}/acknowledgements` — confirmar ✅
Parámetro: id de B1 → **200**, `"status": "Acknowledged"` con hora. Repítelo con el mismo id → **422** (ya no está sonando).

### B5. `POST /api/v1/alerts/{alertId}/escalations` — escalar a mano ⏫
Parámetro: id de B2 → **200**, `"status": "Escalated"`. Si lo intentas con el id de B1 (ya confirmada) → **422**.

### B6. `POST /api/v1/alerts/overdue-escalations` — barrido de vencidas
Sin parámetros (solo Execute). Recién creadas → **200** `[]`. Para verlo actuar: crea otra Critical (B1), **espera 35 segundos**, ejecuta el barrido → **200** con esa alarma, y verifícala en `GET status/escalated`. (Una Warning necesita 2 min + 1 s.)

### B7. `POST` inválido — debe fallar
- `"severity": "Grave"` → **422** `{"message":"Unknown alert severity 'Grave'."}`.
- Sin la línea de `severity` → **422** `{"message":"Unknown alert severity ''."}`.
- `"operatorId": "00000000-0000-0000-0000-000000000000"` → **422** `{"message":"An alert must belong to an operator."}`.
- Ack/escalate con id `00000000-0000-0000-0000-000000000000` → **404**.

---

## Orden sugerido de la demo completa (10 min)

A1 → A2 → A3 → A5 (historial de 3) → A6 (último = Critical) → B1 (alarma grave) → B3 (cola `issued`) → B4 (confirmar) → repetir B4 (ver el 422) → B6 (barrido, vacío) → B1 + esperar 35 s + B6 (escalada real) → `status/escalated` (la ve el supervisor).
