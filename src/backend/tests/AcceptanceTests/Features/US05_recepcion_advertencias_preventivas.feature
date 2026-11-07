# Copied from Resolum-MineSense-Safety/minesense-safety-acceptance-tests (EP02/EP03).
# Keep the text identical: the steps in ../StepDefinitions bind to these exact sentences.
Feature: US05 - Recepción de advertencias preventivas
  Como operador de maquinaria pesada
  quiero recibir una advertencia ante fatiga o un posible microsueño
  para reconocer el riesgo y adoptar una acción segura.

  Scenario: Advertencia por fatiga
    Given se confirma una condición de fatiga
    When el sistema genera la advertencia
    Then el operador recibe una señal perceptible acorde con el nivel de riesgo

  Scenario: Advertencia crítica
    Given se detecta un posible microsueño
    When el sistema genera la advertencia
    Then utiliza los medios disponibles necesarios para comunicar la urgencia

  # The cabin devices (buzzer, vibration, screen) live in the Edge gateway, not in the cloud API.
  # This scenario is verified by the Edge agent tests; here it stays documented and skipped.
  @ignore @edge
  Scenario: Medio de advertencia no disponible
    Given uno de los medios de advertencia no funciona
    And existe otro medio de advertencia disponible
    When el sistema debe comunicar el riesgo
    Then utiliza el medio disponible para emitir la advertencia
    And informa la falla del medio que no funciona
