# EP09 - Privacy & Audit Service: append-only, hash-chained log of every access to sensitive data.
Feature: US21 - Registro de accesos a datos sensibles
  Como responsable de privacidad
  quiero un registro inalterable de cada acceso a los datos de un operador
  para auditar quién consultó qué información y con qué finalidad.

  Background:
    Given se registraron los accesos a los datos biométricos de un operador:
      | campos       | finalidad             | decision |
      | perclos, hrv | fatigue-monitoring    | Granted  |
      | heartRate    | operational-analytics | Denied   |
      | perclos      | safety-alerts         | Granted  |

  Scenario Outline: Consulta del registro por decisión
    When el auditor consulta los accesos del operador con la decisión "<decision>"
    Then obtiene <cantidad> accesos sellados con su huella SHA-256

    Examples:
      | decision | cantidad |
      | Granted  | 2        |
      | Denied   | 1        |

  Scenario: Cada acceso queda sellado con una huella propia
    When el auditor consulta todos los accesos del operador
    Then obtiene 3 accesos sellados con su huella SHA-256
    And cada acceso tiene una huella distinta
    And los accesos aparecen del más reciente al más antiguo

  Scenario: Paginación fuera de rango
    When el auditor consulta los accesos del operador con 101 resultados por página
    Then el sistema responde con el código 422
