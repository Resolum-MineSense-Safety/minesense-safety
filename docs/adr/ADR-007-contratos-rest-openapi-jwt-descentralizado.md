# ADR-007: Contratos REST versionados con OpenAPI y validación JWT descentralizada

## Estado

Aceptada.

**Drivers:** QAS 04; AC05

## Contexto

Dashboard, backend y Edge comparten contratos, y el 100 % de los accesos no autorizados debe rechazarse y registrarse.

## Decisión

API First con `/api/v1`, OpenAPI/Swagger y Bearer Token emitido por Identity & Access Service. Cada servicio valida el JWT localmente con las claves públicas (JWKS) y los accesos se publican hacia Privacy & Audit Service.

## Alternativas consideradas

- Sesiones con estado en cada servicio.
- Llamar a Identity en cada solicitud.

## Consecuencias

- (+) Menor latencia y sin punto único de falla en la validación.
- (–) La revocación de tokens depende de su vida corta.

## Notas

Convenciones de API (sección 2.1.6): `application/json`, identificadores UUID, fechas ISO 8601, códigos HTTP estándar; los cambios incompatibles incrementan la versión mayor (Semantic Versioning).

---

Fuente: informe del equipo, sección 2.1.4.4 *Architecture Decision Records (ADR)*.
