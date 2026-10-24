# Copied from Resolum-MineSense-Safety/minesense-safety-acceptance-tests (EP02/EP03).
# Keep the text identical: the steps in ../StepDefinitions bind to these exact sentences.
Feature: US06 - Confirmación de atención de una advertencia
  Como operador de maquinaria pesada
  quiero confirmar que atiendo una advertencia de fatiga
  para informar que reconozco el riesgo y permitir su seguimiento.

  Scenario: Advertencia confirmada
    Given el operador recibe una advertencia
    When el operador confirma que la atiende
    Then el sistema registra el evento como reconocido

  Scenario: Recuperación posterior
    Given el operador confirma una advertencia
    When sus señales regresan a una condición aceptable
    Then el sistema registra la recuperación

  Scenario: Ausencia de confirmación
    Given se emite una advertencia crítica
    When transcurre el tiempo establecido sin recibir confirmación
    Then el evento permanece pendiente de confirmación
    And el sistema comunica la ausencia de confirmación al supervisor
