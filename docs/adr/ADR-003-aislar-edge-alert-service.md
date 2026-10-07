# ADR-003: Aislar el Edge Alert Service del registro y la sincronización

## Estado

Aceptada.

**Drivers:** AC04, QAS 01, QAS 05

## Contexto

Una falla en el almacenamiento o la sincronización no debe impedir la alarma en cabina.

## Decisión

Contenedores Docker independientes en el gateway (Edge Processing, Edge Alert y Edge Sync Agent), comunicados por el broker local; la alerta depende solo de la detección.

## Alternativas consideradas

- Un único proceso Edge.
- Alerta emitida desde la nube.

## Consecuencias

- (+) Resiliencia de la función crítica.
- (–) Más contenedores por gateway y dependencia del broker local.

---

Fuente: informe del equipo, sección 2.1.4.4 *Architecture Decision Records (ADR)*.
