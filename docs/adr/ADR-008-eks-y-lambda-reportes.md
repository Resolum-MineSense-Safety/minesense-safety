# ADR-008: Despliegue en Amazon EKS con Lambda para reportes pesados

## Estado

Aceptada. Drivers: CS01; CT06.

## Contexto

Se requiere orquestación de contenedores y al menos una función serverless; generar PDFs no debe bloquear los servicios principales.

## Decisión

Microservicios .NET en Kubernetes (EKS) desplegados con Helm y Terraform; AWS Lambda para la generación asíncrona de PDF.

## Alternativas consideradas

- ECS/Fargate.
- Generar los PDFs dentro del Reporting Service.

## Consecuencias

- (+) Escalado independiente y aislamiento de cargas pesadas.
- (–) Costo y curva de aprendizaje de Kubernetes.

## Implementación en el repositorio

- Infraestructura en `infrastructure/terraform`; charts en `infrastructure/helm` y despliegue en `infrastructure/helm/README.md`.
- El flujo de PDF por SQS, Lambda y S3 es opcional y se describe en `docs/services/reporting.md` y en el ADR-010.

## Notas

Texto tomado del Informe del proyecto, sección 2.1.4.4 (Architecture Decision Records). Esta página existía solo en el informe; se agrega aquí para que los ADR citados por el código y los documentos tengan su archivo.
