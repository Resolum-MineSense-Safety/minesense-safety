# ADR-001: Microservicios con Bounded Contexts de granularidad media

## Estado

Aceptada. Drivers: AC04, CT06, QAS 01.

## Contexto

Las funciones críticas (detección y alerta) deben seguir operando aunque falle el registro, los reportes o la supervisión. Además, los requisitos de latencia y escala difieren entre detección y reportes históricos.

## Decisión

Descomponer en 13 microservicios alineados a los Epics EP01–EP12, uno por Bounded Context.

## Alternativas consideradas

- Monolito modular.
- Un microservicio por User Story.

## Consecuencias

- (+) Aislamiento de fallos y evolución independiente.
- (–) Mayor complejidad operativa, consistencia eventual y necesidad de contratos versionados.

## Notas

Texto tomado del Informe del proyecto, sección 2.1.4.4 (Architecture Decision Records). Esta página existía solo en el informe; se agrega aquí para que los ADR citados por el código y los documentos tengan su archivo.
