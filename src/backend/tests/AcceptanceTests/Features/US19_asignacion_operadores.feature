# EP08 - Operations Service: assignment of operators to a shift and a vehicle (TS-08).
Feature: US19 - Asignación de operadores a turnos y vehículos
  Como supervisor de operaciones
  quiero asignar cada operador a un turno y a un vehículo
  para que el monitoreo sepa quién conduce cada equipo.

  Background:
    Given una unidad minera registrada en la región "Arequipa"
    And se registra la flota "Acarreo" con los vehículos:
      | codigo | modelo   |
      | CAT-11 | CAT 793F |
      | CAT-12 | CAT 793F |
    And los operadores de la unidad minera:
      | operador     |
      | Ana Quispe   |
      | Bruno Huamán |

  Scenario: Asignación válida
    When se asigna a "Ana Quispe" al vehículo "CAT-11" en el turno "Day" desde hoy
    Then el sistema responde con el código 201
    And la asignación queda "Active"
    And el monitoreo puede validar que "Ana Quispe" opera el vehículo "CAT-11" hoy

  Scenario Outline: Reglas de conflicto de las asignaciones
    Given "Ana Quispe" está asignada al vehículo "CAT-11" en el turno "Day" desde hoy
    When se asigna a "<operador>" al vehículo "<vehiculo>" en el turno "<turno>" desde hoy
    Then el sistema responde con el código <codigo>

    Examples:
      | operador     | vehiculo | turno | codigo |
      | Ana Quispe   | CAT-12   | Night | 409    |
      | Bruno Huamán | CAT-11   | Day   | 409    |
      | Bruno Huamán | CAT-11   | Night | 201    |
      | Bruno Huamán | CAT-12   | Day   | 201    |

  Scenario: Vehículo en mantenimiento
    Given el vehículo "CAT-12" está en mantenimiento
    When se asigna a "Bruno Huamán" al vehículo "CAT-12" en el turno "Day" desde hoy
    Then el sistema responde con el código 422
    And el mensaje de error menciona "InMaintenance"

  Scenario: Operador asignado a otro vehículo
    Given "Ana Quispe" está asignada al vehículo "CAT-11" en el turno "Day" desde hoy
    When el monitoreo valida si "Ana Quispe" opera el vehículo "CAT-12" hoy
    Then la validación se rechaza indicando que está asignada a otro vehículo

  Scenario: Cambio de vehículo conserva el historial
    Given "Ana Quispe" está asignada al vehículo "CAT-11" en el turno "Day" desde hoy
    When se cambia el vehículo de "Ana Quispe" a "CAT-12" desde hoy
    Then el historial de asignaciones de "Ana Quispe" registra:
      | vehiculo | estado     |
      | CAT-11   | Superseded |
      | CAT-12   | Active     |
