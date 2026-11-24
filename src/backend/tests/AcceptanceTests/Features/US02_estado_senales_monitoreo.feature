# EP01 - Monitoring Service: the Edge gateway reports lost and restored signals.
Feature: US02 - Estado de las señales del monitoreo
  Como operador de maquinaria pesada
  quiero saber si el monitoreo funciona con todas sus señales
  para no confiar en una lectura incompleta o incorrecta.

  Background:
    Given un operador que inicia su turno en un camión de acarreo
    And los dispositivos de cabina del operador están disponibles con 90 % de batería
    And el monitoreo está activo con las señales "EyeTracking, HeartRate"

  Scenario Outline: Pérdida de una señal durante el turno
    When el gateway de la cabina informa la pérdida de la señal "<senal>"
    Then la sesión de monitoreo queda en estado "Partial"
    And el panel del operador muestra el mensaje "<mensaje>"

    Examples:
      | senal       | mensaje                                                 |
      | EyeTracking | Monitoreo parcial: falta seguimiento ocular (cámara)    |
      | HeartRate   | Monitoreo parcial: falta frecuencia cardiaca (wearable) |

  Scenario: Pérdida de todas las señales
    When el gateway de la cabina informa la pérdida de la señal "EyeTracking"
    And el gateway de la cabina informa la pérdida de la señal "HeartRate"
    Then la sesión de monitoreo queda en estado "Unavailable"
    And el panel del operador muestra el mensaje "Monitoreo no disponible"

  Scenario: Recuperación de la señal perdida
    Given el gateway de la cabina informa la pérdida de la señal "HeartRate"
    When el gateway de la cabina informa la recuperación de la señal "HeartRate"
    Then la sesión de monitoreo queda en estado "Active"
    And el historial de señales de la sesión registra:
      | senal     | evento   |
      | HeartRate | Lost     |
      | HeartRate | Restored |

  Scenario: Fin del turno
    When el operador finaliza el monitoreo
    Then la sesión de monitoreo queda en estado "Stopped"
    And la sesión ya no acepta reportes de señales
