# EP05 - Incident Management Service: incidents opened from fatigue alerts and taken by a supervisor.
Feature: US09 - Recepción y asignación de incidentes de fatiga
  Como supervisor de seguridad
  quiero recibir como incidente cada alerta de fatiga y hacerme responsable de su atención
  para que ningún operador en riesgo quede sin seguimiento.

  Scenario Outline: Apertura de un incidente desde una alerta
    When se recibe una alerta "<severidad>" de un operador
    Then se abre un incidente "Pending" con severidad "<severidad>"
    And el incidente aparece en la bandeja de incidentes "Pending" del operador

    Examples:
      | severidad |
      | Warning   |
      | Critical  |

  Scenario: Alerta recibida más de una vez
    Given se recibió una alerta "Critical" de un operador
    When la misma alerta se recibe de nuevo
    Then no se abre un segundo incidente para esa alerta

  Scenario: Un supervisor toma el incidente
    Given se recibió una alerta "Critical" de un operador
    When un supervisor toma el incidente
    Then el incidente queda "Assigned" a ese supervisor
    And el incidente aparece en la bandeja de incidentes "Assigned" del operador

  Scenario: Otro supervisor intenta tomar un incidente asignado
    Given se recibió una alerta "Critical" de un operador
    And un supervisor toma el incidente
    When otro supervisor intenta tomar el incidente
    Then el sistema responde con el código 409
