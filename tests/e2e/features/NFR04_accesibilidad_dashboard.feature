# Non-functional user story NFR04 (accessibility, section 4.4.5 and 4.8).
# Automated with playwright-bdd + axe-core: npx bddgen && npx playwright test --project=bdd-chromium
@nfr @accesibilidad
Feature: NFR04 - Accesibilidad del Supervision Dashboard
  Como supervisor de seguridad con baja visión o que trabaja con teclado
  quiero que el dashboard cumpla las pautas WCAG 2.1 nivel AA
  para leer y atender las alertas sin depender del mouse ni de una visión perfecta.

  Scenario Outline: Cada módulo principal cumple WCAG 2.1 AA
    Given el supervisor inició sesión en el dashboard
    When abre el módulo "<modulo>"
    Then axe no encuentra violaciones de impacto "serious" ni "critical"
    And el contraste de todo el texto es de al menos 4.5 a 1

    Examples:
      | modulo            |
      | Centro de control |
      | Flota             |
      | Alertas           |
      | Incidentes        |
      | Operaciones       |
      | Reportes          |

  Scenario: El ingreso se puede completar solo con teclado
    Given el supervisor abre la página de ingreso
    When completa el formulario con el teclado
      | campo      | tecla |
      | Usuario    | Tab   |
      | Contraseña | Enter |
    Then entra al "Centro de control" sin usar el mouse

  Scenario: Los controles tienen un nombre accesible
    Given el supervisor abre la página de ingreso
    Then estos controles se encuentran por su nombre accesible
      | rol     | nombre     |
      | textbox | Usuario    |
      | textbox | Contraseña |
      | button  | Entrar     |
