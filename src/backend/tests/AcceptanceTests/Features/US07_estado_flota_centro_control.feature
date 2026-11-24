# EP04 - Fleet Monitoring Service: read model of the control center.
Feature: US07 - Estado de la flota en el centro de control
  Como supervisor del centro de control
  quiero ver el estado actual de los operadores de cada flota
  para priorizar la atención de quienes están en mayor riesgo.

  Background:
    Given el centro de control monitorea a los operadores de una flota:
      | operador     | vehiculo | turno | ubicacion  | riesgo   |
      | Ana Quispe   | CA-101   | Day   | Tajo Norte | Normal   |
      | Bruno Huamán | CA-102   | Day   | Tajo Norte | Critical |
      | Carla Rojas  | CA-103   | Night | Botadero   | Warning  |
      | Diego Salas  | CA-104   | Day   | Botadero   | Warning  |

  Scenario: Priorización por nivel de riesgo
    When el supervisor consulta el estado de la flota
    Then los operadores aparecen en este orden:
      | operador     | riesgo   |
      | Bruno Huamán | Critical |
      | Carla Rojas  | Warning  |
      | Diego Salas  | Warning  |
      | Ana Quispe   | Normal   |

  Scenario Outline: Filtros combinados del tablero
    When el supervisor filtra la flota por turno "<turno>", ubicación "<ubicacion>" y riesgo "<riesgo>"
    Then solo ve a los operadores "<operadores>"

    Examples:
      | turno | ubicacion  | riesgo   | operadores               |
      | Day   |            |          | Bruno Huamán, Diego Salas, Ana Quispe |
      | Day   | Botadero   | Warning  | Diego Salas              |
      |       | Tajo Norte |          | Bruno Huamán, Ana Quispe |
      | Night |            | Critical |                          |

  Scenario: Consulta del estado de un operador
    When el supervisor consulta el estado del operador "Carla Rojas"
    Then ve el vehículo "CA-103", la ubicación "Botadero" y el riesgo "Warning"
