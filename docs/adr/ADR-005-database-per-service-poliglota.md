# ADR-005: Database per Service con persistencia políglota (PostgreSQL y MongoDB)

## Estado

Aceptada. Drivers: AC06, QAS 04; CT06.

## Contexto

Hay datos transaccionales (usuarios, flotas, dispositivos) y datos de alto volumen y esquema flexible (telemetría, eventos, incidentes).

## Decisión

Cada servicio es dueño de su base. PostgreSQL con EF Core para Identity & Access, Operations, Device Management y Monitoring; MongoDB para el resto. Sin claves foráneas entre servicios: solo referencias lógicas (`operatorId`, `incidentId`).

## Alternativas consideradas

- Una base compartida.
- Solo PostgreSQL con JSONB.

## Consecuencias

- (+) Autonomía y escalado independiente.
- (–) Sin joins entre servicios, por lo que las consultas transversales usan vistas de lectura o eventos.

## Implementación en el repositorio

- En `docker-compose.yml`, los cuatro contextos relacionales usan cada uno su propia base en un mismo servidor PostgreSQL (`minesense_identity`, `minesense_operations`, `minesense_devices`, `minesense_monitoring`); el servicio crea la base y sus tablas al arrancar.
- Los workflows `postgresql-ci.yml` y `feedback-mongodb-ci.yml` prueban los servicios contra persistencia real.

## Notas

Texto tomado del Informe del proyecto, sección 2.1.4.4 (Architecture Decision Records). Esta página existía solo en el informe; se agrega aquí para que los ADR citados por el código y los documentos tengan su archivo.
