# ADR-007: Contratos REST versionados con OpenAPI y validación JWT descentralizada

## Estado

Aceptada. Drivers: QAS 04; AC05.

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

## Implementación en el repositorio

- Identity & Access firma con RS256, publica la clave pública en `GET /.well-known/jwks.json` y el token de acceso dura 15 minutos por defecto (`docs/services/identity-access.md`).
- En `docker-compose.yml`, `Authentication__IdentityUrl` apunta cada servicio a Identity & Access para validar sus tokens.

## Notas

Texto tomado del Informe del proyecto, sección 2.1.4.4 (Architecture Decision Records). Esta página existía solo en el informe; se agrega aquí para que los ADR citados por el código y los documentos tengan su archivo.
