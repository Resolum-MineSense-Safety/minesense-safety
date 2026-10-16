# MineSense Safety — Índice de análisis

Fecha: 2026-10-09
Repo analizado: `C:\Users\nub\Documents\programming_projects\university\minesense-safety`
Destino notas: `C:\Users\nub\Documents\obsidian\projects\minesense-safety`

## Archivos en esta carpeta

- [[01 - Proyecto global]] — visión, estado por componente, % completitud, gaps de integración.
- [[02 - AlertService (alerts)]] — análisis de correctitud, bugs, tests, qué falta.
- [[03 - FatigueDetectionService (detectfatigue)]] — análisis de correctitud, política de riesgo, bugs, tests, qué falta.
- [[04 - Backlog para 100%]] — checklist accionable priorizado para llegar a producción.
- [[05 - Ejecución y visualización]] — comandos para levantarlo y cómo validar el avance en vivo.
- [[06 - FatigueDetectionService endpoints]] — documentación detallada del microservicio y sus 5 endpoints + ejemplos.
- [[07 - AlertService endpoints]] — documentación detallada del microservicio y sus 7 endpoints + ejemplos.
- [[08 - Explicación sencilla (no técnica)]] — ambos microservicios y sus 12 endpoints en lenguaje normal.
- [[09 - Ejemplos para Swagger]] — bodies y parámetros listos para copiar/pegar y probar cada endpoint.

## Veredicto rápido

- `AlertService`: correcto como slice académico (~90% EP03), **no completo para producción** (~50% prod). Falta persistencia real, worker de overdue, integración con Fatigue, auth, validación completa.
- `FatigueDetectionService`: correcto como placeholder de reglas (~70% EP02), **no completo**. Falta calibración/ML real, integración edge/MQTT, evento `RequiresAlert → Alert`, validación `NaN`, persistencia real.
- Proyecto global: **~20-25%**. Solo 2 de 7 microservicios implementados. Edge, frontend, infra, docs están en 2-5%.
