# Supervision Dashboard (MineSense Safety)

Centro de control web de MineSense Safety: React 19, Vite y Tailwind CSS. Consume los 13
microservicios a través del gateway (`/api/v1`) con el token de Identity & Access.

| Pantalla | Ruta | Microservicios |
|---|---|---|
| Centro de control | `/` | Fleet Monitoring, Alerts, Incident Management, Device Management, Monitoring, Fatigue Detection |
| Flota | `/flota` | Fleet Monitoring, Fatigue Detection |
| Cabina | `/cabina` | Device Management, Monitoring, Fatigue Detection, Alerts, Fleet Monitoring, Analytics, Feedback & Reliability, Incident Management, Privacy & Audit |
| Alertas | `/alertas` | Alerts, Feedback & Reliability |
| Incidentes | `/incidentes` | Incident Management, Privacy & Audit |
| Operaciones | `/operaciones` | Operations |
| Dispositivos | `/dispositivos` | Device Management |
| Edge | `/edge` | Edge Synchronization |
| Tendencias | `/analitica` | Analytics |
| Fiabilidad | `/fiabilidad` | Feedback & Reliability |
| Reportes | `/reportes` | Reporting |
| Privacidad | `/privacidad` | Privacy & Audit |
| Usuarios | `/usuarios` | Identity & Access |
| Servicios | `/servicios` | Los 13: una lectura por servicio y su latencia |

## Ejecutar en local

1. Levantar el backend. Con Docker: `docker compose up --build -d` desde la raíz (gateway en
   `http://localhost:8080`). Sin Docker: `dotnet run` de cada servicio con
   `Authentication__IdentityUrl=http://localhost:5150` (puertos de `Properties/launchSettings.json`).
2. Instalar y arrancar el dashboard:

   ```bash
   npm ci
   npm run dev                                  # proxy a los puertos de dotnet run
   GATEWAY_URL=http://localhost:8080 npm run dev # proxy al gateway de docker compose
   ```

   El proxy de Vite lee las rutas de `infrastructure/helm/minesense-gateway/values.yaml`, así que
   en local se enruta igual que el ALB de AWS.
3. Cargar datos de demostración una sola vez sobre un entorno vacío:

   ```bash
   DEMO_PASSWORD=<contraseña-de-demo> node scripts/seed-demo.mjs
   ```

   Crea la unidad minera Tambo Real con 8 cabinas, dispositivos, usuarios, consentimientos,
   alertas, incidentes, feedback, tendencias, reportes y un lote Edge. Cuentas: `admin` (contraseña
   del perfil Development) y `supervisor`, `jefe.seguridad` y `operador.rosa` con la contraseña de
   `DEMO_PASSWORD`, que nunca se guarda en el repositorio.

## Diseño

- Paleta clara: fondo mineral `#F3F5F4`, tinta basalto `#1F2A2E`, acento amarillo de camión
  minero `#F2B705` y semáforo de riesgo (verde, ámbar, rojo) que significa lo mismo en todas las
  pantallas.
- Tipografía Barlow (interfaz) y Barlow Condensed (cifras y títulos).
- Poco texto: estados como píldoras de color, cifras grandes, botones en forma de píldora y
  formularios en paneles laterales.
- Componentes comunes en `src/components/ui.jsx`; llamadas a la API por bounded context en
  `src/api/services.js`.
