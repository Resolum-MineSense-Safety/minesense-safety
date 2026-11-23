# EP06 - Device Management Service: cabin devices and the pre-shift check that Monitoring calls.
Feature: EP06 - Disponibilidad de los dispositivos de cabina antes del turno
  Como supervisor de seguridad
  quiero verificar los dispositivos de cabina de cada operador antes del turno
  para no iniciar una operación sin protección completa.

  Scenario Outline: Verificación pre-turno de la cámara y el wearable
    Given un operador que inicia su turno en un camión de acarreo
    And el operador tiene asignados los dispositivos:
      | tipo     | estado          | bateria          |
      | camera   | <estado_camara> | <bateria_camara> |
      | wearable | Available       | 90               |
    When se verifica la disponibilidad pre-turno con un umbral de batería de 20 %
    Then el resultado de la verificación es "<resultado>" con la recomendación "<recomendacion>"

    Examples:
      | estado_camara | bateria_camara | resultado            | recomendacion               |
      | Available     | 85             | Available            | StartMonitoring             |
      | Available     | 12             | Warning              | ReviewBatteryBeforeStarting |
      | Limited       | 85             | ProtectionIncomplete | DoNotStartMonitoring        |
      | Interrupted   | 85             | ProtectionIncomplete | DoNotStartMonitoring        |

  Scenario: Falta un tipo de dispositivo
    Given un operador que inicia su turno en un camión de acarreo
    And el operador tiene asignados los dispositivos:
      | tipo   | estado    | bateria |
      | camera | Available | 90      |
    When se verifica la disponibilidad pre-turno con un umbral de batería de 20 %
    Then el resultado de la verificación es "ProtectionIncomplete" con la recomendación "DoNotStartMonitoring"
    And la verificación informa que falta el dispositivo "wearable"

  Scenario: Código de dispositivo duplicado
    Given un dispositivo "camera" registrado
    When se registra otro dispositivo con el mismo código
    Then el sistema responde con el código 409

  Scenario: Dispositivo que ya tiene una asignación activa
    Given un operador que inicia su turno en un camión de acarreo
    And el operador tiene asignados los dispositivos:
      | tipo   | estado    | bateria |
      | camera | Available | 90      |
    When se asigna la misma cámara a otro operador
    Then el sistema responde con el código 422
    And el mensaje de error menciona "already has an active assignment"
