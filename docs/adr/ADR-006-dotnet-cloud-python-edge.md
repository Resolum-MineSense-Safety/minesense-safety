# ADR-006: C# .NET 8 para los servicios cloud y Python para el Edge

## Estado

Aceptada.

**Drivers:** QAS 02; CT03; CS01

## Contexto

El backend requiere concurrencia y testing sólido (xUnit, Moq); el Edge necesita integrarse con el pipeline de ML.

## Decisión

ASP.NET Core Web API en .NET 8 para los microservicios; Python con scikit-learn y pytest en el gateway.

## Alternativas consideradas

- Node.js o Java Spring Boot en backend.
- C++ en el Edge.

## Consecuencias

- (+) Ecosistema maduro y menor fricción con ML.
- (–) Dos stacks que mantener en el equipo.

---

Fuente: informe del equipo, sección 2.1.4.4 *Architecture Decision Records (ADR)*.
