# Architecture Decision Records (ADR)

Registro de las decisiones arquitectónicas de MineSense Safety, transcritas de la sección 2.1.4.4 del informe del equipo.
Cada ADR sigue el formato clásico: Título, Estado, Contexto, Decisión y Consecuencias (más las alternativas consideradas y los drivers que cita el informe).

Los drivers hacen referencia a los identificadores del informe: Quality Attribute Scenarios (`QAS`, sección 2.1.1), Architectural Concerns (`AC`, sección 2.1.2.4), Technical Constraints (`CT`, sección 2.1.2.5.1) y Schedule Constraints (`CS`, sección 2.1.2.5.3).

| ADR | Título | Estado | Drivers |
|-----|--------|--------|---------|
| [ADR-001](ADR-001-microservicios-bounded-contexts-granularidad-media.md) | Microservicios con Bounded Contexts de granularidad media | Aceptada | AC04, CT06, QAS 01 |
| [ADR-002](ADR-002-procesamiento-edge-operacion-desconectada.md) | Procesamiento Edge en cabina con operación desconectada | Aceptada | QAS 01, 02, 03; CT01, CT02; AC01, AC03 |
| [ADR-003](ADR-003-aislar-edge-alert-service.md) | Aislar el Edge Alert Service del registro y la sincronización | Aceptada | AC04, QAS 01, QAS 05 |
| [ADR-004](ADR-004-mqtts-edge-to-cloud-kafka-cloud-to-cloud.md) | MQTTS para Edge-to-Cloud y Apache Kafka para Cloud-to-Cloud | Aceptada | QAS 03, QAS 06; AC02, AC03 |
| [ADR-005](ADR-005-database-per-service-persistencia-poliglota.md) | Database per Service con persistencia políglota (PostgreSQL y MongoDB) | Aceptada | AC06, QAS 04; CT06 |
| [ADR-006](ADR-006-dotnet-cloud-python-edge.md) | C# .NET 8 para los servicios cloud y Python para el Edge | Aceptada | QAS 02; CT03; CS01 |
| [ADR-007](ADR-007-contratos-rest-openapi-jwt-descentralizado.md) | Contratos REST versionados con OpenAPI y validación JWT descentralizada | Aceptada | QAS 04; AC05 |
| [ADR-008](ADR-008-despliegue-eks-lambda-reportes.md) | Despliegue en Amazon EKS con Lambda para reportes pesados | Aceptada | CS01; CT06 |

## Cómo agregar un nuevo ADR

1. Copiar el formato de un ADR existente con el siguiente número correlativo: `ADR-00N-titulo-en-kebab-case.md`.
2. Completar Estado (`Propuesta`, `Aceptada`, `Reemplazada por ADR-XXX`, `Obsoleta`), Contexto, Decisión, Alternativas y Consecuencias.
3. Añadir una fila a la tabla de este índice.
