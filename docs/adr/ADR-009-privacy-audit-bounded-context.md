# ADR-009: Privacy & Audit como bounded context nuevo (scaffold EP09/EP10)

## Estado

Propuesta (rama `feature/ep09-privacy-audit-scaffold`).

## Contexto

EP09 (US20/US21: consentimiento y control de acceso a datos sensibles) y EP10
(US22/US23: reportes y trazabilidad para cumplimiento) no tienen servicio asignado.
El diseño sigue los ADRs del informe: JWT descentralizado con accesos publicados
hacia Privacy & Audit (ADR-007), persistencia políglota con MongoDB para este
servicio (ADR-005) y PDFs pesados en Lambda (ADR-008).

## Decisión

Crear `PrivacyAuditService` (.NET 8, mismo esqueleto DDD/CQRS que `AlertService`,
puerto local `5260`) con dos subdominios: **Privacy** (`ProcessingConsent`, control
de acceso) y **Audit** (`AccessAuditEntry` append-only con hash-chain,
`ComplianceReport` async, `IncidentTrace` read-model). El scaffold expone las
9 rutas planeadas devolviendo 501; la implementación llega por US/TS:

- `GET /api/v1/processing-purposes`, `POST /api/v1/consents`,
  `GET /api/v1/consents/operator/{id}/current|history` (US20)
- `POST /api/v1/access-log/entries`, `GET /api/v1/access-log` (US21)
- `POST /api/v1/reports/safety`, `GET /api/v1/reports/{id}` (US22)
- `GET /api/v1/incidents/{incidentId}/history` (US23)

> [!note] Actualizado
> Las rutas de US22 y US23 se movieron a `POST /api/v1/audit/compliance-reports`,
> `GET /api/v1/audit/compliance-reports/{id}` y `GET /api/v1/audit/incidents/{incidentId}/history`:
> el gateway envía `/api/v1/reports` a Reporting Service y `/api/v1/incidents` a Incident
> Management, así que las rutas originales no eran alcanzables desde fuera del clúster.

## Alternativas consideradas

- Implementar EP09/EP10 dentro de otro servicio (rechazada: mezcla responsabilidades).
- Esperar a una fase posterior sin scaffold (rechazada: el contrato debe existir desde el día 1).

## Consecuencias

- (+) EP09/EP10 tienen dueño, contrato visible en Swagger y CI desde el día 1.
- (–) Requiere que Fatigue/Alert publiquen eventos con `eventId` (Fase 3).

## Notas

Plan completo en Obsidian: `10 - Plan Privacy & AuditService.md`.
