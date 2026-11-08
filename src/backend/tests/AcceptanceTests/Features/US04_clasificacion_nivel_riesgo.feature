# Copied from Resolum-MineSense-Safety/minesense-safety-acceptance-tests (EP02/EP03).
# Keep the text identical: the steps in ../StepDefinitions bind to these exact sentences.
Feature: US04 - Clasificación del nivel de riesgo
  Como supervisor de seguridad
  quiero clasificar cada condición detectada según su nivel de riesgo
  para priorizar la atención de los operadores en mayor peligro.

  Scenario: Clasificación normal
    Given no existen señales relevantes de fatiga
    When el sistema determina el nivel de riesgo
    Then clasifica al operador en condición normal

  Scenario: Clasificación de alerta
    Given existen señales moderadas de fatiga
    When el sistema determina el nivel de riesgo
    Then clasifica al operador en alerta

  Scenario: Clasificación crítica
    Given se reconoce un posible microsueño o una condición grave
    When el sistema determina el nivel de riesgo
    Then clasifica al operador como crítico
    And inicia la respuesta correspondiente al nivel crítico

  # Final version (section 4.8): several operators of a shift classified at once.
  Scenario: Clasificación de varios operadores del turno
    Given los operadores del turno envían sus señales
      | operador     | perclos | parpadeos_por_minuto | variabilidad_cardiaca_ms |
      | Rosa Quispe  | 0.45    | 8                    | 18                       |
      | Luis Condori | 0.20    | 18                   | 45                       |
      | Ana Torres   | 0.05    | 15                   | 60                       |
      | Juan Mamani  | 0.26    | 12                   | 19                       |
    When el sistema determina el nivel de riesgo de cada operador
    Then los operadores quedan clasificados así
      | operador     | nivel    | requiere_alerta |
      | Rosa Quispe  | Critical | sí              |
      | Luis Condori | Warning  | sí              |
      | Ana Torres   | Normal   | no              |
      | Juan Mamani  | Critical | sí              |
