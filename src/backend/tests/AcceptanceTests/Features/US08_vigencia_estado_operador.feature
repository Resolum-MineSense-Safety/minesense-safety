# EP04 - Fleet Monitoring Service: freshness of each operator state (5 minutes by default).
Feature: US08 - Vigencia del estado de cada operador
  Como supervisor del centro de control
  quiero saber si el estado que veo de cada operador está actualizado
  para no tomar decisiones con información desactualizada.

  Background:
    Given el centro de control monitorea a los operadores de una flota:
      | operador      | vehiculo | turno | ubicacion  | riesgo |
      | Elena Paredes | CA-201   | Night | Chancadora | Normal |

  Scenario: Cambio del nivel de riesgo
    When el nivel de riesgo de "Elena Paredes" cambia a "Critical"
    And el supervisor consulta el estado del operador "Elena Paredes"
    Then ve el vehículo "CA-201", la ubicación "Chancadora" y el riesgo "Critical"
    And la información del operador figura como "Current"

  Scenario: Reasignación de vehículo durante el turno
    When "Elena Paredes" pasa al vehículo "CA-220" en la ubicación "Rampa 3"
    And el supervisor consulta el estado del operador "Elena Paredes"
    Then ve el vehículo "CA-220", la ubicación "Rampa 3" y el riesgo "Normal"

  # Time passes on the fake clock of the in-memory Fleet Monitoring host.
  @solo-en-memoria
  Scenario Outline: Información sin novedades
    When transcurren <minutos> minutos sin novedades de "Elena Paredes"
    Then la información del operador figura como "<vigencia>"

    Examples:
      | minutos | vigencia |
      | 4       | Current  |
      | 5       | Current  |
      | 6       | Stale    |
