# ADR-005: Database per Service con persistencia políglota (PostgreSQL y MongoDB)

## Estado

Aceptada.

**Drivers:** AC06, QAS 04; CT06

## Contexto

Hay datos transaccionales (usuarios, flotas, dispositivos) y datos de alto volumen y esquema flexible (telemetría, eventos, incidentes).

## Decisión

Cada servicio es dueño de su base. PostgreSQL con EF Core para Identity, Operations, Device y Monitoring; MongoDB para el resto. Sin claves foráneas entre servicios; solo referencias lógicas (`operatorId`, `incidentId`).

## Alternativas consideradas

- Una base compartida.
- Solo PostgreSQL con JSONB.

## Consecuencias

- (+) Autonomía y escalado independiente.
- (–) Sin joins entre servicios, por lo que las consultas transversales usan vistas de lectura o eventos.

## Notas

El detalle de entidades por servicio está en la sección 2.1.6 (Modelo de datos por servicio). En el Edge, el buffer local usa SQLite (tabla de contenedores de la sección 2.1.4.2).

---

Fuente: informe del equipo, sección 2.1.4.4 *Architecture Decision Records (ADR)*.
