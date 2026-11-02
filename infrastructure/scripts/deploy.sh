#!/usr/bin/env bash
# Deploys every MineSense Safety microservice, the dashboard, the gateway and (Staging) the Edge
# simulator to one EKS environment. The same script runs in the CD pipeline and by hand (section 3.4).
#
#   infrastructure/scripts/deploy.sh <staging|production> <image-tag>
#
# Requires kubectl already pointing at the cluster (aws eks update-kubeconfig) and Helm 3.
# Optional environment variables:
#   SERVICES                    space-separated subset of services (default: all in values/services)
#   SERVICE_TIMEOUT             how long a release may take to become ready (default 6m)
#   LB_CONTROLLER_ROLE_ARN      IAM role of the AWS Load Balancer Controller (installs/updates it)
#   EKS_CLUSTER_NAME, AWS_REGION, VPC_ID   needed together with LB_CONTROLLER_ROLE_ARN
#   REPORTING_SERVICE_ROLE_ARN  IRSA role of the Reporting Service (Terraform output)
#   REPORT_REQUESTS_QUEUE_URL, REPORTS_BUCKET  serverless PDFs, both or neither (Terraform outputs
#                               report_requests_queue_url, reports_bucket); else Reporting renders in process
#   KAFKA_BOOTSTRAP_SERVERS     MSK Serverless IAM brokers (Terraform output kafka_bootstrap_brokers)
#   KAFKA_CLIENT_ROLE_ARN       IRSA role of the services with domain events (kafka_client_role_arn)
#   API_HOST, ACM_CERTIFICATE_ARN  public host and HTTPS certificate; without them the ALB is HTTP only
#   EDGE_SIMULATOR              true/false; defaults to true in Staging and false in Production
set -euo pipefail
source "$(dirname "$0")/lib/common.sh"

ENVIRONMENT="${1:?usage: deploy.sh <staging|production> <image-tag>}"
IMAGE_TAG="${2:?usage: deploy.sh <staging|production> <image-tag>}"
require_environment "$ENVIRONMENT"
require_tools kubectl helm curl openssl

HELM_DIR="$REPO_ROOT/infrastructure/helm"
ENV_VALUES="$HELM_DIR/values/environments/$ENVIRONMENT.yaml"
SERVICE_TIMEOUT="${SERVICE_TIMEOUT:-6m}"
# identity-access first: the other services validate tokens against its JWKS.
SERVICES="${SERVICES:-identity-access-service $(ls "$HELM_DIR/values/services" | tr -d '\r' | sed 's/\.yaml$//' | grep -v '^identity-access-service$' | tr '\n' ' ')}"
# The services that call AddMineSenseEvents; the same list as modules/messaging in Terraform.
KAFKA_CLIENTS=" alert-service analytics-service edge-synchronization-service fatigue-detection-service feedback-reliability-service fleet-monitoring-service incident-management-service privacy-audit-service "
default_edge=false; [[ "$ENVIRONMENT" == staging ]] && default_edge=true
EDGE_SIMULATOR="${EDGE_SIMULATOR:-$default_edge}"

# Helm "--set" splits on commas, and a broker list has them.
escape_commas() { printf '%s' "${1//,/\\,}"; }

# Prints what is needed to understand a failed release. Application logs hold no secrets, and
# "describe pod" only shows the names of the secrets in envFrom.
diagnose() {
  local release="$1" pod pods
  echo "---------- Diagnóstico de $release ----------" >&2
  kubectl get pods -n "$ENVIRONMENT" -l "app.kubernetes.io/instance=$release" -o wide >&2 || true
  pods="$(kubectl get pods -n "$ENVIRONMENT" -l "app.kubernetes.io/instance=$release" -o name 2>/dev/null | tr -d '\r')" || pods=""
  for pod in $pods; do
    echo "--- eventos de $pod" >&2
    kubectl describe "$pod" -n "$ENVIRONMENT" 2>/dev/null | sed -n '/^Events:/,$p' | tail -n 15 >&2 || true
    echo "--- logs de $pod" >&2
    kubectl logs "$pod" -n "$ENVIRONMENT" --all-containers --tail=60 >&2 2>&1 || true
    echo "--- logs del reinicio anterior de $pod" >&2
    kubectl logs "$pod" -n "$ENVIRONMENT" --all-containers --tail=60 --previous >&2 2>&1 || true
  done
  echo "---------- Fin del diagnóstico de $release ----------" >&2
}

image_pull_failed() {
  kubectl get pods -n "$ENVIRONMENT" -l "app.kubernetes.io/instance=$1" 2>/dev/null | grep -qE 'ImagePullBackOff|ErrImagePull'
}

