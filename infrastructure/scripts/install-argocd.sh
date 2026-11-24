#!/usr/bin/env bash
# Argo CD on one EKS cluster (section 4.2): installs the argo-cd chart in the "argocd" namespace,
# then the MineSense project, the ApplicationSet of the environment and the gateway/Edge apps.
# Run it once after deploy-all.sh (or instead of deploy.sh's Helm loop when GITOPS_ENABLED=true).
#
#   infrastructure/scripts/install-argocd.sh staging
# UI: kubectl -n argocd port-forward svc/argocd-server 8443:443   (user admin, password below)
set -euo pipefail
source "$(dirname "$0")/lib/common.sh"

ENVIRONMENT="${1:?usage: install-argocd.sh <staging|production>}"
require_environment "$ENVIRONMENT"
require_tools helm kubectl
GITOPS="$REPO_ROOT/infrastructure/gitops/argocd"
CHART_VERSION="${ARGOCD_CHART_VERSION:-8.3.0}"

log "Argo CD $CHART_VERSION"
helm repo add argo https://argoproj.github.io/argo-helm --force-update >/dev/null
helm upgrade --install argocd argo/argo-cd --version "$CHART_VERSION" \
  --namespace argocd --create-namespace \
  --set configs.params."server\.insecure"=false \
  --set global.nodeSelector.workload=microservices \
  --wait --timeout 10m

log "Proyecto y aplicaciones de $ENVIRONMENT"
kubectl apply -f "$GITOPS/project.yaml"
kubectl apply -f "$GITOPS/applicationset-$ENVIRONMENT.yaml"
kubectl apply -f "$GITOPS/platform-$ENVIRONMENT.yaml"

kubectl get applications -n argocd
echo "Contraseña inicial: kubectl -n argocd get secret argocd-initial-admin-secret -o jsonpath='{.data.password}' | base64 -d"
