#!/usr/bin/env bash
# Deploys every MineSense Safety microservice and the API gateway to one EKS environment.
# The same script runs in the CD pipeline and by hand (section 3.4).
#
#   infrastructure/scripts/deploy.sh <staging|production> <image-tag>
#
# Requires kubectl already pointing at the cluster (aws eks update-kubeconfig) and Helm 3.
# Optional environment variables:
#   SERVICES                    space-separated subset of services (default: all in values/services)
#   LB_CONTROLLER_ROLE_ARN      IAM role of the AWS Load Balancer Controller (installs/updates it)
#   EKS_CLUSTER_NAME, AWS_REGION, VPC_ID   needed together with LB_CONTROLLER_ROLE_ARN
#   REPORTING_SERVICE_ROLE_ARN  IRSA role of the Reporting Service (Terraform output)
#   KAFKA_BOOTSTRAP_SERVERS     MSK Serverless IAM brokers (Terraform output kafka_bootstrap_brokers)
#   API_HOST, ACM_CERTIFICATE_ARN  public host and HTTPS certificate of the gateway
set -euo pipefail

ENVIRONMENT="${1:?usage: deploy.sh <staging|production> <image-tag>}"
IMAGE_TAG="${2:?usage: deploy.sh <staging|production> <image-tag>}"
case "$ENVIRONMENT" in staging|production) ;; *) echo "Unknown environment: $ENVIRONMENT" >&2; exit 1 ;; esac

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
HELM_DIR="$ROOT/infrastructure/helm"
ENV_VALUES="$HELM_DIR/values/environments/$ENVIRONMENT.yaml"
SERVICES="${SERVICES:-$(ls "$HELM_DIR/values/services" | sed 's/\.yaml$//')}"

echo "==> Namespace $ENVIRONMENT"
kubectl apply -f "$ROOT/infrastructure/k8s/namespaces.yaml"

if [[ -n "${LB_CONTROLLER_ROLE_ARN:-}" ]]; then
  echo "==> AWS Load Balancer Controller"
  helm repo add eks https://aws.github.io/eks-charts >/dev/null
  helm repo update eks >/dev/null
  helm upgrade --install aws-load-balancer-controller eks/aws-load-balancer-controller \
    --namespace kube-system \
    --version 1.10.1 \
    --set clusterName="${EKS_CLUSTER_NAME:?EKS_CLUSTER_NAME is required}" \
    --set region="${AWS_REGION:?AWS_REGION is required}" \
    --set vpcId="${VPC_ID:?VPC_ID is required}" \
    --set serviceAccount.name=aws-load-balancer-controller \
    --set-string "serviceAccount.annotations.eks\.amazonaws\.com/role-arn=$LB_CONTROLLER_ROLE_ARN" \
    --wait --timeout 5m
fi

for service in $SERVICES; do
  echo "==> $service ($IMAGE_TAG)"
  extra=()
  if [[ "$service" == "reporting-service" && -n "${REPORTING_SERVICE_ROLE_ARN:-}" ]]; then
    extra+=(--set-string "serviceAccount.roleArn=$REPORTING_SERVICE_ROLE_ARN")
  fi
  if [[ -n "${KAFKA_BOOTSTRAP_SERVERS:-}" ]]; then
    extra+=(--set-string "env.Kafka__BootstrapServers=$KAFKA_BOOTSTRAP_SERVERS")
  fi
  # --atomic rolls the release back automatically if the new pods never become ready.
  helm upgrade --install "$service" "$HELM_DIR/minesense-service" \
    --namespace "$ENVIRONMENT" \
    -f "$ENV_VALUES" \
    -f "$HELM_DIR/values/services/$service.yaml" \
    --set-string "image.tag=$IMAGE_TAG" \
    "${extra[@]}" \
    --atomic --wait --timeout 5m
done

echo "==> API gateway"
helm upgrade --install minesense-gateway "$HELM_DIR/minesense-gateway" \
  --namespace "$ENVIRONMENT" \
  --set-string "host=${API_HOST:-}" \
  --set-string "certificateArn=${ACM_CERTIFICATE_ARN:-}" \
  --wait --timeout 5m

echo "==> Smoke test"
address=""
for _ in $(seq 1 30); do
  address="$(kubectl get ingress minesense-api -n "$ENVIRONMENT" -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || true)"
  [[ -n "$address" ]] && break
  sleep 10
done
if [[ -z "$address" ]]; then
  echo "The load balancer has no address yet; check the AWS Load Balancer Controller." >&2
  exit 1
fi
scheme="http"; [[ -n "${ACM_CERTIFICATE_ARN:-}" ]] && scheme="https"
target="${API_HOST:-$address}"
# The ALB needs a moment to register the targets; the JWKS of Identity & Access is public.
for _ in $(seq 1 30); do
  if curl -fsS --max-time 10 -k "$scheme://$target/.well-known/jwks.json" -o /dev/null; then
    echo "Deployed $IMAGE_TAG to $ENVIRONMENT: $scheme://$target"
    exit 0
  fi
  sleep 10
done
echo "Smoke test failed: $scheme://$target/.well-known/jwks.json did not answer 200." >&2
exit 1
