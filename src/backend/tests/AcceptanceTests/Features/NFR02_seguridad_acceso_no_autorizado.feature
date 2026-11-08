# Non-functional user story NFR02 (QAS 04, SLO-4, OWASP A01). Section 4.8.
@nfr @seguridad
Feature: NFR02 - Rechazo y registro de accesos no autorizados
  Como responsable de cumplimiento y privacidad
  quiero que toda petición sin credenciales válidas sea rechazada y registrada
  para proteger la información del operador y contar con evidencia para auditoría.

  Background:
    Given los servicios exigen el token de Identity & Access

  Scenario Outline: Peticiones sin token o con token falso
    When se llama a "<metodo> <ruta>" de "<servicio>" con credencial "<credencial>"
    Then la respuesta es 401
    And el intento queda registrado con método, ruta, fecha y motivo

    Examples:
      | servicio          | metodo | ruta                                   | credencial   |
      | Fatigue Detection | POST   | /api/v1/fatigue-assessments            | ninguna      |
      | Fatigue Detection | GET    | /api/v1/fatigue-assessments/latest     | token falso  |
      | Alerts            | GET    | /api/v1/alerts/status/Issued           | ninguna      |
      | Alerts            | POST   | /api/v1/alerts                         | token falso  |
      | Alerts            | POST   | /api/v1/alerts/overdue-escalations     | ninguna      |

  Scenario: Las rutas técnicas siguen públicas para la plataforma
    Then estas rutas responden sin token
      | servicio          | ruta          |
      | Fatigue Detection | /health/live  |
      | Fatigue Detection | /health/ready |
      | Alerts            | /health/live  |
      | Alerts            | /metrics      |
