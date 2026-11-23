# EP05 - Incident Management Service: unattended critical incidents climb the escalation chain
# every 2 minutes (ShiftManager, then SafetyManager).
Feature: US11 - Escalamiento de incidentes críticos no atendidos
  Como jefe de seguridad
  quiero que los incidentes críticos sin responsable se escalen automáticamente
  para que siempre haya alguien atendiendo a un operador en riesgo.

  # Time passes on the fake clock of the in-memory Incident Management host.
  @solo-en-memoria
  Scenario Outline: Escalamiento progresivo de un incidente sin responsable
    Given se recibió una alerta "Critical" de un operador
    When transcurren <minutos> minutos sin que nadie tome el incidente
    Then el escalamiento del incidente es "<estado>" con nivel <nivel>
    And se notificó a los roles "<roles>"

    Examples:
      | minutos | estado      | nivel | roles                       |
      | 1       | NotRequired | 0     |                             |
      | 3       | Escalated   | 1     | ShiftManager                |
      | 5       | Escalated   | 2     | ShiftManager, SafetyManager |
      | 9       | Escalated   | 2     | ShiftManager, SafetyManager |

  @solo-en-memoria
  Scenario: Incidente tomado a tiempo
    Given se recibió una alerta "Critical" de un operador
    And un supervisor toma el incidente
    When transcurren 5 minutos desde la apertura del incidente
    Then el escalamiento del incidente es "NotRequired" con nivel 0

  Scenario: Incidente de advertencia
    Given se recibió una alerta "Warning" de un operador
    When se consulta el escalamiento del incidente
    Then el escalamiento del incidente es "NotApplicable" con nivel 0
    And se notificó a los roles ""
