# EP05 - Incident Management Service: actions taken to protect the operator and documented closure.
Feature: US10 - Registro de acciones y cierre de incidentes
  Como supervisor de seguridad
  quiero documentar las acciones tomadas y cerrar el incidente con su resolución
  para dejar evidencia de cómo se protegió al operador.

  Background:
    Given se recibió una alerta "Critical" de un operador
    And un supervisor toma el incidente

  Scenario Outline: Registro de una acción de protección
    When el supervisor registra la acción "<tipo>" con el resultado "<resultado>"
    Then el incidente muestra la acción "<tipo>" con el resultado "<resultado>"

    Examples:
      | tipo            | resultado                      |
      | OperatorContact | Operador responde por radio    |
      | PreventivePause | Pausa de 15 minutos            |
      | OperatorRelief  | Relevo por operador de reserva |
      | Other           | Evaluación médica en tópico    |

  Scenario: Acción sin resultado
    When el supervisor registra la acción "OperatorContact" sin indicar el resultado
    Then el sistema responde con el código 400

  Scenario: Cierre sin acciones registradas
    When el supervisor cierra el incidente 10 minutos después de su apertura con la resolución "Sin novedad"
    Then el sistema responde con el código 409
    And el mensaje de error menciona "no action has been recorded"

  Scenario: Cierre documentado del incidente
    Given el supervisor registra la acción "PreventivePause" con el resultado "Pausa de 15 minutos"
    When el supervisor cierra el incidente 10 minutos después de su apertura con la resolución "Operador descansó y retomó la operación"
    Then el incidente queda "Closed" con un tiempo de atención de 600 segundos
    And el historial del incidente registra en orden los eventos:
      | evento         |
      | Opened         |
      | Assigned       |
      | ActionRecorded |
      | Closed         |
    And ya no se pueden registrar acciones en el incidente
