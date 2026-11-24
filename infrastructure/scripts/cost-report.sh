#!/usr/bin/env bash
# Real cost incurred by the account in a period, by AWS service, as a Markdown table (AV3 feedback:
# "presenta el costo real de Staging"). Cost Explorer data lags up to 24 h, so run it the day after a
# deployment. It reads with ce:GetCostAndUsage only; Cost Explorer charges 0.01 USD per request.
#
#   infrastructure/scripts/cost-report.sh 2026-11-01 2026-11-03          # whole account
#   COST_TAG_PROJECT="MineSense Safety" infrastructure/scripts/cost-report.sh 2026-11-01 2026-11-03
#
# COST_TAG_PROJECT filters by the Project tag that every Terraform resource carries; it only works
# after activating "Project" as a cost allocation tag in Billing (it is not retroactive).
set -euo pipefail
source "$(dirname "$0")/lib/common.sh"

START="${1:?usage: cost-report.sh <start YYYY-MM-DD> <end YYYY-MM-DD, exclusive>}"
END="${2:?usage: cost-report.sh <start YYYY-MM-DD> <end YYYY-MM-DD, exclusive>}"
require_tools aws
check_identity >&2

filter=()
if [[ -n "${COST_TAG_PROJECT:-}" ]]; then
  filter=(--filter "{\"Tags\":{\"Key\":\"Project\",\"Values\":[\"$COST_TAG_PROJECT\"]}}")
fi

# Cost Explorer only answers in us-east-1.
rows="$(aws ce get-cost-and-usage --region us-east-1 \
  --time-period "Start=$START,End=$END" --granularity MONTHLY --metrics UnblendedCost \
  --group-by Type=DIMENSION,Key=SERVICE "${filter[@]}" \
  --query 'ResultsByTime[].Groups[].[Keys[0], Metrics.UnblendedCost.Amount]' --output text | tr -d '\r')"

printf '### Costo real AWS del %s al %s%s\n\n' "$START" "$END" "${COST_TAG_PROJECT:+ (tag Project=$COST_TAG_PROJECT)}"
printf '| Servicio | Costo (USD) |\n|---|---:|\n'
# Services of several months come in several rows: add them up, then sort by cost.
printf '%s\n' "$rows" | awk -F '\t' 'NF == 2 { sum[$1] += $2 } END { for (s in sum) if (sum[s] >= 0.005) printf "%s\t%.2f\n", s, sum[s] }' |
  sort -t $'\t' -k2 -gr | awk -F '\t' '{ printf "| %s | %s |\n", $1, $2; total += $2 } END { printf "| **Total** | **%.2f** |\n", total }'
