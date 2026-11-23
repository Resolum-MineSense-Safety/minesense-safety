# EP11 - Analytics Service (TS-22): fatigue alert trends by operational dimension.
Feature: US24 - Tendencias de fatiga por contexto operativo
  Como jefe de seguridad
  quiero ver en qué turnos, ubicaciones y flotas se concentran las alertas de fatiga
  para orientar las medidas preventivas donde más se necesitan.

  Scenario Outline: Tendencia agrupada por dimensión
    Given se registraron alertas de fatiga en una unidad minera:
      | turno | ubicacion  | flota   | severidad | cantidad |
      | Night | Tajo Norte | Acarreo | Critical  | 4        |
      | Night | Botadero   | Acarreo | Warning   | 3        |
      | Day   | Botadero   | Carguío | Warning   | 3        |
    When el analista consulta la tendencia agrupada por "<dimension>"
    Then la tendencia suma 10 alertas y es representativa
    And el grupo "<grupo>" tiene <alertas> alertas, <criticas> críticas y una proporción de <proporcion>

    Examples:
      | dimension | grupo      | alertas | criticas | proporcion |
      | Shift     | Night      | 7       | 4        | 0.7        |
      | Location  | Botadero   | 6       | 0        | 0.6        |
      | Location  | Tajo Norte | 4       | 4        | 0.4        |
      | Fleet     | Carguío    | 3       | 0        | 0.3        |

  Scenario: Muestra insuficiente
    Given se registraron alertas de fatiga en una unidad minera:
      | turno | ubicacion  | flota   | severidad | cantidad |
      | Day   | Tajo Norte | Acarreo | Warning   | 4        |
    When el analista consulta la tendencia agrupada por "Shift"
    Then la tendencia no es representativa y advierte que se necesitan al menos 10 observaciones

  Scenario: Alerta registrada dos veces
    Given se registraron alertas de fatiga en una unidad minera:
      | turno | ubicacion  | flota   | severidad | cantidad |
      | Day   | Tajo Norte | Acarreo | Warning   | 2        |
    When la última alerta se registra de nuevo
    And el analista consulta la tendencia agrupada por "Shift"
    Then la tendencia suma 2 alertas

  Scenario: Dimensión desconocida
    When el analista consulta la tendencia agrupada por "Clima"
    Then el sistema responde con el código 422
