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

  # Final version (section 4.8): every threshold of the detection policy, as a data table.
  Scenario Outline: Evaluación de señales según los umbrales de fatiga
    Given el operador presenta las señales de cabina
      | perclos   | parpadeos_por_minuto | variabilidad_cardiaca_ms |
      | <perclos> | <parpadeos>          | <variabilidad>           |
    When el sistema evalúa las señales
    Then el nivel de riesgo registrado es "<nivel>"
    And la evaluación requiere alerta "<requiere_alerta>"

    Examples: Umbrales de FatigueRiskPolicy
      | caso                          | perclos | parpadeos | variabilidad | nivel    | requiere_alerta |
      | operador descansado           | 0.05    | 15        | 60           | Normal   | no              |
      | justo debajo del umbral       | 0.14    | 24        | 30           | Normal   | no              |
      | ojos cerrados 15 % del tiempo | 0.15    | 15        | 60           | Warning  | sí              |
      | parpadeo acelerado            | 0.10    | 25        | 60           | Warning  | sí              |
      | variabilidad cardiaca baja    | 0.10    | 15        | 29           | Warning  | sí              |
      | posible microsueño            | 0.40    | 15        | 60           | Critical | sí              |
      | fatiga combinada grave        | 0.25    | 15        | 19           | Critical | sí              |
