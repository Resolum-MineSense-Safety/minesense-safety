#!/usr/bin/env bash
# Monitoring stack of one EKS cluster (section 4.3): kube-prometheus-stack (Prometheus,
# Alertmanager, Grafana, kube-state-metrics, node-exporter) in the "monitoring" namespace,
# the SLO rules of infrastructure/observability/rules as a PrometheusRule and the MineSense
# dashboard as a Grafana ConfigMap. Run it before deploy.sh so the ServiceMonitor CRD exists.
#
#   infrastructure/scripts/install-observability.sh staging
#
# Optional: ALERT_WEBHOOK_URL / ONCALL_WEBHOOK_URL (Slack/Teams/Discord-compatible webhooks).
# Grafana: kubectl -n monitoring port-forward svc/kube-prometheus-stack-grafana 3000:80
set -euo pipefail
source "$(dirname "$0")/lib/common.sh"

ENVIRONMENT="${1:?usage: install-observability.sh <staging|production>}"
require_environment "$ENVIRONMENT"
require_tools helm kubectl openssl
OBS_DIR="$REPO_ROOT/infrastructure/observability"
CHART_VERSION="${KUBE_PROMETHEUS_STACK_VERSION:-79.0.0}"

log "Namespace monitoring y secretos"
kubectl create namespace monitoring --dry-run=client -o yaml | kubectl apply -f - >/dev/null
if ! kubectl get secret grafana-admin -n monitoring >/dev/null 2>&1; then
  kubectl create secret generic grafana-admin -n monitoring \
    --from-literal=admin-user=admin \
    --from-literal=admin-password="$(openssl rand -base64 24 | tr -d '\r')" >/dev/null
  echo "Contraseña de Grafana en el Secret monitoring/grafana-admin (no se imprime)."
fi
# Without a real webhook the notifications go nowhere, but the alerts still show in Alertmanager.
kubectl create secret generic alertmanager-webhook -n monitoring \
  --from-literal=team-url="${ALERT_WEBHOOK_URL:-http://127.0.0.1:9/disabled}" \
  --from-literal=oncall-url="${ONCALL_WEBHOOK_URL:-${ALERT_WEBHOOK_URL:-http://127.0.0.1:9/disabled}}" \
  --dry-run=client -o yaml | kubectl apply -f - >/dev/null

log "kube-prometheus-stack $CHART_VERSION"
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts --force-update >/dev/null
helm upgrade --install kube-prometheus-stack prometheus-community/kube-prometheus-stack \
  --version "$CHART_VERSION" --namespace monitoring \
  -f "$OBS_DIR/kube-prometheus-stack.values.yaml" \
  --wait --timeout 10m

log "Reglas de SLO (PrometheusRule) y dashboard de Grafana"
{
  printf 'apiVersion: monitoring.coreos.com/v1\nkind: PrometheusRule\nmetadata:\n'
  printf '  name: minesense-slo\n  namespace: monitoring\n  labels:\n    release: kube-prometheus-stack\nspec:\n'
  sed -e '/^#/d' -e 's/^/  /' "$OBS_DIR/rules/minesense-slo.rules.yml"
} | kubectl apply -f - >/dev/null
kubectl create configmap minesense-dashboard -n monitoring \
  --from-file=minesense-slo.json="$OBS_DIR/dashboards/minesense-slo.json" \
  --dry-run=client -o yaml | kubectl label --local -f - grafana_dashboard=1 -o yaml | kubectl apply -f - >/dev/null

kubectl get pods -n monitoring
echo "Grafana: kubectl -n monitoring port-forward svc/kube-prometheus-stack-grafana 3000:80"
