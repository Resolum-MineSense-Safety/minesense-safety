#!/usr/bin/env bash
# Deploys one environment from nothing with a single command: state backend, Lambda package,
# Terraform, kubeconfig, secrets, Helm releases, demo data and final status.
# Every step is idempotent: if something fails, fix it and run the same command again.
#
#   infrastructure/scripts/deploy-all.sh staging sha-<commit>
#
# Optional environment variables:
#   GHCR_TOKEN, GHCR_USERNAME   token with read:packages when the GHCR packages are private
#   TF_STATE_BUCKET             state bucket (default minesense-safety-tfstate-<account id>)
#   TF_VAR_enable_kafka=false   no MSK Serverless (about 0.78 USD/h less; events are only logged)
#   API_HOST, ACM_CERTIFICATE_ARN   public host and HTTPS certificate; without them the ALB is HTTP only
#   SKIP_DEMO_SEED=true         do not load the demo data
set -euo pipefail
source "$(dirname "$0")/lib/common.sh"

ENVIRONMENT="${1:?usage: deploy-all.sh <staging|production> <image-tag>}"
IMAGE_TAG="${2:?usage: deploy-all.sh <staging|production> <image-tag>, e.g. sha-<commit>}"
require_environment "$ENVIRONMENT"
require_tools aws terraform helm kubectl dotnet curl openssl
SCRIPTS="$REPO_ROOT/infrastructure/scripts"
STACK="$(stack_dir "$ENVIRONMENT")"
REGISTRY_PATH="resolum-minesense-safety"
IMAGES="identity-access-service operations-service device-management-service monitoring-service
  fatigue-detection-service alert-service fleet-monitoring-service incident-management-service
  privacy-audit-service reporting-service analytics-service feedback-reliability-service
  edge-synchronization-service supervision-dashboard edge-agent"

log "1/8 Identidad y herramientas"
check_identity
ACCOUNT_ID="$(aws_account_id)"
kubectl version --client 2>/dev/null | tr -d '\r' | head -n 1 || true
terraform version 2>/dev/null | tr -d '\r' | head -n 1 || true
helm version --short 2>/dev/null | tr -d '\r' || true
[[ "$ENVIRONMENT" == staging ]] && echo "Costo estimado de Staging encendido: ~1.25 USD/h (~30 USD/día). Apágalo con destroy-all.sh."

# ---------- Images: fail now instead of after 30 minutes of Terraform ----------
# registry_token <repository>: bearer token for GHCR; anonymous unless GHCR_TOKEN is set.
registry_token() {
  local url="https://ghcr.io/token?scope=repository:$REGISTRY_PATH/$1:pull" response
  if [[ -n "${GHCR_TOKEN:-}" ]]; then
    response="$(printf 'user = "%s:%s"\n' "${GHCR_USERNAME:-minesense}" "$GHCR_TOKEN" | curl -s --max-time 20 -K - "$url")"
  else
    response="$(curl -s --max-time 20 "$url")"
  fi
  printf '%s' "$response" | json_field token
}

log "2/8 Imágenes $IMAGE_TAG en GHCR"
missing=""
for image in $IMAGES; do
  token="$(registry_token "$image")"
  code="000"
  if [[ -n "$token" ]]; then
    code="$(curl -s --max-time 20 -o /dev/null -w '%{http_code}' -I \
      -H "Authorization: Bearer $token" \
      -H "Accept: application/vnd.oci.image.manifest.v1+json, application/vnd.oci.image.index.v1+json, application/vnd.docker.distribution.manifest.v2+json, application/vnd.docker.distribution.manifest.list.v2+json" \
      "https://ghcr.io/v2/$REGISTRY_PATH/$image/manifests/$IMAGE_TAG")"
  fi
  [[ "$code" == 200 ]] || missing="$missing $image"
done
unset token
if [[ -n "$missing" ]]; then
  cat >&2 <<EOF
ERROR: no se pudo leer $IMAGE_TAG de:$missing
Dos causas posibles:
 - El tag no existe: construye las imágenes con el workflow cd-deploy.yml (docs/deploy/aws-staging.md).
 - Los paquetes son privados: exporta un token con read:packages, sin escribirlo en un archivo:
     gh auth refresh -s read:packages && export GHCR_TOKEN="\$(gh auth token)" GHCR_USERNAME="\$(gh api user --jq .login)"
EOF
  exit 1
fi
echo "Las 15 imágenes existen."

log "3/8 Backend de estado (bootstrap)"
BUCKET="$(state_bucket_name)"
OIDC_ARN="arn:aws:iam::$ACCOUNT_ID:oidc-provider/token.actions.githubusercontent.com"
bucket_exists=false; table_exists=false; oidc_exists=false
aws s3api head-bucket --bucket "$BUCKET" >/dev/null 2>&1 && bucket_exists=true
aws dynamodb describe-table --table-name "$TF_LOCK_TABLE" >/dev/null 2>&1 && table_exists=true
aws iam get-open-id-connect-provider --open-id-connect-provider-arn "$OIDC_ARN" >/dev/null 2>&1 && oidc_exists=true
if [[ "$bucket_exists" == true && "$table_exists" == true && "$oidc_exists" == true ]]; then
  echo "Ya existe: s3://$BUCKET, tabla $TF_LOCK_TABLE y proveedor OIDC de GitHub."
