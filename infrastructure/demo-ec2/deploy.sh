#!/usr/bin/env bash
# Demo environment of MineSense Safety on one EC2 instance (README.md).
#
#   ./deploy.sh up [git-ref]   # creates the instance (default ref: develop) and waits until it answers
#   ./deploy.sh status         # URL and state
#   ./deploy.sh logs           # boot log through the EC2 console output
#   ./deploy.sh down           # terminates the instance and releases its IP and security group
#
# Needs the AWS CLI signed in (aws login / aws sso login). AWS_PROFILE and AWS_REGION are honored.
set -euo pipefail
cd "$(dirname "$0")"

NAME=minesense-demo
TYPE="${INSTANCE_TYPE:-t3.xlarge}"
REGION="${AWS_REGION:-$(command aws configure get region 2>/dev/null || echo us-east-1)}"
REPO_URL="${REPO_URL:-https://github.com/Resolum-MineSense-Safety/minesense-safety.git}"
SECRETS=.demo-credentials   # generated passwords of the demo; never committed (.gitignore)
aws() { command aws --region "$REGION" "$@"; }

instance_id() {
  aws ec2 describe-instances --filters "Name=tag:Name,Values=$NAME" "Name=instance-state-name,Values=pending,running,stopping,stopped" \
    --query 'Reservations[].Instances[].InstanceId' --output text
}
public_ip() { aws ec2 describe-addresses --filters "Name=tag:Name,Values=$NAME" --query 'Addresses[0].PublicIp' --output text; }
random() { LC_ALL=C tr -dc 'A-Za-z0-9' < /dev/urandom | head -c "$1"; }

up() {
  local ref="${1:-develop}"
  if [[ -n "$(instance_id)" ]]; then echo "Ya existe una instancia $NAME."; status; return; fi

  local vpc subnet sg ami
  vpc="$(aws ec2 describe-vpcs --filters Name=isDefault,Values=true --query 'Vpcs[0].VpcId' --output text)"
  subnet="$(aws ec2 describe-subnets --filters "Name=vpc-id,Values=$vpc" Name=default-for-az,Values=true --query 'Subnets[0].SubnetId' --output text)"
  sg="$(aws ec2 describe-security-groups --filters "Name=group-name,Values=$NAME" "Name=vpc-id,Values=$vpc" --query 'SecurityGroups[0].GroupId' --output text)"
  if [[ "$sg" == "None" ]]; then
    sg="$(aws ec2 create-security-group --group-name "$NAME" --description "MineSense demo: HTTP only" --vpc-id "$vpc" --query GroupId --output text)"
    aws ec2 authorize-security-group-ingress --group-id "$sg" --protocol tcp --port 80 --cidr 0.0.0.0/0 >/dev/null
    aws ec2 create-tags --resources "$sg" --tags "Key=Name,Value=$NAME" "Key=Project,Value=minesense-safety"
  fi
  ami="$(aws ssm get-parameter --name /aws/service/ami-amazon-linux-latest/al2023-ami-kernel-default-x86_64 --query Parameter.Value --output text)"

  if [[ ! -f "$SECRETS" ]]; then
    printf 'ADMIN_PASSWORD=%s\nDEMO_PASSWORD=%s\nPOSTGRES_PASSWORD=%s\n' "Ms-$(random 14)" "Demo-$(random 12)" "$(random 24)" > "$SECRETS"
  fi
  # shellcheck disable=SC1090
  source "$SECRETS"
  local data
  data="$(sed -e "s|__REPO_URL__|$REPO_URL|" -e "s|__REF__|$ref|" -e "s|__ADMIN_PASSWORD__|$ADMIN_PASSWORD|" \
              -e "s|__DEMO_PASSWORD__|$DEMO_PASSWORD|" -e "s|__POSTGRES_PASSWORD__|$POSTGRES_PASSWORD|" user-data.sh)"

  local id
  id="$(aws ec2 run-instances --image-id "$ami" --instance-type "$TYPE" --subnet-id "$subnet" --security-group-ids "$sg" \
        --associate-public-ip-address --user-data "$data" \
        --block-device-mappings 'DeviceName=/dev/xvda,Ebs={VolumeSize=40,VolumeType=gp3,DeleteOnTermination=true}' \
        --metadata-options 'HttpTokens=required,HttpEndpoint=enabled' \
        --tag-specifications "ResourceType=instance,Tags=[{Key=Name,Value=$NAME},{Key=Project,Value=minesense-safety}]" \
        --query 'Instances[0].InstanceId' --output text)"
  echo "Instancia $id ($TYPE) creada desde $ref; esperando que arranque..."
  aws ec2 wait instance-running --instance-ids "$id"

  local alloc
  alloc="$(aws ec2 allocate-address --domain vpc --tag-specifications "ResourceType=elastic-ip,Tags=[{Key=Name,Value=$NAME}]" --query AllocationId --output text)"
  aws ec2 associate-address --instance-id "$id" --allocation-id "$alloc" >/dev/null
  local ip; ip="$(public_ip)"
  echo "IP elástica: $ip. Construyendo imágenes y levantando el entorno (15 a 25 min)..."
  for _ in $(seq 1 200); do
    if curl -fsS -m 5 "http://$ip/healthz" >/dev/null 2>&1; then
      echo; echo "Listo: http://$ip"; echo "Credenciales: infrastructure/demo-ec2/$SECRETS"; return
    fi
    printf '.'; sleep 10
  done
  echo; echo "Todavía no responde; revisa ./deploy.sh logs"
}

status() {
  local id; id="$(instance_id)"
  if [[ -z "$id" ]]; then echo "No hay instancia $NAME."; return; fi
  aws ec2 describe-instances --instance-ids "$id" --query 'Reservations[0].Instances[0].[InstanceId,InstanceType,State.Name,LaunchTime]' --output text
  local ip; ip="$(public_ip)"
  echo "URL: http://$ip"
  if curl -fsS -m 5 "http://$ip/healthz" >/dev/null 2>&1; then echo "El gateway responde."; else echo "El gateway todavía no responde."; fi
}

logs() { aws ec2 get-console-output --instance-id "$(instance_id)" --latest --output text | grep -a 'minesense-demo' | tail -n "${LINES:-60}"; }

down() {
  local id; id="$(instance_id)"
  if [[ -n "$id" ]]; then
    aws ec2 terminate-instances --instance-ids "$id" >/dev/null
    echo "Terminando $id..."
    aws ec2 wait instance-terminated --instance-ids "$id"
  fi
  local alloc; alloc="$(aws ec2 describe-addresses --filters "Name=tag:Name,Values=$NAME" --query 'Addresses[0].AllocationId' --output text)"
  if [[ -n "$alloc" && "$alloc" != "None" ]]; then aws ec2 release-address --allocation-id "$alloc"; echo "IP liberada."; fi
  local sg; sg="$(aws ec2 describe-security-groups --filters "Name=group-name,Values=$NAME" --query 'SecurityGroups[0].GroupId' --output text)"
  if [[ -n "$sg" && "$sg" != "None" ]]; then aws ec2 delete-security-group --group-id "$sg"; echo "Security group borrado."; fi
}

case "${1:-}" in
  up) up "${2:-}" ;;
  status) status ;;
  logs) logs ;;
  down) down ;;
  *) sed -n '2,9p' "$0"; exit 1 ;;
esac
