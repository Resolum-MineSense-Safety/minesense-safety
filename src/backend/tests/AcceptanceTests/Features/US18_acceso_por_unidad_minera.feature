# EP08 - Identity & Access Service: the administrator grants and revokes mining unit scopes.
# These scenarios sign in as the bootstrap administrator of the in-memory host.
@solo-en-memoria
Feature: US18 - Acceso de los usuarios por unidad minera
  Como administrador de la plataforma
  quiero definir a qué unidades mineras accede cada usuario
  para que cada supervisor vea solo la operación que le corresponde.

  Scenario: Concesión de una unidad minera
    Given un usuario registrado con el rol "Supervisor"
    When el administrador le concede acceso a una unidad minera
    And el usuario inicia sesión con su contraseña
    Then su sesión incluye esa unidad minera

  Scenario: Revocación de una unidad minera
    Given un usuario registrado con el rol "Supervisor"
    And el administrador le concede acceso a una unidad minera
    When el administrador le revoca el acceso a esa unidad minera
    And el usuario inicia sesión con su contraseña
    Then su sesión no incluye ninguna unidad minera

  Scenario: Un supervisor no puede conceder accesos
    Given un usuario registrado con el rol "Supervisor"
    And el usuario inicia sesión con su contraseña
    When el usuario intenta concederse acceso a una unidad minera
    Then el sistema responde con el código 403

  Scenario: Usuario desactivado
    Given un usuario registrado con el rol "Operator"
    When el administrador desactiva al usuario
    And el usuario inicia sesión con su contraseña
    Then el sistema responde con el código 401
