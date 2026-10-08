# Documentación de arquitectura – MineSense Safety

Documentación de arquitectura del monorepo, derivada del informe del equipo (secciones 2.1.3 a 2.1.6).

## Contenido

| Sección | Descripción |
|---------|-------------|
| [Architecture Decision Records](adr/README.md) | Índice y registros ADR-001 a ADR-008 (sección 2.1.4.4). |
| [C1 – Contexto](c4-diagrams/c1-context.md) | Actores, sistema y sistemas externos (sección 2.1.4.1). |
| [C2 – Contenedores](c4-diagrams/c2-containers.md) | Contenedores Edge y Cloud, mensajería y bases de datos (sección 2.1.4.2). |
| [C3 – Componentes](c4-diagrams/c3-components.md) | Fatigue Detection, Incident Management y Edge Processing (sección 2.1.4.3). |
| [Context Map (DDD)](ddd/context-map.md) | Mapa de Bounded Contexts y sus relaciones. |

## Convenciones

- Los diagramas se escriben en Mermaid (`flowchart`) para que GitHub los renderice directamente.
- El informe indica que las vistas C4 se modelan como código en Structurizr DSL (`workspace.dsl`); ese archivo aún no está en el repositorio.
- Las ambigüedades frente al informe se documentan al final de cada página.
- Nuevas decisiones de arquitectura: agregar un ADR en `adr/` y registrarlo en su índice.
