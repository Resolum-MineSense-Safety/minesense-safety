# Pruebas avanzadas (sección 4.4)

Mapa de las pruebas del proyecto: qué se prueba, con qué herramienta, dónde está el código, qué
workflow las ejecuta y cómo correrlas. Los resultados con cifras salen en cada ejecución del CI
(artefactos del workflow y resumen del job); este documento no copia números que cambian en cada corrida.

| Sección | Tipo | Herramienta | Código | Workflow |
|---|---|---|---|---|
| 4.4.1 | End-to-end y de navegador | Playwright | `tests/e2e` | `.github/workflows/e2e-tests.yml` |
| 4.4.2 | Integración con terceros (AWS) | xUnit + Testcontainers + LocalStack | `src/backend/tests/ThirdPartyIntegrationTests` | `.github/workflows/third-party-integration.yml` |
| 4.4.3 | Rendimiento | k6 | `tests/performance/k6` | `.github/workflows/performance.yml` |
| 4.4.4 | Seguridad dinámica (DAST) | OWASP ZAP | `tests/security/zap` | `.github/workflows/dast-zap.yml` |
| 4.4.5 | Accesibilidad | axe-core (WCAG 2.1 AA) y Lighthouse | `tests/e2e/tests/accessibility.spec.js`, `tests/e2e/scripts/lighthouse.mjs` | `.github/workflows/e2e-tests.yml` |
| 4.8 | Aceptación (Gherkin) | Reqnroll (backend) y playwright-bdd (dashboard) | `src/backend/tests/AcceptanceTests`, `tests/e2e/features` | `.github/workflows/acceptance-tests.yml`, `e2e-tests.yml` |

## 4.4.1 End-to-end y de navegador

- Proyectos de Playwright (`tests/e2e/playwright.config.js`): `chromium`, `firefox` y `webkit` (escritorio),
  `tablet-webkit` (iPad, solo `responsive.spec.js`) y `bdd-chromium` y `bdd-firefox` (escenarios Gherkin).
  Cada proyecto corre como un job distinto, así que el workflow ejecuta la suite en tres motores de navegador.
- Pruebas agnósticas del navegador: `auth`, `navigation` y `alerts-flow`. Específicas: `browser-specific` y `responsive`.
- El job levanta el stack con `docker compose`, siembra datos con `src/frontend/scripts/seed-demo.mjs` y ejecuta la suite.

## 4.4.2 Integración con terceros

- `ReportingAwsIntegrationTests` prueba Reporting Service contra Amazon SQS y S3 emulados por LocalStack en un
  contenedor (Testcontainers), con las peticiones reales del SDK de AWS.
- El workflow solo corre cuando cambian `ReportingService`, estas pruebas o el propio workflow.

## 4.4.3 Rendimiento

- Escenarios: `smoke.js`, `load.js` y `stress.js` (`tests/performance/k6`).
- Los umbrales de k6 son los SLO de [docs/operations/slo.md](https://github.com/Resolum-MineSense-Safety/minesense-safety/blob/develop/docs/operations/slo.md): un SLO roto hace fallar el job.
- `stress.js` busca el punto de quiebre: el paso de k6 se marca `continue-on-error` para que cruzar sus umbrales no
  bloquee un pull request. El resumen queda en el artefacto `k6-stress`.
- Contra el entorno desplegado: ejecutar el workflow a mano con `base_url` del ALB de Staging.

## 4.4.4 Seguridad dinámica

- Escaneo base pasivo del dashboard y escaneo activo de las APIs descritas por OpenAPI (Alerts y Fatigue Detection).
- `tests/security/zap/rules.tsv` decide qué hallazgos rompen el pipeline: solo las reglas marcadas `FAIL`; el resto
  queda como advertencia. Detalle y mapeo a OWASP Top 10 en [tests/security/zap/README.md](https://github.com/Resolum-MineSense-Safety/minesense-safety/blob/develop/tests/security/zap/README.md).

## 4.4.5 Accesibilidad

- `accessibility.spec.js` revisa con axe-core las páginas del dashboard contra WCAG 2.1 AA y deja el informe JSON en `tests/e2e/reports/axe`.
  La línea base previa a las correcciones está en `tests/e2e/reports/axe-baseline`.
- `lighthouse.mjs` puntúa las páginas con Lighthouse (solo Chromium).
- La historia RNF04 tiene su escenario en `tests/e2e/features/RNF04_accesibilidad_dashboard.feature`.

## Cómo correrlas

```powershell
# End-to-end (con el stack arriba: docker compose up -d --build)
cd tests/e2e; npm ci; npx playwright install; npx playwright test

# Rendimiento (k6 instalado)
k6 run -e BASE_URL=http://localhost:8080 tests/performance/k6/load.js

# Integración con LocalStack (Docker en marcha)
dotnet test src/backend/tests/ThirdPartyIntegrationTests
```

## Preguntas abiertas

- El escaneo activo de ZAP cubre dos APIs; el resto de los servicios solo pasa por las pruebas de seguridad del CI.
- Las pruebas de rendimiento corren contra el compose del runner; la comparación con el autoescalado en EKS exige un
  Staging desplegado (cuesta dinero por hora) y no está ejecutada.
