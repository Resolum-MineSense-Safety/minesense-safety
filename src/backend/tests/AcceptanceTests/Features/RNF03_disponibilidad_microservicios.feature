# Non-functional: liveness and readiness probes (Kubernetes, ALB) and reachability of every service.
# The gateway only routes /api/v1/... paths, so the probes themselves are checked in memory; against
# a deployed environment every microservice is reached through the gateway instead.
@no-funcional @disponibilidad
Feature: RNF03 - Disponibilidad de los microservicios
  Como responsable de operaciones de la plataforma
  quiero que cada microservicio informe si está vivo y listo para recibir tráfico
  para que Kubernetes y el balanceador solo envíen solicitudes a instancias sanas.

  @solo-en-memoria
  Scenario Outline: Sondas de vida y de preparación
    When se consultan las sondas de salud de "<servicio>"
    Then la sonda "/health/live" responde "Healthy" sin token de acceso
    And la sonda "/health/ready" responde "Healthy" sin token de acceso

    Examples:
      | servicio            |
      | FatigueDetection    |
      | Alerts              |
      | IdentityAccess      |
      | Operations          |
      | DeviceManagement    |
      | Monitoring          |
      | FleetMonitoring     |
      | IncidentManagement  |
      | Analytics           |
      | FeedbackReliability |
      | PrivacyAudit        |
      | Reporting           |
      | EdgeSynchronization |

  Scenario Outline: Cada microservicio atiende a través del punto de entrada
    When se consulta la ruta "<ruta>" de "<servicio>" a través del punto de entrada
    Then el microservicio responde sin error de servidor en menos de 3000 ms

    Examples:
      | servicio            | ruta                                                                                                    |
      | FatigueDetection    | /api/v1/fatigue-assessments/latest?operatorId=7d1f8a52-2a0c-4c43-9a8e-1f2b3c4d5e6f                      |
      | Alerts              | /api/v1/alerts/status/Escalated                                                                         |
      | IdentityAccess      | /.well-known/jwks.json                                                                                  |
      | Operations          | /api/v1/mining-units                                                                                    |
      | DeviceManagement    | /api/v1/devices/pre-shift-availability?operatorId=7d1f8a52-2a0c-4c43-9a8e-1f2b3c4d5e6f&batteryThreshold=20 |
      | Monitoring          | /api/v1/monitoring-sessions?status=Active                                                               |
      | FleetMonitoring     | /api/v1/fleet/status?fleet=sin-operadores                                                               |
      | IncidentManagement  | /api/v1/incidents?status=Pending&operatorId=7d1f8a52-2a0c-4c43-9a8e-1f2b3c4d5e6f                        |
      | Analytics           | /api/v1/analytics/fatigue-trends?groupBy=Shift&mineSite=sin-datos                                       |
      | FeedbackReliability | /api/v1/feedback-alerts?operationId=7d1f8a52-2a0c-4c43-9a8e-1f2b3c4d5e6f&from=2026-01-01&to=2026-02-01 |
      | PrivacyAudit        | /api/v1/processing-purposes                                                                             |
      | Reporting           | /api/v1/report-definitions                                                                              |
      | EdgeSynchronization | /api/v1/synchronization-batches/7d1f8a52-2a0c-4c43-9a8e-1f2b3c4d5e6f                                    |
