# Pipelines CI/CD (sección 3.2)

![Pipelines CI/CD](https://raw.githubusercontent.com/Resolum-MineSense-Safety/minesense-safety/develop/docs/diagrams/ci-cd-workflows.png)

Todos los pipelines viven en
[`.github/workflows`](https://github.com/Resolum-MineSense-Safety/minesense-safety/tree/develop/.github/workflows)
y se ejecutan en GitHub Actions. La integración continua valida cada Pull Request antes del
merge. La entrega continua lleva la misma imagen de `release/*` a Staging y luego a Production
([sección 3.4](https://github.com/Resolum-MineSense-Safety/minesense-safety/blob/develop/infrastructure/helm/README.md)).

## Workflows

| Workflow (archivo) | Job | Disparador | Qué valida |
| --- | --- | --- | --- |
| Backend CI (`dotnet-build-and-xunit.yml`) | Build and Test | PR a develop/main, push a develop | Restore con caché de NuGet, build Release y pruebas xUnit de todos los servicios. |
| SonarQube Analysis (`sonarqube-analysis.yml`) | Static Code Analysis | PR a develop/main, push a develop | SonarCloud: bugs, vulnerabilidades, code smells, duplicación y cobertura (OpenCover). |
| Frontend CI (`react-vite-build.yml`) | Build and Lint | PR a develop/main, push a develop | `npm ci` con caché, ESLint y `vite build` del Dashboard. |
| Containers CI (`containers-ci.yml`) | Compose and Gateway Config, Image × 14 | Cambios en código, Dockerfiles o compose | Build de las imágenes, arranque endurecido, probe de salud, usuario no root, Trivy y compose/gateway sincronizados. |
| Infrastructure CI (`terraform-validate.yml`) | Terraform Validate × 3 | Cambios en `infrastructure/terraform` | `terraform fmt -check` y `validate` de bootstrap, staging y production. |
| Deployment CI (`helm-validate.yml`) | Helm Lint and Schema | Cambios en `infrastructure/helm` o `k8s` | `helm lint --strict` (13 servicios × 2 entornos) y kubeconform contra Kubernetes 1.30. |
| Feedback MongoDB Integration (`feedback-mongodb-ci.yml`) | Feedback tests with MongoDB | Cambios en Feedback & Reliability | Pruebas con MongoDB real como *service container* y smoke test del contenedor. |
| Continuous Delivery (`cd-deploy.yml`) | Resolve, Build × 13, Deploy, Open release PR | Push a `release/*` y `main`, manual | Imágenes en GHCR, despliegue con Helm en EKS y PR de promoción. |

## Reglas de las ramas (rulesets)

| Regla | develop | main |
| --- | --- | --- |
| Merge solo por Pull Request | sí | sí |
| Aprobaciones requeridas | 1 | 1 |
| Revisión invalidada al hacer un nuevo push | sí | sí |
| Conversaciones resueltas antes del merge | sí | sí |
| Checks obligatorios | Build and Test, Static Code Analysis | Build and Test, Static Code Analysis, Build and Lint |
| Rama actualizada con la base (strict) | sí | sí |
| Prohibido force push y borrado | sí | sí |
| Historial lineal | no | sí |

## Métricas (05/10/2026 – 24/10/2026)

Datos de 361 ejecuciones obtenidos con `gh run list`:

| Workflow | Ejecuciones | Éxito | Fallos | Mediana |
| --- | --- | --- | --- | --- |
| SonarQube Analysis | 114 | 94,7 % | 6 | 1,7 min |
| Backend CI | 114 | 96,5 % | 4 | 0,8 min |
| Frontend CI | 114 | 96,5 % | 4 | 0,3 min |
| Feedback MongoDB Integration | 7 | 100 % | 0 | 1,4 min |
| Edge CI (Python) | 6 | 100 % | 0 | 0,3 min |
| Infrastructure CI | 5 | 100 % | 0 | 0,4 min |
| Deployment CI | 1 | 100 % | 0 | 0,2 min |

De las 114 ejecuciones de cada workflow principal, 77 corresponden a Pull Requests y 37 a
pushes a develop. Los fallos bloquearon el merge hasta que el autor corrigió el problema en su
propia rama; por ejemplo, el hallazgo S6964 de SonarCloud en los PRs #25–#27.

## Prácticas comunes

- `concurrency` por rama: un nuevo push cancela la ejecución anterior de la misma rama. El
  despliegue nunca se cancela a mitad.
- Permisos mínimos del `GITHUB_TOKEN` (`contents: read`). Solo se agregan
  `pull-requests: read` para SonarCloud, `packages: write` para publicar en GHCR e
  `id-token: write` para OIDC.
- Secretos únicamente en GitHub Secrets (`SONAR_TOKEN`, `SONAR_HOST_URL`). AWS se usa por OIDC,
  sin claves de larga duración.
- Versiones fijadas: .NET 8.0.x, Node 20, Terraform 1.9.8, Helm 3.16.2, kubeconform 0.6.7 y
  `aquasecurity/trivy-action` v0.36.0.
- Caché de NuGet, npm y capas Docker (`type=gha`).
- Filtros `paths`: Terraform, Helm, contenedores y MongoDB solo se ejecutan cuando cambian sus
  archivos, lo que mantiene el feedback de un PR común por debajo de 2 minutos.

## Ejecutar lo mismo en local

```bash
# Backend
cd src/backend && dotnet build MineSenseSafety.sln -c Release && dotnet test MineSenseSafety.sln -c Release

# Frontend
cd src/frontend && npm ci && npm run lint && npm run build

# Contenedores
docker compose config --quiet && python infrastructure/scripts/render-gateway-nginx.py && git diff --exit-code infrastructure/docker/gateway/nginx.conf
```