else
  BOOTSTRAP="$TF_DIR/bootstrap"
  terraform -chdir="$BOOTSTRAP" init -input=false >/dev/null
  # There can only be one provider per account: create it only if this stack does not manage it yet.
  create_oidc=true
  if [[ "$oidc_exists" == true ]] &&
    ! terraform -chdir="$BOOTSTRAP" state list 2>/dev/null | tr -d '\r' | grep -q 'aws_iam_openid_connect_provider.github'; then
    create_oidc=false
  fi
  terraform -chdir="$BOOTSTRAP" apply -input=false -auto-approve \
    -var "aws_region=$AWS_REGION" -var "state_bucket_name=$BUCKET" -var "lock_table_name=$TF_LOCK_TABLE" \
    -var "create_github_oidc_provider=$create_oidc"
fi

log "4/8 Paquete de la Lambda ReportPdfGenerator"
LAMBDA_PROJECT="$REPO_ROOT/src/serverless/ReportPdfGenerator"
LAMBDA_ZIP="$LAMBDA_PROJECT/artifacts/ReportPdfGenerator.zip"
if [[ -f "$LAMBDA_ZIP" ]] && [[ -z "$(find "$LAMBDA_PROJECT" -newer "$LAMBDA_ZIP" -type f \( -name '*.cs' -o -name '*.csproj' \) -print -quit)" ]]; then
  echo "Sin cambios desde el último empaquetado: $LAMBDA_ZIP"
else
  bash "$SCRIPTS/package-lambda.sh"
fi

log "5/8 Terraform apply de $ENVIRONMENT (la primera vez tarda 30-40 minutos)"
terraform_init "$STACK" "$BUCKET"
terraform -chdir="$STACK" apply -input=false -auto-approve -var "aws_region=$AWS_REGION"

log "6/8 Conexión al clúster y secretos"
CLUSTER_NAME="$(tf_output "$STACK" cluster_name)"
aws eks update-kubeconfig --name "$CLUSTER_NAME" --region "$AWS_REGION" >/dev/null
kubectl get nodes
bash "$SCRIPTS/create-k8s-secrets.sh" "$ENVIRONMENT"

log "7/8 Helm: controlador del ALB, servicios, dashboard, gateway y simulador Edge"
EKS_CLUSTER_NAME="$CLUSTER_NAME" \
  VPC_ID="$(tf_output "$STACK" vpc_id)" \
  LB_CONTROLLER_ROLE_ARN="$(tf_output "$STACK" load_balancer_controller_role_arn)" \
  REPORTING_SERVICE_ROLE_ARN="$(tf_output "$STACK" reporting_service_role_arn)" \
  KAFKA_BOOTSTRAP_SERVERS="$(tf_output "$STACK" kafka_bootstrap_brokers)" \
  KAFKA_CLIENT_ROLE_ARN="$(tf_output "$STACK" kafka_client_role_arn)" \
  bash "$SCRIPTS/deploy.sh" "$ENVIRONMENT" "$IMAGE_TAG"

address="$(kubectl get ingress minesense-api -n "$ENVIRONMENT" -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' | tr -d '\r')"
scheme="http"; [[ -n "${ACM_CERTIFICATE_ARN:-}" ]] && scheme="https"
PUBLIC_URL="$scheme://${API_HOST:-$address}"

log "8/8 Datos de demostración"
# seed-demo.mjs must run once on an empty environment; a ConfigMap remembers that it ran.
if [[ "${SKIP_DEMO_SEED:-false}" == true ]]; then
  echo "Omitido (SKIP_DEMO_SEED=true)."
elif kubectl get configmap minesense-demo-seed -n "$ENVIRONMENT" >/dev/null 2>&1; then
  echo "Ya cargados en una ejecución anterior."
elif ! command -v node >/dev/null 2>&1; then
  warn "No hay Node.js en PATH: los datos de demostración no se cargaron. Instálalo (18+) y repite este comando."
else
  admin_password="$(read_secret_key "$ENVIRONMENT" identity-access-secrets IdentityAccess__BootstrapAdministrator__Password)"
  demo_password="$(read_secret_key "$ENVIRONMENT" minesense-demo-credentials DEMO_PASSWORD)"
  if BASE_URL="$PUBLIC_URL" ADMIN_USER=admin ADMIN_PASSWORD="$admin_password" DEMO_PASSWORD="$demo_password" \
    node "$REPO_ROOT/src/frontend/scripts/seed-demo.mjs"; then
    kubectl create configmap minesense-demo-seed -n "$ENVIRONMENT" --from-literal=image-tag="$IMAGE_TAG" >/dev/null
  else
    warn "La carga de datos de demostración falló; el despliegue sigue en pie. El script no es repetible: revisa los datos antes de reintentar (docs/deploy/aws-staging.md)."
  fi
  unset admin_password demo_password
fi

bash "$SCRIPTS/status.sh" "$ENVIRONMENT"

cat <<EOF

================================================================
 MineSense Safety en $ENVIRONMENT: $PUBLIC_URL
 Usuario: admin. Contraseña:
   kubectl get secret identity-access-secrets -n $ENVIRONMENT -o jsonpath='{.data.IdentityAccess__BootstrapAdministrator__Password}' | base64 -d
 Usuarios demo (supervisor, jefe.seguridad, operador.rosa):
   kubectl get secret minesense-demo-credentials -n $ENVIRONMENT -o jsonpath='{.data.DEMO_PASSWORD}' | base64 -d
 Apagar y dejar de pagar: infrastructure/scripts/destroy-all.sh $ENVIRONMENT
================================================================
EOF
