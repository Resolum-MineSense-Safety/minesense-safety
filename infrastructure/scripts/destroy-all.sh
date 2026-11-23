#!/usr/bin/env bash
# Tears one environment down so it stops costing money. The order matters: the ALB and its security
# groups are created by the Load Balancer Controller, outside Terraform, and block the deletion of
# the VPC, so the Ingress goes first and the controller last.
#
#   infrastructure/scripts/destroy-all.sh staging [--yes]
#
# The state backend (bucket, lock table) and the GitHub OIDC provider stay: they cost cents and any
# other environment may use them. The end of the script explains how to remove them.
set -euo pipefail
source "$(dirname "$0")/lib/common.sh"

ENVIRONMENT="${1:?usage: destroy-all.sh <staging> [--yes]}"
require_environment "$ENVIRONMENT"
[[ "$ENVIRONMENT" == staging || "${ALLOW_PRODUCTION_DESTROY:-}" == yes ]] ||
  die "Production no se destruye con este script (tiene deletion protection)."
require_tools aws terraform helm kubectl
check_identity
if [[ "${2:-}" != "--yes" ]]; then
  printf 'Esto borra TODO %s, incluidas sus bases de datos. Escribe "%s" para continuar: ' "$ENVIRONMENT" "$ENVIRONMENT"
  read -r answer
  [[ "$answer" == "$ENVIRONMENT" ]] || die "Cancelado."
fi

STACK="$(stack_dir "$ENVIRONMENT")"
BUCKET="$(state_bucket_name)"
CLUSTER_NAME="minesense-$ENVIRONMENT"
ACCOUNT_ID="$(aws_account_id)"

log "Backend de Terraform"
aws s3api head-bucket --bucket "$BUCKET" >/dev/null 2>&1 || die "No existe el bucket de estado $BUCKET: no hay nada que destruir con Terraform."
terraform_init "$STACK" "$BUCKET"

# ALBs of this cluster (the controller tags them with elbv2.k8s.aws/cluster).
cluster_load_balancers() {
  aws resourcegroupstaggingapi get-resources --resource-type-filters elasticloadbalancing:loadbalancer \
    --tag-filters "Key=elbv2.k8s.aws/cluster,Values=$CLUSTER_NAME" \
    --query 'ResourceTagMappingList[].ResourceARN' --output text 2>/dev/null | tr -d '\r' | sed 's/^None$//'
}

if aws eks describe-cluster --name "$CLUSTER_NAME" >/dev/null 2>&1; then
  log "Quitar el ALB (Ingress) y las aplicaciones de Helm"
  aws eks update-kubeconfig --name "$CLUSTER_NAME" --region "$AWS_REGION" >/dev/null
  helm uninstall edge-agent -n "$ENVIRONMENT" 2>/dev/null || true
  helm uninstall minesense-gateway -n "$ENVIRONMENT" --wait --timeout 5m 2>/dev/null || true
  # The controller must stay alive until it has deleted the ALB (finalizer of the Ingress).
  for _ in $(seq 1 30); do
    [[ -z "$(kubectl get ingress -n "$ENVIRONMENT" -o name 2>/dev/null | tr -d '\r')" && -z "$(cluster_load_balancers)" ]] && break
    echo "Esperando a que se borre el ALB..."
    sleep 10
  done
  for release in $(helm list -n "$ENVIRONMENT" -q 2>/dev/null | tr -d '\r'); do
    helm uninstall "$release" -n "$ENVIRONMENT" --wait --timeout 3m >/dev/null 2>&1 || true
  done
  helm uninstall aws-load-balancer-controller -n kube-system --wait --timeout 3m 2>/dev/null || true
else
  echo "El clúster $CLUSTER_NAME ya no existe."
fi

# Leftovers of a controller that died before finishing: the VPC cannot be deleted while they exist.
for arn in $(cluster_load_balancers); do
  warn "Borrando ALB huérfano $arn"
  aws elbv2 delete-load-balancer --load-balancer-arn "$arn" || true
done
VPC_ID="$(tf_output "$STACK" vpc_id)"
if [[ -n "$VPC_ID" ]]; then
  orphan_groups="$(aws ec2 describe-security-groups \
    --filters "Name=vpc-id,Values=$VPC_ID" "Name=tag:elbv2.k8s.aws/cluster,Values=$CLUSTER_NAME" \
    --query 'SecurityGroups[].GroupId' --output text 2>/dev/null | tr -d '\r' | sed 's/^None$//')"
  if [[ -n "$orphan_groups" ]]; then
    # The ALB takes a moment to release them, and the groups of the controller may reference each other.
    sleep 60
    for group in $orphan_groups; do
      for rule in $(aws ec2 describe-security-group-rules --filters "Name=group-id,Values=$group" \
        --query 'SecurityGroupRules[?!IsEgress].SecurityGroupRuleId' --output text 2>/dev/null | tr -d '\r' | sed 's/^None$//'); do
        aws ec2 revoke-security-group-ingress --group-id "$group" --security-group-rule-ids "$rule" >/dev/null 2>&1 || true
      done
    done
    for group in $orphan_groups; do
      warn "Borrando security group huérfano $group"
      aws ec2 delete-security-group --group-id "$group" || true
    done
  fi
fi

log "Terraform destroy (20-30 minutos; la thing type de IoT sola espera 5)"
terraform -chdir="$STACK" destroy -input=false -auto-approve -var "aws_region=$AWS_REGION"

log "Comprobación de lo que queda"
remaining="$(aws resourcegroupstaggingapi get-resources --tag-filters "Key=Environment,Values=$ENVIRONMENT" "Key=Project,Values=MineSense Safety" \
  --query 'ResourceTagMappingList[].ResourceARN' --output text 2>/dev/null | tr -d '\r' | sed 's/^None$//')"
if [[ -n "$remaining" ]]; then
  # The tagging API keeps recently deleted resources for a while: check them again in an hour.
  echo "Recursos aún etiquetados con $ENVIRONMENT (la API tarda en actualizarse; revisa de nuevo en una hora):"
  printf '%s\n' "$remaining" | tr '\t' '\n' | sed 's/^/  /'
else
  echo "Ningún recurso etiquetado con $ENVIRONMENT."
fi

cat <<EOF

$ENVIRONMENT ya no genera costos por hora. Quedan a propósito (centavos al mes):
  - Bucket de estado s3://$BUCKET y tabla DynamoDB $TF_LOCK_TABLE
  - Proveedor OIDC de GitHub en IAM (gratis)
  - Grupos de CloudWatch Logs que las aplicaciones recrean, si aparecen: /aws/eks/$CLUSTER_NAME/cluster

Para borrar también el backend, si ningún otro entorno lo usa y existe el estado local de bootstrap:
  cd infrastructure/terraform/bootstrap
  terraform apply  -var state_bucket_name=$BUCKET -var force_destroy_state=true
  terraform destroy -var state_bucket_name=$BUCKET -var force_destroy_state=true
Sin ese estado local (otra máquina), desde la consola de AWS: vaciar y borrar el bucket, borrar la tabla
$TF_LOCK_TABLE y el proveedor arn:aws:iam::$ACCOUNT_ID:oidc-provider/token.actions.githubusercontent.com
EOF
