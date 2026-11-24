# Despliegue de Staging en AWS con un solo comando

Levanta MineSense Safety completo en AWS: red, EKS, RDS PostgreSQL, Amazon DocumentDB, MSK Serverless, IoT Core, Lambda de reportes, los 13 microservicios, el dashboard, el gateway (ALB) y el simulador Edge.

> **Estado de validación.** Nada de esto se ha aplicado todavía contra una cuenta de AWS. Lo validado sin AWS: `terraform validate` y `fmt`, `helm lint` y kubeconform, sintaxis de los scripts, empaquetado real de la Lambda, `create-k8s-secrets.sh` contra `aws`/`kubectl` simulados y pruebas unitarias de la validación TLS. Conviene reservar tiempo para depurar el primer `apply`: ver [Riesgos del primer despliegue](#riesgos-del-primer-despliegue).

## Costo por hora

Precios aproximados de `us-east-1`; se cobran desde el `apply` hasta el `destroy`, aunque nadie use el sistema.

| Recurso | Detalle | USD/hora |
| --- | --- | --- |
| MSK Serverless | 0.75 por clúster + 21 particiones × 0.0015 (7 tópicos × 3) | 0.78 |
| Nodos EKS | 2 × t3.large (0.0832) | 0.17 |
| Plano de control EKS | Soporte estándar; una versión en soporte extendido costaría 0.60 | 0.10 |
| DocumentDB | 1 × db.t4g.medium (precio no confirmado en la lista oficial; ±0.08) | 0.08 |
| NAT Gateway | Sin contar 0.045 por GB | 0.045 |
| ALB | Hora + LCU mínimo | 0.03 |
| RDS PostgreSQL | db.t4g.micro + 20 GB | 0.02 |
| IPv4 públicas y discos | 3 IP × 0.005 + 40 GB de nodos | 0.02 |
| **Total** | | **≈ 1.25 USD/h (≈ 30 USD/día)** |

Sin Kafka baja a **≈ 0.47 USD/h**: `export TF_VAR_enable_kafka=false` antes de `deploy-all.sh`. Los servicios solo registran sus eventos en el log, y la pantalla Cabina del dashboard (compilada con `VITE_EVENT_DRIVEN=true`) no verá la cadena alerta → incidente hecha por el backend. Es un ahorro real, pero esa parte de la evidencia se pierde.

Para la evidencia basta un rato: despliega, captura y ejecuta `destroy-all.sh`. Un `apply` más un `destroy` tardan cerca de una hora en total, unos 2 USD. **Configura un presupuesto con alerta en AWS Budgets antes de empezar.**

## 1. Preparar la máquina (una vez)

En PowerShell, desde la raíz del repositorio:

```powershell
powershell -ExecutionPolicy Bypass -File infrastructure\scripts\setup-aws-tools.ps1
```

Instala con winget AWS CLI v2, Terraform, Helm y kubectl, y ejecuta `aws configure` para que escribas tus credenciales. Quedan en `%USERPROFILE%\.aws`; nunca las pegues en el repositorio ni en un chat.

También hacen falta Git Bash y el SDK de .NET (8 o superior), y Node.js 18+ para cargar los datos de demostración. El usuario de IAM necesita permisos amplios (`AdministratorAccess` es lo simple en una cuenta de curso) y la cuenta debe permitir EKS, MSK y DocumentDB: el plan gratuito de AWS puede bloquearlos.

Docker Desktop trae un `kubectl` antiguo que puede ir primero en `PATH`. `deploy-all.sh` imprime la versión en uso: debe estar a una versión menor de 1.35 como máximo.

## 2. Imágenes

Las imágenes salen del workflow [cd-deploy.yml](https://github.com/Resolum-MineSense-Safety/minesense-safety/blob/develop/.github/workflows/cd-deploy.yml), con tag `sha-<commit>`: se publican 15 (13 servicios, `supervision-dashboard` y `edge-agent`), escaneadas con Trivy. Se generan al hacer push a `release/*` o a mano:

```bash
gh workflow run cd-deploy.yml --ref <rama> -f environment=staging
```

El despliegue dentro de ese workflow sigue apagado mientras `EKS_DEPLOY_ENABLED` no sea `true`, así que solo construye.

Los paquetes de GHCR son **privados** (el registro responde `authentication required` sin token), así que el clúster necesita una credencial. `deploy-all.sh` crea el secreto `ghcr-pull` y lo usa en todos los pods si exportas un token con `read:packages`:

```bash
gh auth refresh -s read:packages
export GHCR_TOKEN="$(gh auth token)" GHCR_USERNAME="$(gh api user --jq .login)"
```

La alternativa sin token es volver públicos los 15 paquetes en `https://github.com/orgs/Resolum-MineSense-Safety/packages` → paquete → *Package settings* → *Change visibility* (GitHub no ofrece API para esto).

## 3. Desplegar

En Git Bash, desde la raíz del repositorio:

```bash
infrastructure/scripts/deploy-all.sh staging sha-<commit>
```

La primera vez tarda 40-60 minutos. Si algo falla, corrige la causa y repite **el mismo comando**: cada paso es idempotente.

| Paso | Qué hace |
| --- | --- |
| 1 | Comprueba la identidad con `aws sts get-caller-identity` y las herramientas. |
| 2 | Verifica que las 15 imágenes existan antes de gastar en infraestructura. |
| 3 | Crea, si faltan, el bucket de estado `minesense-safety-tfstate-<id de cuenta>`, la tabla de bloqueo y el proveedor OIDC de GitHub. |
| 4 | Empaqueta la Lambda para `dotnet8` arm64 (`package-lambda.sh`). |
| 5 | `terraform init` y `apply` de `environments/staging`. |
| 6 | Configura `kubectl` y crea los secretos (`create-k8s-secrets.sh`). |
| 7 | Instala el AWS Load Balancer Controller, los servicios, el dashboard, el gateway y el simulador Edge (`deploy.sh`). |
| 8 | Carga los datos de demostración una vez, imprime el estado y la URL pública. |

Al final imprime la URL (`http://<alb>...`) y el comando para leer la contraseña de `admin`.

### Variables opcionales

| Variable | Efecto |
| --- | --- |
| `TF_STATE_BUCKET` | Otro nombre para el bucket de estado. |
| `TF_VAR_enable_kafka=false` | Sin MSK Serverless. |
| `API_HOST`, `ACM_CERTIFICATE_ARN` | Dominio y certificado: el ALB pasa a HTTPS. Sin ellos es **solo HTTP**. |
| `SERVICE_TIMEOUT` | Espera por servicio (por defecto `6m`). |
| `SKIP_DEMO_SEED=true` | No carga datos de demostración. |

## 4. Evidencia

```bash
infrastructure/scripts/status.sh staging
```

Imprime nodos, `kubectl get pods,svc,ingress`, `/health/ready` de cada servicio por `port-forward` (esas rutas no se publican en el ALB), el JWKS público, la respuesta del dashboard y de la API, los últimos lotes del simulador Edge y el estado de los consumidores de Kafka.

| Exigencia de la entrega | Dónde se demuestra |
| --- | --- |
| Microservicios contenedorizados en el registro | Paquetes de GHCR y `cd-deploy.yml` |
| CI/CD funcionando | Ejecuciones de GitHub Actions |
| Despliegue cloud con IaC | `terraform` + `status.sh` |
| Pruebas de aceptación en el pipeline | `acceptance-tests.yml` (corren dentro del pipeline, no contra Staging) |
| Backend, frontend, base de datos y edge | URL pública, `kubectl get pods`, logs de `edge-agent` y lotes en la pantalla Edge |

## 5. Apagar

```bash
infrastructure/scripts/destroy-all.sh staging
```

Pide escribir `staging` para confirmar (`--yes` lo omite). Primero quita el Ingress y espera a que el controlador borre el ALB, luego las aplicaciones y el controlador, y después ejecuta `terraform destroy` (20-30 minutos). Si un ALB o security group quedó huérfano, lo borra para que la VPC no bloquee el destroy. Termina listando lo que queda: bucket de estado, tabla de bloqueo y proveedor OIDC, que cuestan centavos, con los comandos para borrarlos.

**Se borran los datos de RDS y DocumentDB, sin snapshot final.** Comprueba en la consola de AWS que no quede nada encendido.

## Decisiones de Staging

**Amazon DocumentDB 5.0.** Se fija la versión del motor porque la predeterminada podría no ser compatible con el driver 3.x.

- Conexión con `tls=true`, `replicaSet=rs0`, `readPreference=primary` y `retryWrites=false` (las escrituras reintentables solo existen desde el motor 8.0.2).
- El driver de .NET ignora `tlsCAFile`: `MongoDb__TlsCaFile` apunta al `global-bundle.pem` de Amazon, montado desde el ConfigMap `documentdb-ca`, y `MongoClientFactory` valida el certificado solo contra ese paquete, sin afectar a las demás conexiones TLS.
- Índices: el `unique + sparse` de Alerts (un solo campo no multivalor) y el TTL de `processed_events` son compatibles, así que **no se apagó nada**. La consulta por `AssessmentId` añade `$exists` porque sin él DocumentDB no usa el índice disperso, y los índices se crean de uno en uno porque solo admite una construcción por colección.

**MSK Serverless.** El cliente del PR #72 ya soportaba SASL/IAM (`Kafka__Authentication=AwsMskIam`), pero ningún servicio tenía permisos. Ahora Terraform crea un rol IRSA compartido (`kafka_client_role_arn`) para los 7 servicios que usan eventos: alerts, analytics, fatigue, feedback, fleet, incidents y privacy.

**PostgreSQL.** Cada servicio crea su base al arrancar con el usuario maestro de RDS, que tiene `CREATEDB`: `minesense_identity`, `minesense_operations`, `minesense_devices` y `minesense_monitoring`. RDS rota esa contraseña cada 7 días. Si el entorno vive más, los servicios dejan de autenticarse hasta repetir `create-k8s-secrets.sh staging`, que actualiza los secretos y reinicia lo afectado.

**Simulador Edge.** Corre como Deployment `edge-agent` sin probes HTTP, con sistema de archivos de solo lectura. Su buffer SQLite va en `emptyDir`: sobrevive al reinicio del contenedor, no al borrado del pod. `deploy.sh` registra el usuario `edge-cabin-01` (rol Operator) y guarda las credenciales en `edge-agent-secrets`.

**Capacidad.** Las solicitudes suman ≈ 2 vCPU y 3 GiB con los componentes del sistema, frente a ≈ 3.8 vCPU y 14 GiB asignables en 2 t3.large (70 pods). Alcanza; una t3.medium quedaría justa en memoria y número de pods.

**Reporting.** La Lambda, la cola SQS y el rol se crean, pero el Reporting Service de esta rama aún genera los reportes en proceso (`InProcessReportRenderer`). Conectarlo a la cola es trabajo de otra rama: la Lambda se puede demostrar con un mensaje de `sample-request.json`, no desde la aplicación.

## Riesgos del primer despliegue

| Riesgo | Síntoma | Qué hacer |
| --- | --- | --- |
| Cuenta sin acceso o con límites | `SubscriptionRequiredException`, `OptInRequired`, `LimitExceeded` en Terraform | Pasar al plan de pago o pedir cuota. |
| Driver 3.x contra DocumentDB | El servicio no arranca: error de autenticación o de versión de wire | Quitar `authMechanism` del secreto, o probar `documentdb_engine_version`; `deploy.sh` imprime los logs. |
| Certificado de DocumentDB | `The remote certificate is invalid` | Revisar que exista el ConfigMap `documentdb-ca` y su montaje. |
| IAM de MSK | Logs con `Kafka consumer error` o `SASL authentication error` | Revisar la anotación del ServiceAccount y `kafka_client_role_arn`. |
| Evento publicado a un tópico aún sin consumidor | `was not delivered` en los logs (MSK Serverless no crea tópicos solo) | Los consumidores crean sus tópicos al arrancar; se corrige al subir todos. |
| Controlador del ALB | El Ingress queda sin dirección | `deploy.sh` imprime el `describe` y los logs del controlador. |
| HTTP sin cifrar | Contraseñas viajan sin TLS en Staging | Aceptable solo para la demo; usar `ACM_CERTIFICATE_ARN` si hay dominio. |
| Seed interrumpido | `seed-demo.mjs` no es repetible (conflicto 409) | Sin datos reales lo más simple es `destroy-all.sh` y empezar de nuevo, o usar `SKIP_DEMO_SEED=true`. |
| Recrear justo tras destruir | Nombre de thing type de IoT en uso | Esperar unos minutos y repetir `deploy-all.sh`. |
