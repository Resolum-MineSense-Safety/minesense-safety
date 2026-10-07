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

## Visual design

The dashboard is designed for supervisors in a mine control room who must see, in two seconds,
who is at risk and act on it.

- **Palette ("survey sheet")**: limestone ground `#E8ECE6`, paper `#F6F7F3`, slate ink `#1D2A2E`
  and malachite `#1E6A5A` (copper ore) for actions. Risk colours appear only when someone is at
  risk: ochre for warning and signal red for critical; a normal state stays in neutral ink.
- **Risk by shape, not only colour**: normal is a ring, warning a diamond and critical a square of
  hazard tape, so it reads for colour-blind users and on washed-out screens. Hazard tape and the
  pulse animation are reserved for critical risk (the pulse respects `prefers-reduced-motion`).
- **Type**: one family, Archivo (variable width, self-hosted with `@fontsource-variable/archivo`).
  Expanded and heavy for the status sentence that opens each view, normal for text, condensed
  with tabular figures for dense table data. Times use the 24-hour clock.
- **Layout**: a slate rail with the sections, the shift and the clock; each view opens with a
  sentence that states the situation ("1 operador en riesgo crítico") followed by the work list.
