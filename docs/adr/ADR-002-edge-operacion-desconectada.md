# ADR-002: Procesamiento Edge en cabina con operación desconectada

## Estado

Aceptada. Drivers: QAS 01, 02, 03; CT01, CT02; AC01, AC03.

## Contexto

Las faenas tienen conectividad intermitente y la detección debe responder con P95 ≤ 500 ms.

## Decisión

Ejecutar inferencia, clasificación y alertas en un gateway de cabina; la nube solo recibe los resultados cuando hay red.

## Alternativas consideradas

- Inferencia en la nube.
- Inferencia en el wearable.

## Consecuencias

- (+) Protección sin red y baja latencia.
- (–) Hay que desplegar y actualizar modelos en cada gateway y la capacidad de cómputo es limitada (CT03).

## Notas

Texto tomado del Informe del proyecto, sección 2.1.4.4 (Architecture Decision Records). Esta página existía solo en el informe; se agrega aquí para que los ADR citados por el código y los documentos tengan su archivo.
