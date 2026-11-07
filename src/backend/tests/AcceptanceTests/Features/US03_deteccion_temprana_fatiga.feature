# Copied from Resolum-MineSense-Safety/minesense-safety-acceptance-tests (EP02/EP03).
# Keep the text identical: the steps in ../StepDefinitions bind to these exact sentences.
Feature: US03 - Detección temprana de fatiga
  Como operador de maquinaria pesada
  quiero identificar oportunamente señales de fatiga o microsueño
  para recibir una advertencia antes de que el riesgo aumente.

  Scenario: Condición normal
    Given las señales del operador están dentro de los parámetros aceptables
    When el sistema evalúa las señales
    Then mantiene la condición del operador como normal

  Scenario: Fatiga detectada
    Given las señales del operador presentan un patrón asociado con fatiga
    When el sistema completa la evaluación
    Then registra la condición de riesgo correspondiente

  Scenario: Información insuficiente
    Given no existe información suficiente para determinar el estado del operador
    When el sistema intenta realizar la evaluación
    Then declara el resultado como no concluyente
    And no lo presenta como una detección confirmada
