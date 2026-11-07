# Software Factory de Resolum (sección 3.8)

![Software Factory](https://raw.githubusercontent.com/Resolum-MineSense-Safety/minesense-safety/develop/docs/diagrams/software-factory.png)

## Qué cambió en el tercer entregable

| Etapa | Segundo entregable | Tercer entregable |
| --- | --- | --- |
| Code | GitFlow, Conventional Commits, plantilla de PR, rulesets | Además: Dependabot y política de seguridad (`SECURITY.md`) |
| Build y Test | Backend CI, SonarCloud, Frontend CI | Además: Containers CI, CodeQL, gitleaks, Dependency Review, Terraform CI y Helm CI |
| Release | Sin imágenes publicadas | Imágenes chiseled de 13 servicios y del Dashboard, escaneadas con Trivy y publicadas en GHCR con tag inmutable |
| Deploy | Docker Compose parcial | Compose completo y endurecido; Terraform (3.3), Helm sobre EKS y `cd-deploy.yml` (3.4) |
| Operate | Sin probes | `/health/live` y `/health/ready` en todos los servicios; escaneo semanal de seguridad |

## Workflows

| Archivo | Propósito |
| --- | --- |
| `dotnet-build-and-xunit.yml` | Build y pruebas del backend (obligatorio) |
| `sonarqube-analysis.yml` | Calidad y cobertura en SonarCloud (obligatorio) |
| `react-vite-build.yml` | Lint y build del Dashboard (obligatorio en main) |
| `containers-ci.yml` | Imágenes, compose y gateway |
| `security-scan.yml` | CodeQL, gitleaks y Dependency Review |
| `terraform-validate.yml` | Infraestructura como código |
| `helm-validate.yml` | Charts de Kubernetes |
| `feedback-mongodb-ci.yml` | Integración con MongoDB real |
| `cd-deploy.yml` | Entrega continua a Staging y Production |

## Métricas del proceso (05/10/2026 – 24/10/2026)

| Indicador | Valor |
| --- | --- |
| Pull Requests | 46 (38 integrados, 5 abiertos, 3 cerrados sin integrar) |
| PR integrados con al menos una revisión | 33 de 38 (87 %) |
| Tiempo de apertura a merge | mediana 1,0 h; percentil 75: 2,9 h |
| Commits en develop con Conventional Commits | 106 de 111 (95,5 %); con scope 97 (87,4 %) |
| Integrantes con commits en develop | 8 |
| Ejecuciones de CI | 361; éxito entre 94,7 % y 100 %; medianas menores a 2 min |

Los datos se obtienen con `gh pr list`, `gh run list` y `git log`, de modo que cualquier
integrante puede recalcularlos al cierre de cada sprint.

## Próximos pasos

- Activar los despliegues cuando esté la cuenta de AWS del curso (`EKS_DEPLOY_ENABLED`).
- Ejecutar automáticamente los escenarios Gherkin del repositorio de pruebas de aceptación
  (sección 3.6).
- Monitoreo en el clúster con Prometheus, Grafana y EFK (sección 1.4.7).
- Agregar Containers CI y Security Scanning a los checks obligatorios de los rulesets.