# An interrupted run leaves the release "pending-*" and Helm then refuses any new operation.
clear_pending() {
  local release="$1" namespace="${2:-$ENVIRONMENT}" status
  status="$(helm status "$release" -n "$namespace" 2>/dev/null | tr -d '\r' | sed -n 's/^STATUS: //p')" || status=""
  case "$status" in
    pending-install) helm uninstall "$release" -n "$namespace" --wait >/dev/null || true ;;
    pending-upgrade|pending-rollback) helm rollback "$release" 0 -n "$namespace" --wait --timeout 3m >/dev/null || true ;;
  esac
}

echo "==> Namespace $ENVIRONMENT"
kubectl apply -f "$REPO_ROOT/infrastructure/k8s/namespaces.yaml"

if [[ -n "${LB_CONTROLLER_ROLE_ARN:-}" ]]; then
  echo "==> AWS Load Balancer Controller"
  helm repo add eks https://aws.github.io/eks-charts --force-update >/dev/null
  helm repo update eks >/dev/null
  clear_pending aws-load-balancer-controller kube-system
  helm upgrade --install aws-load-balancer-controller eks/aws-load-balancer-controller \
    --namespace kube-system \
    --version 1.17.1 \
    --set clusterName="${EKS_CLUSTER_NAME:?EKS_CLUSTER_NAME is required}" \
    --set region="${AWS_REGION:?AWS_REGION is required}" \
    --set vpcId="${VPC_ID:?VPC_ID is required}" \
    --set serviceAccount.name=aws-load-balancer-controller \
    --set-string "serviceAccount.annotations.eks\.amazonaws\.com/role-arn=$LB_CONTROLLER_ROLE_ARN" \
    --wait --timeout 5m
fi

# Private GHCR packages: create-k8s-secrets.sh leaves this secret when GHCR_TOKEN is set.
pull_secret=()
if kubectl get secret ghcr-pull -n "$ENVIRONMENT" >/dev/null 2>&1; then
  pull_secret=(--set "imagePullSecrets[0].name=ghcr-pull")
fi

failed=()
for service in $SERVICES; do
  echo "==> $service ($IMAGE_TAG)"
  extra=("${pull_secret[@]}")
  if [[ "$service" == "reporting-service" && -n "${REPORTING_SERVICE_ROLE_ARN:-}" ]]; then
    extra+=(--set-string "serviceAccount.roleArn=$REPORTING_SERVICE_ROLE_ARN")
  fi
  if [[ "$service" == "reporting-service" && -n "${REPORT_REQUESTS_QUEUE_URL:-}" ]]; then
    extra+=(--set-string "env.Reporting__SqsQueueUrl=$REPORT_REQUESTS_QUEUE_URL")
  fi
  if [[ "$service" == "reporting-service" && -n "${REPORTS_BUCKET:-}" ]]; then
    extra+=(--set-string "env.Reporting__S3Bucket=$REPORTS_BUCKET")
  fi
  if [[ "$KAFKA_CLIENTS" == *" $service "* && -n "${KAFKA_CLIENT_ROLE_ARN:-}" ]]; then
    extra+=(--set-string "serviceAccount.roleArn=$KAFKA_CLIENT_ROLE_ARN")
  fi
  if [[ -n "${KAFKA_BOOTSTRAP_SERVERS:-}" ]]; then
    extra+=(--set-string "env.Kafka__BootstrapServers=$(escape_commas "$KAFKA_BOOTSTRAP_SERVERS")")
  fi
  clear_pending "$service"
  # No --atomic: it would delete the failing pods before anyone can read their logs.
  if helm upgrade --install "$service" "$HELM_DIR/minesense-service" \
    --namespace "$ENVIRONMENT" \
    -f "$ENV_VALUES" \
    -f "$HELM_DIR/values/services/$service.yaml" \
    --set-string "image.tag=$IMAGE_TAG" \
    "${extra[@]}" \
    --wait --timeout "$SERVICE_TIMEOUT"; then
    continue
  fi

  failed+=("$service")
  echo "ERROR: $service no quedó listo en $SERVICE_TIMEOUT." >&2
  diagnose "$service"
  if image_pull_failed "$service"; then
    echo "ERROR: el clúster no puede descargar la imagen; los demás servicios fallarían igual." >&2
    echo "Revisa que exista el tag $IMAGE_TAG y exporta GHCR_TOKEN (docs/deploy/aws-staging.md)." >&2
    exit 1
  fi
  # Back to the last good revision when there was one; a first install stays for debugging.
  if (($(helm history "$service" -n "$ENVIRONMENT" 2>/dev/null | wc -l) > 2)); then
    helm rollback "$service" 0 -n "$ENVIRONMENT" --wait --timeout 3m || true
  fi
