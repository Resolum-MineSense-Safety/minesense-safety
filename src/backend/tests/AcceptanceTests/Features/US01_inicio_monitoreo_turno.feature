# EP01 - Monitoring Service, with the pre-shift check of Device Management (camera and wearable).
Feature: US01 - Inicio del monitoreo durante el turno
  Como operador de maquinaria pesada
  quiero que el monitoreo de fatiga comience al iniciar mi turno
  para estar protegido desde el primer momento de la operación.

  Background:
    Given un operador que inicia su turno en un camión de acarreo

  Scenario Outline: Inicio del monitoreo según las señales disponibles
    Given los dispositivos de cabina del operador están disponibles con <bateria> % de batería
    When se inicia el monitoreo con las señales "<senales>"
    Then la sesión de monitoreo queda en estado "<estado>"
    And el panel del operador muestra el mensaje "<mensaje>"

    Examples:
      | bateria | senales                | estado      | mensaje                                                 |
      | 90      | EyeTracking, HeartRate | Active      | Monitoreo activo                                        |
      | 75      | EyeTracking            | Partial     | Monitoreo parcial: falta frecuencia cardiaca (wearable) |
      | 15      | HeartRate              | Partial     | Monitoreo parcial: falta seguimiento ocular (cámara)    |
      | 60      |                        | Unavailable | Monitoreo no disponible                                 |

  Scenario: Dispositivos no disponibles antes del turno
    Given el operador tiene asignados los dispositivos:
      | tipo     | estado      | bateria |
      | camera   | Interrupted | 80      |
      | wearable | Available   | 90      |
    When se inicia el monitoreo con las señales "EyeTracking, HeartRate"
    Then el inicio del monitoreo se rechaza porque los dispositivos no están disponibles

  Scenario: Una sola sesión abierta por operador
    Given los dispositivos de cabina del operador están disponibles con 90 % de batería
    And el monitoreo está activo con las señales "EyeTracking, HeartRate"
    When se inicia el monitoreo con las señales "EyeTracking, HeartRate"
    Then el inicio del monitoreo se rechaza porque ya existe una sesión abierta
