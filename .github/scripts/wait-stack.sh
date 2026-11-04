#!/usr/bin/env bash
# Waits until every microservice of docker-compose.yml answers /health/ready (databases created,
# Kafka reachable) and the gateway serves the dashboard. Used by the E2E, k6 and ZAP workflows.
set -euo pipefail
PORTS="5202 5203 5204 5201 5205 5206 5207 5208 5280 5260 5270 5209 5281"
for port in $PORTS; do
  for attempt in $(seq 1 90); do
    if curl -fs -o /dev/null "http://localhost:$port/health/ready"; then break; fi
    [[ $attempt == 90 ]] && { echo "Port $port never became ready" >&2; exit 1; }
    sleep 2
  done
done
curl -fs -o /dev/null http://localhost:8080/ && echo "Stack ready."
