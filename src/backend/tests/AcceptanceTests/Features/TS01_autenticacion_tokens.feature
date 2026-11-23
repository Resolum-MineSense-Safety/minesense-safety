# EP08 - Identity & Access Service (TS-01 to TS-03, ADR-007): sign-in, token rotation and profile.
Feature: TS01 - Inicio de sesión con tokens de acceso
  Como usuario de MineSense Safety
  quiero iniciar sesión y mantenerla de forma segura
  para acceder solo a la información que me corresponde.

  Scenario: Registro e inicio de sesión
    Given un usuario registrado con el rol "Supervisor"
    When el usuario inicia sesión con su contraseña
    Then recibe un token de acceso "Bearer" que vence en 15 minutos como máximo
    And su perfil muestra el rol "Supervisor"

  Scenario Outline: Credenciales inválidas
    Given un usuario registrado con el rol "Operator"
    When se inicia sesión con <credenciales>
    Then el sistema responde con el código 401
    And el mensaje de error menciona "Invalid credentials."

    Examples:
      | credenciales                   |
      | una contraseña incorrecta      |
      | un usuario que no existe       |
      | una contraseña vacía           |

  Scenario: Renovación de la sesión
    Given un usuario registrado con el rol "Supervisor"
    And el usuario inicia sesión con su contraseña
    When el usuario renueva su sesión
    Then recibe un nuevo par de tokens
    And el token de renovación anterior ya no es aceptado

  Scenario: Cierre de sesión
    Given un usuario registrado con el rol "Operator"
    And el usuario inicia sesión con su contraseña
    When el usuario cierra su sesión
    Then su token de renovación ya no es aceptado

  Scenario: Auto-registro como administrador
    When alguien se registra por su cuenta con el rol "Administrator"
    Then el sistema responde con el código 403

  Scenario Outline: Datos de registro inválidos
    When alguien se registra por su cuenta con el rol "<rol>" y la contraseña "<contrasena>"
    Then el sistema responde con el código 422
    And el mensaje de error menciona "<mensaje>"

    Examples:
      | rol        | contrasena      | mensaje                 |
      | Supervisor | corta           | at least 8 characters   |
      | Gerente    | Turno-Noche-2026 | Unknown user role       |
