# Non-functional: latency of the critical operations, measured on the client with a Stopwatch.
# In memory the thresholds check the code path of each API; against a deployed environment they
# include the network, the gateway and the database, so they are wider.
@no-funcional @rendimiento
Feature: RNF01 - Rendimiento de las operaciones críticas
  Como jefe de seguridad
  quiero que las evaluaciones, las alertas y las consultas del centro de control respondan rápido
  para que la advertencia llegue al operador y al supervisor a tiempo.

  Scenario Outline: Latencia con solicitudes secuenciales
    Given la operación "<operacion>" tiene datos de prueba
    When se ejecutan <solicitudes> solicitudes secuenciales de "<operacion>"
    Then todas las solicitudes responden con éxito
    And el percentil 95 de la latencia es menor a <p95_memoria> ms en memoria y a <p95_desplegado> ms en el entorno desplegado

    Examples:
      | operacion                   | solicitudes | p95_memoria | p95_desplegado |
      | evaluación de fatiga        | 50          | 200         | 1500           |
      | emisión de alerta           | 50          | 200         | 1500           |
      | estado de la flota          | 50          | 200         | 1500           |
      | bandeja de incidentes       | 50          | 200         | 1500           |
      | tendencia de fatiga         | 30          | 300         | 2000           |
      | inicio de sesión            | 20          | 1000        | 3000           |

  Scenario Outline: Latencia con clientes concurrentes
    Given la operación "<operacion>" tiene datos de prueba
    When se ejecutan <solicitudes> solicitudes de "<operacion>" con <clientes> clientes en paralelo
    Then todas las solicitudes responden con éxito
    And el percentil 95 de la latencia es menor a <p95_memoria> ms en memoria y a <p95_desplegado> ms en el entorno desplegado

    Examples:
      | operacion             | solicitudes | clientes | p95_memoria | p95_desplegado |
      | evaluación de fatiga  | 100         | 10       | 500         | 3000           |
      | estado de la flota    | 100         | 10       | 500         | 3000           |
      | bandeja de incidentes | 100         | 10       | 500         | 3000           |
