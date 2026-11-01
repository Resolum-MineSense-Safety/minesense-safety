# ADR-010: Reporting Service y su frontera con Privacy & Audit (EP10)

## Estado

Propuesta (rama `feature/ep10-reporting-scaffold`).

## Contexto

EP10 asigna los reportes a Reporting Service, pero TS-21 (`POST /api/v1/reports/safety`,
`GET /api/v1/reports/{id}`) ya está implementado en PrivacyAuditService como registro
evidencial. ADR-005 asigna MongoDB a este servicio ("vistas de lectura"); ADR-008 delega
los PDFs pesados a Lambda; ADR-004/Kafka lo declara consumidor vital de eventos.

## Decisión

- Reporting Service es dueño de: catálogo versionado de indicadores (`ReportDefinition`),
  jobs async (`ReportJob`), artefactos con checksum y expiración (`ReportArtifact`),
  generación/entrega (`GET download`, Lambda en Fase 4).
- PrivacyAuditService conserva su intake evidencial (`/api/v1/reports/safety`); no se duplica:
  solo referencias lógicas (`reportId`), sin DB compartida (ADR-005).
- Migración total de `/api/v1/reports/*` a Reporting Service queda como deuda registrada,
  a evaluar cuando ambos servicios tengan persistencia real.

> [!note] Actualizado
> La deuda se cerró: el intake evidencial de PrivacyAuditService pasó a
> `/api/v1/audit/compliance-reports` y `/api/v1/reports/*` es solo de Reporting Service, que ya
> genera el artefacto en memoria y lo entrega en `GET /api/v1/reports/{id}/download`.

## Alternativas consideradas

- Migrar ya las rutas TS-21 (rechazada: rework prematuro, rompe evidencia de PrivacyAudit).
- Un solo servicio para EP09+EP10 (rechazada: granularidad media, ADR-001; capacidades independientes).

## Consecuencias

- (+) Sin rework, contratos claros, evolución independiente.
- (–) Dos superficies `/reports/*` temporalmente; documentar la frontera en Swagger.

## Notas

Plan completo en Obsidian: `16 - Plan Reporting Service.md`.
