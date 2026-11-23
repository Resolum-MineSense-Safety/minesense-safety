# EP11 - Analytics Service: alerts before and after a preventive measure, over equal windows.
Feature: US25 - Evaluación de medidas preventivas
  Como jefe de seguridad
  quiero comparar las alertas de fatiga antes y después de aplicar una medida preventiva
  para saber si la medida está funcionando.

  Scenario: Medida con períodos comparables
    Given una medida preventiva "PreventivePause" aplicada hace 40 días en una unidad minera
    And se registraron alertas en esa unidad minera alrededor de la medida:
      | periodo | cantidad |
      | antes   | 12       |
      | despues | 6        |
    When el analista evalúa la medida con una ventana de 30 días
    Then la evaluación es comparable con 12 alertas antes y 6 después
    And la variación de alertas por día es de -50 %
    And la conclusión aclara que es una variación observada

  Scenario Outline: Evaluaciones que todavía no son comparables
    Given una medida preventiva "OperatorRelief" aplicada hace <dias> días en una unidad minera
    And se registraron alertas en esa unidad minera alrededor de la medida:
      | periodo | cantidad          |
      | antes   | <alertas_previas> |
    When el analista evalúa la medida con una ventana de 30 días
    Then la evaluación no es comparable porque "<motivo>"

    Examples:
      | dias | alertas_previas | motivo                 |
      | 10   | 12              | is still running       |
      | 40   | 4               | at least 10 are needed |
