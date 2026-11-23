# Non-functional: access control (ADR-007) and error responses.
# In memory each service is started with Authentication:IdentityUrl set, as Docker Compose and Helm
# do; a deployed environment already runs that way, so the same scenarios go through its gateway.
@no-funcional @seguridad
Feature: RNF02 - Seguridad del acceso a las APIs
  Como responsable de seguridad de la información
  quiero que solo los usuarios autenticados accedan a los datos de los operadores
  para proteger su información biométrica y operativa.

  Scenario Outline: Rutas protegidas sin token de acceso
    When se consulta la ruta "<ruta>" de "<servicio>" sin token de acceso
    Then el sistema responde con el código 401

    Examples:
      | servicio            | ruta                                                                                                    |
      | FatigueDetection    | /api/v1/fatigue-assessments/latest?operatorId=7d1f8a52-2a0c-4c43-9a8e-1f2b3c4d5e6f                      |
      | Alerts              | /api/v1/alerts/status/Issued                                                                            |
      | IdentityAccess      | /api/v1/users/me                                                                                        |
      | Operations          | /api/v1/mining-units                                                                                    |
      | DeviceManagement    | /api/v1/devices                                                                                         |
      | Monitoring          | /api/v1/monitoring-sessions                                                                             |
      | FleetMonitoring     | /api/v1/fleet/status                                                                                    |
      | IncidentManagement  | /api/v1/incidents                                                                                       |
      | Analytics           | /api/v1/analytics/preventive-measures                                                                   |
      | FeedbackReliability | /api/v1/feedback-alerts?operationId=7d1f8a52-2a0c-4c43-9a8e-1f2b3c4d5e6f&from=2026-01-01&to=2026-02-01 |
      | PrivacyAudit        | /api/v1/access-log                                                                                      |
      | Reporting           | /api/v1/report-definitions                                                                              |
      | EdgeSynchronization | /api/v1/synchronization-batches/7d1f8a52-2a0c-4c43-9a8e-1f2b3c4d5e6f                                    |

  Scenario Outline: Token falsificado
    When se consulta la ruta "<ruta>" de "<servicio>" con un token falsificado
    Then el sistema responde con el código 401

    Examples:
      | servicio           | ruta                         |
      | Alerts             | /api/v1/alerts/status/Issued |
      | IdentityAccess     | /api/v1/users/me             |
      | IncidentManagement | /api/v1/incidents            |
      | PrivacyAudit       | /api/v1/access-log           |

  Scenario: La clave pública de los tokens se publica sin autenticación
    When se consulta la ruta "/.well-known/jwks.json" de "IdentityAccess" sin token de acceso
    Then el sistema responde con el código 200
    And la respuesta publica una clave RSA para verificar las firmas "RS256"

  Scenario: Las credenciales inválidas no revelan si el usuario existe
    When se inicia sesión con un usuario inexistente y luego con una contraseña incorrecta de un usuario real
    Then ambas respuestas son 401 con el mismo mensaje

  Scenario Outline: Los errores no exponen detalles internos
    When se envía un cuerpo JSON malformado a "<ruta>" de "<servicio>"
    Then el sistema responde con el código 400
    And la respuesta no expone trazas de la pila ni nombres de excepciones

    Examples:
      | servicio            | ruta                                |
      | FatigueDetection    | /api/v1/fatigue-assessments         |
      | Alerts              | /api/v1/alerts                      |
      | IncidentManagement  | /api/v1/incidents                   |
      | Monitoring          | /api/v1/monitoring-sessions         |
      | Analytics           | /api/v1/analytics/fatigue-events    |
      | EdgeSynchronization | /api/v1/synchronization-batches     |
