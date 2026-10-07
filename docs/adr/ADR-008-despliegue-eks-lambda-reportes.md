# ADR-008: Despliegue en Amazon EKS con Lambda para reportes pesados

## Estado

Aceptada.

**Drivers:** CS01; CT06

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

---

Fuente: informe del equipo, sección 2.1.4.4 *Architecture Decision Records (ADR)*.
