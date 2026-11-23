# EP09 - Privacy & Audit Service: purposes of the processing policy and consent decisions.
Feature: US20 - Consentimiento informado del operador
  Como operador de maquinaria pesada
  quiero conocer y decidir sobre el uso de mis datos biométricos
  para que el monitoreo respete mi privacidad.

  Scenario: Catálogo de finalidades vigente
    When el operador consulta las finalidades del tratamiento de sus datos
    Then la política vigente es "v1.0" con las finalidades:
      | finalidad             | obligatoria |
      | fatigue-monitoring    | true        |
      | safety-alerts         | true        |
      | operational-analytics | false       |

  Scenario Outline: Decisión de consentimiento y su efecto en el monitoreo
    When el operador registra la decisión "<estado>" para las finalidades "<finalidades>"
    Then su consentimiento vigente es "<estado>"
    And el consentimiento bloquea el monitoreo: <bloquea>

    Examples:
      | estado   | finalidades                                              | bloquea |
      | Accepted | fatigue-monitoring, safety-alerts, operational-analytics | false   |
      | Accepted | fatigue-monitoring, safety-alerts                        | false   |
      | Accepted | fatigue-monitoring                                       | true    |
      | Declined | fatigue-monitoring, safety-alerts                        | true    |
      | Revoked  | fatigue-monitoring, safety-alerts                        | true    |

  Scenario: Decisión repetida
    Given el operador registró la decisión "Accepted" para las finalidades "fatigue-monitoring, safety-alerts"
    When el operador registra la decisión "Accepted" para las finalidades "fatigue-monitoring, safety-alerts"
    Then el sistema responde con el código 200
    And el historial de consentimientos del operador tiene 1 decisiones

  Scenario: Cambio de decisión
    Given el operador registró la decisión "Accepted" para las finalidades "fatigue-monitoring, safety-alerts"
    When el operador registra la decisión "Revoked" para las finalidades "fatigue-monitoring, safety-alerts"
    Then su consentimiento vigente es "Revoked"
    And el historial de consentimientos del operador tiene 2 decisiones

  Scenario: Finalidad desconocida
    When el operador registra la decisión "Accepted" para las finalidades "fatigue-monitoring, venta-de-datos"
    Then el sistema responde con el código 422
    And el mensaje de error menciona "Unknown processing purpose"
