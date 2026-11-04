#!/usr/bin/env bash
# Runs the 13 microservices with `dotnet run` (in-memory persistence, no Kafka) on the ports the
# Vite dev proxy expects, for the E2E, accessibility and performance tests without Docker.
# The CI uses docker compose instead (workflow e2e-tests.yml).
#
#   bash tests/e2e/scripts/start-native.sh        # builds once, starts everything, waits for /health/ready
#   bash tests/e2e/scripts/start-native.sh stop
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
BACKEND="$ROOT/src/backend"
LOGS="$ROOT/tests/e2e/.native-logs"
declare -A PORTS=(
  [IdentityAccessService]=5150 [OperationsService]=5103 [DeviceManagementService]=5031
  [MonitoringService]=5101 [FatigueDetectionService]=5158 [AlertService]=5233
  [FleetMonitoringService]=5255 [IncidentManagementService]=5070 [PrivacyAuditService]=5260
  [ReportingService]=5270 [AnalyticsService]=5090 [EdgeSynchronizationService]=5280
  [FeedbackReliabilityService]=5281
)

if [[ "${1:-}" == stop ]]; then
  for pid in "$LOGS"/*.pid; do [[ -f "$pid" ]] && kill "$(cat "$pid")" 2>/dev/null || true; done
  exit 0
fi

mkdir -p "$LOGS"
dotnet build "$BACKEND/MineSenseSafety.sln" -c Release -nologo -v q
for service in "${!PORTS[@]}"; do
  port="${PORTS[$service]}"
  # From the output folder, so the host finds appsettings.Development.json.
  (cd "$BACKEND/$service/bin/Release/net8.0" &&
    ASPNETCORE_ENVIRONMENT=Development \
    Authentication__IdentityUrl="http://localhost:5150/" \
    Kafka__BootstrapServers="" \
    exec dotnet "$service.dll" --urls "http://localhost:$port") >"$LOGS/$service.log" 2>&1 &
  echo $! >"$LOGS/$service.pid"
done

for service in "${!PORTS[@]}"; do
  port="${PORTS[$service]}"
  for _ in $(seq 1 60); do
    curl -fs "http://localhost:$port/health/live" >/dev/null 2>&1 && break
    sleep 1
  done
  printf '%-30s %s\n' "$service" "$(curl -s -o /dev/null -w '%{http_code}' "http://localhost:$port/health/ready")"
done
