# Non-functional user story NFR01 (QAS 02, SLO-2). Section 4.8.
@nfr @rendimiento
Feature: NFR01 - Rendimiento de la detección y la alerta
  Como supervisor de seguridad
  quiero que la evaluación de fatiga y la emisión de la alerta respondan en menos de 500 ms
  para que el operador reciba la advertencia antes de que el riesgo aumente.

  # In-process latency of the API itself (no network). The k6 load test of section 4.4.3 checks
  # the same SLO end to end under 50 concurrent users.
  Scenario Outline: Latencia P95 de cada endpoint del camino crítico
    Given el servicio ya atendió peticiones de calentamiento
    When se envían <peticiones> peticiones a "<endpoint>"
    Then el percentil 95 de la latencia es menor a <limite_ms> ms
    And ninguna petición falla

    Examples:
      | endpoint                  | peticiones | limite_ms |
      | evaluación de fatiga      | 200        | 500       |
      | emisión de alerta         | 200        | 500       |
      | consulta de alertas       | 200        | 2000      |
