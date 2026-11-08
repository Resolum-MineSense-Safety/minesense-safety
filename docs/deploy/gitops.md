# Entrega continua sobre Kubernetes: GitOps con Argo CD

Sección 4.2 del informe. MineSense tiene dos modos de despliegue con los **mismos charts de Helm**. Se
cambia de uno a otro con la variable del repositorio `GITOPS_ENABLED`.

| Modo | Cómo llega un cambio al clúster | Cuándo se usa |
|---|---|---|
| **Push** (`GITOPS_ENABLED` vacía) | `cd-deploy.yml` corre `deploy.sh`, que hace `helm upgrade --install` por servicio con OIDC | Primer despliegue de un entorno nuevo y demostraciones con `deploy-all.sh` |
| **GitOps pull** (`GITOPS_ENABLED=true`) | Argo CD dentro del clúster compara Git con el clúster y sincroniza | Operación normal de Staging y Production |

## Piezas

| Archivo | Para qué sirve |
|---|---|
| `infrastructure/gitops/argocd/project.yaml` | `AppProject`: solo este repositorio, solo los namespaces `staging` y `production`, solo objetos con namespace, ventana de sincronización de Production y rol `oncall` |
| `infrastructure/gitops/argocd/applicationset-<env>.yaml` | Una `Application` por microservicio (14), generada desde una lista |
| `infrastructure/gitops/argocd/platform-<env>.yaml` | Gateway (Ingress del ALB) y simulador Edge |
| `infrastructure/gitops/environments/<env>/release.yaml` | **Estado deseado:** el tag de imagen que corre en cada entorno |
| `infrastructure/gitops/environments/<env>/platform/*.yaml` | ARN, URL de SQS y brokers de MSK salidos de Terraform (`gitops-platform-values.sh`). No son secretos |
| `.github/workflows/gitops-promote.yml` | Abre los PR que cambian `release.yaml` |
| `infrastructure/scripts/install-argocd.sh` | Instala Argo CD y aplica el proyecto y las aplicaciones |

## Flujo de promoción entre ambientes

```text
feature/* ──PR──► develop ──► release/x.y.z ──cd-deploy──► imágenes sha-<commit> (Trivy OK) en GHCR
                                                        │
                                gitops-promote (auto)   ▼
                              PR "deploy sha-… to staging" ──merge──► Argo CD sincroniza Staging (automático)
                                                        │
                                 smoke k6 + E2E ok      ▼
                         gitops-promote (manual) PR "promote sha-… to production" ──aprobación──► merge en main
                                                        │
                                                        ▼
                           Guardia: Sync de *-production en Argo CD, dentro de la ventana (L-V 08:00-17:00)
```

- **Build once, deploy many:** Production nunca reconstruye. Copia el tag que ya pasó por Staging.
- **Staging** sincroniza solo, con `prune` y `selfHeal`: si alguien cambia algo a mano con `kubectl`, Argo CD
  lo revierte.
- **Production** sincroniza en manual, solo dentro de la ventana y después de la aprobación del PR (protección
  de `main`).

## Estrategia de despliegue: *rolling update* sin pérdida de capacidad

Está en `templates/deployment.yaml` del chart `minesense-service`:

```yaml
strategy:
  type: RollingUpdate
  rollingUpdate:
    maxUnavailable: 0   # nunca baja de la capacidad actual
    maxSurge: 1         # crea un pod nuevo, espera su readiness y recién retira uno viejo
```

Junto con el `readinessProbe` (`/health/ready`), un pod nuevo solo recibe tráfico cuando su base de datos y
sus dependencias responden. El `PodDisruptionBudget` de Production (`minAvailable: 1`) protege los
servicios durante una actualización de nodos.

Se eligió *rolling update* y no *blue-green* ni *canary* porque:

1. Cada servicio es *stateless* y las APIs son retrocompatibles dentro de `/api/v1`, así que dos versiones
   pueden convivir unos segundos.
2. Un *blue-green* duplicaría los 15 servicios en un clúster de 2 nodos, con un costo que Staging no justifica.
3. Para *canary* hace falta dividir el tráfico por peso (Argo Rollouts o un service mesh). Queda en el roadmap
   para `fatigue-detection-service` y `alert-service`, que son el camino crítico.

## Estrategia de rollback

| Situación | Acción | Tiempo aproximado |
|---|---|---|
| Un servicio no queda listo durante `deploy.sh` | **Automático:** `deploy.sh` ejecuta `helm rollback <svc> 0` a la última revisión buena | 1-3 min |
| Una versión ya desplegada genera errores (alerta `MineSenseErrorBudgetFastBurn`) | **GitOps:** `git revert` del commit de `release.yaml` y merge. Argo CD vuelve al tag anterior | 2-5 min |
| Urgencia fuera de horario | `argocd app rollback <svc>-production <id>` o `helm rollback <svc> 0 -n production` (rol `minesense-oncall`) | < 2 min |
| Modo push | Volver a ejecutar `cd-deploy.yml` (workflow_dispatch) con `image_tag` = tag estable | 5-10 min |

El historial de cada release queda en Helm (`revisionHistoryLimit: 5`) y en Git (`release.yaml`). Así se puede
saber qué versión corría en cada momento, y eso alimenta las métricas DORA de la sección 4.6.

## Demostración del rollback

```bash
NS=staging SVC=alert-service
helm history $SVC -n $NS                    # revisiones: N-1 (estable) y N (actual)
helm upgrade $SVC infrastructure/helm/minesense-service -n $NS --reuse-values \
  --set-string image.tag=sha-no-existe       # despliegue roto a propósito
kubectl -n $NS rollout status deploy/$SVC --timeout=90s   # falla: ImagePullBackOff, el pod viejo sigue sirviendo
helm rollback $SVC 0 -n $NS --wait          # vuelve a la revisión estable
kubectl -n $NS get pods -l app.kubernetes.io/name=$SVC
```

Como `maxUnavailable: 0`, el pod viejo **nunca deja de atender** mientras el nuevo falla. El rollback no tiene
caída.
