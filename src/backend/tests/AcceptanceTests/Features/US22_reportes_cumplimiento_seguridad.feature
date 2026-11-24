# EP10 - Reporting Service: versioned indicators and report jobs with a downloadable artifact.
Feature: US22 - Reportes de cumplimiento de seguridad
  Como jefe de seguridad
  quiero generar reportes de los indicadores de fatiga
  para demostrar el cumplimiento ante la gerencia y los entes reguladores.

  Background:
    Given un indicador de reporte versión "1.0" con las fuentes de datos:
      | fuente    |
      | alerts    |
      | incidents |

  Scenario: Reporte CSV listo para descargar
    When el jefe de seguridad solicita el reporte en formato "Csv" de los últimos 30 días
    Then el reporte queda en estado "Ready" con la versión "1.0" del indicador
    And el archivo descargado coincide con su suma de verificación SHA-256

  # In memory the PDF is rendered in process (Ready); in the cloud it goes to the Lambda (Rendering).
  Scenario: Reporte PDF aceptado para su generación
    When el jefe de seguridad solicita el reporte en formato "Pdf" de los últimos 30 días
    Then el reporte queda aceptado para su generación

  Scenario: Versión ya definida del indicador
    When se define otra vez la versión "1.0" del indicador
    Then el sistema responde con el código 409

  Scenario Outline: Solicitudes de reporte inválidas
    When el jefe de seguridad solicita un reporte <solicitud>
    Then el sistema responde con el código 422
    And el mensaje de error menciona "<mensaje>"

    Examples:
      | solicitud                     | mensaje                                   |
      | de un indicador que no existe | Unknown report definition                 |
      | con el período invertido      | start date must not be after the end date |
      | en un formato no soportado    | Unknown report format                     |
