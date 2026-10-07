# MineSense Safety - Control Center Dashboard

React 19 + Vite + Tailwind CSS dashboard used by mine supervisors to monitor operator fatigue. The UI is in Spanish (Peru); the code is in English.

## Sections

- **Estado de flota**: fleet status table with combined filters (shift, fleet, location, risk level), Normal/Warning/Critical badges and a form to register monitored operators.
- **Alertas**: alerts by operator id, with acknowledge, escalate and open-incident actions.
- **Incidentes**: incidents by status; open, assign, register corrective action, escalate and close.
- **Simulador de fatiga**: sends PERCLOS, blink rate and HRV to the fatigue detection service and shows the resulting risk level.
- **Acceso**: sign-in / sign-up against the identity service.

## Backend services

| Proxy prefix | Service                   | Local port |
| ------------ | ------------------------- | ---------- |
| `/iam`       | IdentityAccessService     | 5150       |
| `/fatigue`   | FatigueDetectionService   | 5158       |
| `/alerts`    | AlertService              | 5233       |
| `/fleet`     | FleetMonitoringService    | 5255       |
| `/incidents` | IncidentManagementService | 5070       |

In development, `vite.config.js` proxies each prefix to its service and strips the prefix, so the backend needs no CORS configuration. Base URLs can be overridden with `VITE_*` variables (see `.env.example`). Business-rule errors (HTTP 422 `{ message }`) are shown to the user as returned by the backend.

## Scripts

```bash
npm ci          # install dependencies
npm run dev     # start the dev server with the API proxy
npm run lint    # ESLint (run in CI)
npm run build   # production build (run in CI)
```

## Structure

```
src/
  services/    one API module per bounded context + fetch wrapper (http.js)
  views/       one component per dashboard section
  components/  shared UI primitives (buttons, fields, badges, states)
  hooks/       useAsyncAction and GUID helpers
  i18n/        Spanish labels for backend enum values and date formatting
```
