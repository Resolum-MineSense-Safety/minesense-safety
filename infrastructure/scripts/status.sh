#!/usr/bin/env bash
# Prints the evidence of a deployed environment: pods, services, ingress, the readiness of every
# service (through port-forward, because /health/* is not published on the ALB), the JWKS, the
# dashboard and the activity of the Edge simulator. Read-only; prints no secrets.
#
#   infrastructure/scripts/status.sh <staging|production>
set -uo pipefail
source "$(dirname "$0")/lib/common.sh"

ENVIRONMENT="${1:?usage: status.sh <staging|production>}"
require_environment "$ENVIRONMENT"
require_tools kubectl curl

log "Nodos"
kubectl get nodes -o wide

log "Pods, servicios e ingress en $ENVIRONMENT"
kubectl get pods,svc,ingress -n "$ENVIRONMENT" -o wide

log "Readiness de cada servicio (/health/ready por port-forward)"
port=18100
problems=0
for service in $(kubectl get deployments -n "$ENVIRONMENT" -l app.kubernetes.io/part-of=minesense-safety -o name 2>/dev/null | tr -d '\r' | sed 's|.*/||'); do
  [[ "$service" == edge-agent ]] && continue
  path=/health/ready
  [[ "$service" == supervision-dashboard ]] && path=/healthz
  port=$((port + 1))
  kubectl port-forward -n "$ENVIRONMENT" "svc/$service" "$port:80" >/dev/null 2>&1 &
  forward=$!
  code="000"
  for _ in 1 2 3 4 5 6 7 8; do
    sleep 1
    code="$(curl -s --max-time 5 -o "$DEV_NULL" -w '%{http_code}' "http://127.0.0.1:$port$path")" && [[ "$code" != 000 ]] && break
  done
  kill "$forward" 2>/dev/null
  wait "$forward" 2>/dev/null
  printf '  %-34s %-14s HTTP %s\n' "$service" "$path" "$code"
  [[ "$code" == 200 ]] || problems=$((problems + 1))
done

address="$(kubectl get ingress minesense-api -n "$ENVIRONMENT" -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null | tr -d '\r')"
if [[ -n "$address" ]]; then
  host="$(kubectl get ingress minesense-api -n "$ENVIRONMENT" -o jsonpath='{.spec.rules[0].host}' 2>/dev/null | tr -d '\r')"
  scheme=http
  kubectl get ingress minesense-api -n "$ENVIRONMENT" -o jsonpath='{.metadata.annotations}' 2>/dev/null | grep -q 'certificate-arn' && scheme=https
  base="$scheme://${host:-$address}"
  log "Entrada pública: $base"
  printf 'Dashboard  GET /                        HTTP %s\n' "$(curl -sk --max-time 15 -o "$DEV_NULL" -w '%{http_code}' "$base/")"
  printf 'API        GET /api/v1/mining-units     HTTP %s (401 = la API responde y exige token)\n' \
    "$(curl -sk --max-time 15 -o "$DEV_NULL" -w '%{http_code}' "$base/api/v1/mining-units")"
  echo "JWKS       GET /.well-known/jwks.json:"
  curl -sk --max-time 15 "$base/.well-known/jwks.json" | head -c 400
  echo
else
  warn "El ingress todavía no tiene dirección."
  problems=$((problems + 1))
fi

if kubectl get deployment edge-agent -n "$ENVIRONMENT" >/dev/null 2>&1; then
  log "Simulador Edge (últimas líneas)"
  kubectl logs deployment/edge-agent -n "$ENVIRONMENT" --tail=15 2>&1
fi

log "Consumidores de Kafka (grupo suscrito o errores)"
for service in alert-service fleet-monitoring-service; do
  kubectl logs "deployment/$service" -n "$ENVIRONMENT" --tail=500 2>/dev/null | tr -d '\r' |
    grep -E 'Consumer group|Kafka (consumer|producer) error|not created' | tail -n 3 | sed "s/^/  [$service] /"
done

((problems == 0)) && echo && echo "Todo responde."
exit 0
