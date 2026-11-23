# One-time stack that creates the remote state backend shared by every
# environment: an encrypted, versioned S3 bucket and a DynamoDB lock table.
# It also registers GitHub Actions as an OpenID Connect provider of the account,
# so the deployment pipeline gets short-lived AWS credentials without access keys.
terraform {
  required_version = ">= 1.6.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.70"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

variable "aws_region" {
  description = "Region of the state backend."
  type        = string
  default     = "us-east-1"
}

variable "state_bucket_name" {
  description = "Globally unique name of the Terraform state bucket (deploy-all.sh uses minesense-safety-tfstate-<account id>)."
  type        = string
}

variable "lock_table_name" {
  description = "DynamoDB table with the state locks."
  type        = string
  default     = "minesense-safety-terraform-locks"
}

variable "force_destroy_state" {
  description = "Apply with true right before destroying this stack: S3 refuses to delete a bucket with objects, which is what protects the state from an accidental destroy."
  type        = bool
  default     = false
}

variable "create_github_oidc_provider" {
  description = "False when the account already has the GitHub Actions OIDC provider (there can only be one)."
  type        = bool
  default     = true
}

resource "aws_s3_bucket" "state" {
  bucket        = var.state_bucket_name
  force_destroy = var.force_destroy_state
}

resource "aws_s3_bucket_versioning" "state" {
  bucket = aws_s3_bucket.state.id

  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "state" {
  bucket = aws_s3_bucket.state.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "aws:kms"
    }
  }
}

resource "aws_s3_bucket_public_access_block" "state" {
  bucket                  = aws_s3_bucket.state.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_dynamodb_table" "locks" {
  name         = var.lock_table_name
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "LockID"

  attribute {
    name = "LockID"
    type = "S"
  }
}

# GitHub Actions OIDC provider: one per AWS account, shared by Staging and Production.
# Each environment creates its own deploy role that trusts it (modules/delivery).
resource "aws_iam_openid_connect_provider" "github" {
  count = var.create_github_oidc_provider ? 1 : 0

  url            = "https://token.actions.githubusercontent.com"
  client_id_list = ["sts.amazonaws.com"]
  # AWS validates GitHub's certificate chain itself; the thumbprint is kept for older providers.
  thumbprint_list = ["6938fd4d98bab03faadb97b34396831e3780aea1"]
}

output "github_oidc_provider_arn" {
  value = one(aws_iam_openid_connect_provider.github[*].arn)
}

output "state_bucket" {
  value = aws_s3_bucket.state.bucket
}

output "lock_table" {
  value = aws_dynamodb_table.locks.name
}

# ---------- Infrastructure pipeline (infrastructure-cd.yml) ----------
# Two roles so a pull request can never change the account:
#   terraform-plan  - read-only, assumable from pull requests and develop: terraform plan + cost report
#   terraform-apply - administrator, assumable ONLY from the protected GitHub environment
#                     "infrastructure", which requires a reviewer's approval before each apply/destroy.

variable "github_repository" {
  description = "Repository whose workflows may assume the infrastructure roles."
  type        = string
  default     = "Resolum-MineSense-Safety/minesense-safety"
}

variable "create_github_terraform_roles" {
  description = "Create the plan/apply roles of the infrastructure pipeline."
  type        = bool
  default     = true
}

data "aws_caller_identity" "current" {}

locals {
  github_oidc_arn = "arn:aws:iam::${data.aws_caller_identity.current.account_id}:oidc-provider/token.actions.githubusercontent.com"
  terraform_roles = var.create_github_terraform_roles ? {
    plan = {
      subjects = [
        "repo:${var.github_repository}:pull_request",
        "repo:${var.github_repository}:ref:refs/heads/develop",
      ]
      policy = "arn:aws:iam::aws:policy/ReadOnlyAccess"
    }
    apply = {
      subjects = ["repo:${var.github_repository}:environment:infrastructure"]
      policy   = "arn:aws:iam::aws:policy/AdministratorAccess"
    }
  } : {}
}

data "aws_iam_policy_document" "github_terraform_assume" {
  for_each = local.terraform_roles

  statement {
    actions = ["sts:AssumeRoleWithWebIdentity"]

    principals {
      type        = "Federated"
      identifiers = [local.github_oidc_arn]
    }

    condition {
      test     = "StringEquals"
      variable = "token.actions.githubusercontent.com:aud"
      values   = ["sts.amazonaws.com"]
    }

    condition {
      test     = "StringEquals"
      variable = "token.actions.githubusercontent.com:sub"
      values   = each.value.subjects
    }
  }
}

resource "aws_iam_role" "github_terraform" {
  for_each = local.terraform_roles

  name                 = "minesense-github-terraform-${each.key}"
  description          = "GitHub Actions infrastructure pipeline: terraform ${each.key}."
  assume_role_policy   = data.aws_iam_policy_document.github_terraform_assume[each.key].json
  max_session_duration = 7200

  depends_on = [aws_iam_openid_connect_provider.github]
}

resource "aws_iam_role_policy_attachment" "github_terraform" {
  for_each = local.terraform_roles

  role       = aws_iam_role.github_terraform[each.key].name
  policy_arn = each.value.policy
}

# ReadOnlyAccess does not read secret values, and terraform plan refreshes the DocumentDB password
# version. Only the secrets of this project, and plan runs with -lock=false so it never writes.
data "aws_iam_policy_document" "plan_secrets" {
  statement {
    actions   = ["secretsmanager:GetSecretValue"]
    resources = ["arn:aws:secretsmanager:*:${data.aws_caller_identity.current.account_id}:secret:minesense-*"]
  }
}

resource "aws_iam_role_policy" "plan_secrets" {
  count  = var.create_github_terraform_roles ? 1 : 0
  name   = "read-project-secrets"
  role   = aws_iam_role.github_terraform["plan"].id
  policy = data.aws_iam_policy_document.plan_secrets.json
}

output "github_terraform_plan_role_arn" {
  description = "Repository variable AWS_TERRAFORM_PLAN_ROLE_ARN."
  value       = try(aws_iam_role.github_terraform["plan"].arn, null)
}

output "github_terraform_apply_role_arn" {
  description = "Repository variable AWS_TERRAFORM_APPLY_ROLE_ARN."
  value       = try(aws_iam_role.github_terraform["apply"].arn, null)
}
