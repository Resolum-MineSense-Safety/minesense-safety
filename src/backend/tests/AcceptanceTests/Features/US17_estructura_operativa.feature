# EP08 - Operations Service: mining units, locations, fleets and vehicles.
Feature: US17 - Registro de la estructura operativa
  Como administrador de la operación minera
  quiero registrar las unidades mineras con sus ubicaciones, flotas y vehículos
  para que el monitoreo, las alertas y los reportes usen la misma estructura.

  Scenario: Unidad minera con sus ubicaciones
    Given una unidad minera registrada en la región "Cajamarca"
    When se agregan las ubicaciones:
      | nombre         | tipo     |
      | Tajo Norte     | Pit      |
      | Botadero Sur   | Dump     |
      | Taller Central | Workshop |
    Then la unidad minera tiene las ubicaciones:
      | nombre         | tipo     |
      | Tajo Norte     | Pit      |
      | Botadero Sur   | Dump     |
      | Taller Central | Workshop |

  Scenario: Ubicación repetida
    Given una unidad minera registrada en la región "Cajamarca"
    And se agregan las ubicaciones:
      | nombre     | tipo |
      | Tajo Norte | Pit  |
    When se agrega la ubicación "Tajo Norte" de tipo "Pit"
    Then el sistema responde con el código 409

  Scenario: Código de negocio duplicado
    Given una unidad minera registrada en la región "Cajamarca"
    When se registra otra unidad minera con el mismo código de negocio
    Then el sistema responde con el código 409

  Scenario Outline: Datos obligatorios de la unidad minera
    When se registra una unidad minera con código "<codigo>", nombre "<nombre>" y región "<region>"
    Then el sistema responde con el código 400
    And el mensaje de error menciona "Missing mandatory data: <faltante>"

    Examples:
      | codigo | nombre    | region    | faltante     |
      |        | Yanacocha | Cajamarca | BusinessCode |
      | UM-X   |           | Cajamarca | Name         |
      | UM-Y   | Antamina  |           | Region       |

  Scenario: Flota con sus vehículos
    Given una unidad minera registrada en la región "Áncash"
    When se registra la flota "Acarreo" con los vehículos:
      | codigo | modelo       |
      | CAT-01 | CAT 797F     |
      | KOM-02 | Komatsu 930E |
    Then la flota tiene los vehículos activos "CAT-01, KOM-02"

  Scenario: Vehículo en una flota inexistente
    When se registra el vehículo "CAT-99" en una flota que no existe
    Then el sistema responde con el código 404