done

echo "==> Gateway (dashboard en / y API en /api/v1)"
clear_pending minesense-gateway
helm upgrade --install minesense-gateway "$HELM_DIR/minesense-gateway" \
  --namespace "$ENVIRONMENT" \
  --set-string "host=${API_HOST:-}" \
  --set-string "certificateArn=${ACM_CERTIFICATE_ARN:-}" \
  --wait --timeout 5m

echo "==> Prueba de humo"
address=""
for _ in $(seq 1 30); do
  address="$(kubectl get ingress minesense-api -n "$ENVIRONMENT" -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null | tr -d '\r' || true)"
  [[ -n "$address" ]] && break
  sleep 10
done
if [[ -z "$address" ]]; then
  echo "The load balancer has no address yet; check the AWS Load Balancer Controller:" >&2
  kubectl describe ingress minesense-api -n "$ENVIRONMENT" | tail -n 20 >&2 || true
  kubectl logs -n kube-system -l app.kubernetes.io/name=aws-load-balancer-controller --tail=40 >&2 || true
  exit 1
fi
scheme="http"; [[ -n "${ACM_CERTIFICATE_ARN:-}" ]] && scheme="https"
target="${API_HOST:-$address}"
base_url="$scheme://$target"

# The ALB needs a moment to resolve in DNS and register the targets; the JWKS of Identity & Access is public.
smoke_ok=false
for _ in $(seq 1 45); do
  if curl -fsS --max-time 10 -k "$base_url/.well-known/jwks.json" -o "$DEV_NULL" 2>/dev/null; then
    smoke_ok=true
    break
  fi
  sleep 10
done
if [[ "$smoke_ok" != true ]]; then
  echo "Smoke test failed: $base_url/.well-known/jwks.json did not answer 200." >&2
  kubectl describe ingress minesense-api -n "$ENVIRONMENT" | tail -n 20 >&2 || true
  exit 1
fi

# ---------- Edge simulator ----------
# Creates the user of the gateway once. An Operator can sign up without a token (src/edge/README.md).
ensure_edge_credentials() {
  kubectl get secret edge-agent-secrets -n "$ENVIRONMENT" >/dev/null 2>&1 && return 0
  local username password status attempt
  password="$(random_password)"
  for attempt in 1 2 3; do
    username="edge-cabin-01"
    # 409: the user exists but its Secret was lost (the password cannot be recovered).
    ((attempt > 1)) && username="edge-cabin-01-$(openssl rand -hex 3 | tr -d '\r')"
    status="$(printf '{"username":"%s","fullName":"Gateway simulador cabina 01","password":"%s","role":"Operator"}' "$username" "$password" |
      curl -sS -k --max-time 20 -o "$DEV_NULL" -w '%{http_code}' -X POST "$base_url/api/v1/authentication/sign-up" \
        -H 'Content-Type: application/json' --data-binary @-)" || status="000"
    case "$status" in
      201) break ;;
      409) continue ;;
      *) echo "No se pudo registrar el usuario Edge (HTTP $status)." >&2; return 1 ;;
    esac
  done
  [[ "$status" == 201 ]] || return 1
  {
    printf 'apiVersion: v1\nkind: Secret\nmetadata:\n  name: edge-agent-secrets\n  namespace: %s\ntype: Opaque\ndata:\n' "$ENVIRONMENT"
    printf '  EDGE_USERNAME: %s\n' "$(printf '%s' "$username" | b64)"
    printf '  EDGE_PASSWORD: %s\n' "$(printf '%s' "$password" | b64)"
  } | kubectl apply -f - >/dev/null
  echo "Usuario Edge $username registrado y guardado en el Secret edge-agent-secrets."
}

if [[ "$EDGE_SIMULATOR" == true ]]; then
  echo "==> Simulador Edge ($IMAGE_TAG)"
  if ensure_edge_credentials; then
    clear_pending edge-agent
    if ! helm upgrade --install edge-agent "$HELM_DIR/minesense-edge-simulator" \
      --namespace "$ENVIRONMENT" \
      --set-string "image.tag=$IMAGE_TAG" \
      "${pull_secret[@]}" \
      --wait --timeout "$SERVICE_TIMEOUT"; then
      failed+=("edge-agent")
      diagnose edge-agent
    fi
  else
    failed+=("edge-agent")
  fi
fi

if ((${#failed[@]} > 0)); then
  echo "Despliegue incompleto de $IMAGE_TAG en $ENVIRONMENT. Fallaron: ${failed[*]}" >&2
  exit 1
fi
echo "Deployed $IMAGE_TAG to $ENVIRONMENT: $base_url"
