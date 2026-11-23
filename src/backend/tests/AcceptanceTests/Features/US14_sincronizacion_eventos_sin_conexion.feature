# EP06/EP07 - Edge Synchronization Service: idempotent reception of the batches captured offline.
Feature: US14 - Recuperación de los eventos registrados sin conexión
  Como supervisor de seguridad
  quiero que los eventos que la cabina registró sin conexión lleguen completos a la nube
  para no perder evidencia cuando se recupera la comunicación.

  Scenario: Recepción de un lote pendiente
    Given la cabina acumuló sin conexión los eventos:
      | tipo            | minutos_atras |
      | DeviceHeartbeat | 30            |
      | DeviceHeartbeat | 20            |
      | DeviceHeartbeat | 10            |
    When el gateway de la cabina envía el lote al recuperar la conexión
    Then la nube acepta el lote con sus 3 eventos

  Scenario: Reintento del mismo lote
    Given la cabina acumuló sin conexión los eventos:
      | tipo            | minutos_atras |
      | DeviceHeartbeat | 15            |
    And el gateway de la cabina envía el lote al recuperar la conexión
    When el gateway reenvía el mismo lote con otro contenido
    Then el sistema responde con el código 200
    And la nube conserva el lote original con 1 eventos

  Scenario Outline: Lotes inválidos
    Given la cabina acumuló sin conexión los eventos:
      | tipo            | minutos_atras |
      | DeviceHeartbeat | 5             |
      | DeviceHeartbeat | 3             |
    When el gateway de la cabina envía un lote <defecto>
    Then el sistema responde con el código 422
    And el mensaje de error menciona "<mensaje>"

    Examples:
      | defecto                    | mensaje                            |
      | sin eventos                | between 1 and 500 events           |
      | con eventos repetidos      | Event ids must be unique           |
      | con una secuencia negativa | Sequence number cannot be negative |
      | con un evento sin tipo     | Event type is required             |
