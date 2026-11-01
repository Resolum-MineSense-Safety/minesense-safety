#!/usr/bin/env bash
# Shared helpers of the deployment scripts. They run on Linux and on Git Bash for Windows with the
# native aws, terraform, helm, kubectl and dotnet on PATH, which needs three precautions:
#  - MSYS must not rewrite arguments such as /health/ready into Windows paths;
#  - paths handed to native tools must be E:/... instead of /e/...;
#  - native tools end their lines with CRLF, so every captured value goes through "tr -d '\r'".
# Nothing here prints a secret; the scripts never enable "set -x".

export MSYS_NO_PATHCONV=1
export MSYS2_ARG_CONV_EXCL='*'

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && (pwd -W 2>/dev/null || pwd))"
TF_DIR="$REPO_ROOT/infrastructure/terraform"
AWS_REGION="${AWS_REGION:-us-east-1}"
AWS_DEFAULT_REGION="$AWS_REGION"
export AWS_REGION AWS_DEFAULT_REGION
# The AWS CLI must not open a pager in the middle of a script.
export AWS_PAGER=""
export TF_IN_AUTOMATION=1
TF_LOCK_TABLE="${TF_LOCK_TABLE:-minesense-safety-terraform-locks}"

log() { printf '\n==> %s\n' "$*"; }
warn() { printf 'AVISO: %s\n' "$*" >&2; }
die() { printf 'ERROR: %s\n' "$*" >&2; exit 1; }

require_environment() {
  case "${1:-}" in
    staging|production) ;;
    *) die "Entorno desconocido: '${1:-}' (staging o production)" ;;
  esac
}

require_tools() {
  local tool missing=()
  for tool in "$@"; do
    command -v "$tool" >/dev/null 2>&1 || missing+=("$tool")
  done
  ((${#missing[@]} == 0)) || die "Faltan en PATH: ${missing[*]} (Windows: infrastructure/scripts/setup-aws-tools.ps1)"
}

# Account id of the credentials in use; fails early with a clear message.
aws_account_id() {
  aws sts get-caller-identity --query Account --output text 2>/dev/null | tr -d '\r'
}

check_identity() {
  local identity
  identity="$(aws sts get-caller-identity --query Arn --output text 2>/dev/null | tr -d '\r')" ||
    die "Sin credenciales de AWS válidas: ejecuta 'aws configure' (o exporta AWS_PROFILE)."
  [[ -n "$identity" ]] || die "Sin credenciales de AWS válidas: ejecuta 'aws configure' (o exporta AWS_PROFILE)."
  printf 'Identidad AWS: %s (region %s)\n' "$identity" "$AWS_REGION"
}

# Globally unique name of the state bucket; TF_STATE_BUCKET overrides it.
state_bucket_name() {
  if [[ -n "${TF_STATE_BUCKET:-}" ]]; then printf '%s' "$TF_STATE_BUCKET"; return; fi
  printf 'minesense-safety-tfstate-%s' "$(aws_account_id)"
}

terraform_init() {
  local stack="$1" bucket="$2"
  terraform -chdir="$stack" init -input=false -reconfigure \
    -backend-config="bucket=$bucket" \
    -backend-config="dynamodb_table=$TF_LOCK_TABLE" \
    -backend-config="region=$AWS_REGION" >/dev/null
}

# tf_output <stack dir> <output>: empty when the output does not exist or is null.
tf_output() {
  local value
  value="$(terraform -chdir="$1" output -raw "$2" 2>/dev/null | tr -d '\r')" || value=""
  case "$value" in *"No value for output"*|*"Warning:"*|*"Error:"*) value="" ;; esac
  printf '%s' "$value"
}

# json_field <key> < json: value of a top-level string field. Written in awk because neither jq nor
# Python come with Git Bash. Output goes to the caller's variable, never to the terminal.
json_field() {
  awk -v key="$1" '
    function hexval(text,    i, value) {
      value = 0
      for (i = 1; i <= length(text); i++) value = value * 16 + index("0123456789abcdef", tolower(substr(text, i, 1))) - 1
      return value
    }
    BEGIN { RS = "\001" }
    {
      if (!match($0, "\"" key "\"[[:space:]]*:[[:space:]]*\"")) next
      text = substr($0, RSTART + RLENGTH); out = ""
      for (i = 1; i <= length(text); i++) {
        c = substr(text, i, 1)
        if (c == "\"") break
        if (c != "\\") { out = out c; continue }
        i++; c = substr(text, i, 1)
        if (c == "n") out = out "\n"
        else if (c == "t") out = out "\t"
        else if (c == "r") out = out "\r"
        else if (c == "u") { out = out sprintf("%c", hexval(substr(text, i + 1, 4))); i += 4 }
        else out = out c
      }
      printf "%s", out
    }'
}

b64() { base64 | tr -d '\r\n'; }

# True when a value can be read from an existing Kubernetes Secret; prints the decoded value.
read_secret_key() {
  local namespace="$1" secret="$2" key="$3" encoded
  encoded="$(kubectl get secret "$secret" -n "$namespace" -o "jsonpath={.data['$key']}" --ignore-not-found 2>/dev/null | tr -d '\r')"
  [[ -n "$encoded" ]] || return 1
  printf '%s' "$encoded" | base64 -d
}

# Random password that satisfies any reasonable policy; only ASCII letters, digits and a dash.
random_password() { printf '%s-Aa1' "$(openssl rand -hex 16 | tr -d '\r')"; }

# Terraform stack of the environment.
stack_dir() { printf '%s/environments/%s' "$TF_DIR" "$1"; }
