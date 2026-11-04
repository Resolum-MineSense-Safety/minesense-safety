# Non-functional user story NFR03 (availability, SLO-1, section 4.1). Section 4.8.
@nfr @disponibilidad
Feature: NFR03 - Autodiagnóstico para la disponibilidad en Kubernetes
  Como responsable de operaciones
  quiero que cada microservicio informe si está vivo, si está listo y cómo responde
  para que Kubernetes reemplace o retire una instancia fallida sin intervención manual.

  Scenario Outline: Sondas y métricas de cada microservicio
    When Kubernetes consulta "<ruta>" en "<servicio>"
    Then responde 200 en menos de 1000 ms
    And el contenido es de tipo "<tipo>"

    Examples:
      | servicio          | ruta          | tipo       |
      | Fatigue Detection | /health/live  | text/plain |
      | Fatigue Detection | /health/ready | text/plain |
      | Fatigue Detection | /metrics      | text/plain |
      | Alerts            | /health/live  | text/plain |
      | Alerts            | /health/ready | text/plain |
      | Alerts            | /metrics      | text/plain |

  Scenario: Las métricas permiten calcular los SLI
    Given el servicio "Alerts" atendió peticiones con distintos resultados
      | ruta                           | veces |
      | /api/v1/alerts/status/Issued   | 3     |
      | /api/v1/alerts/no-existe       | 2     |
    When Prometheus lee "/metrics" de "Alerts"
    Then la serie "http_request_duration_seconds_count" existe para los códigos
      | codigo |
      | 200    |
      | 404    |
