terraform {
  required_version = ">= 1.6.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.70"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }

  # Remote state created by ../../bootstrap; one key per environment. The bucket name is globally
  # unique, so it is not fixed here:
  #   terraform init -backend-config="bucket=minesense-safety-tfstate-<account id>" \
  #     -backend-config="dynamodb_table=minesense-safety-terraform-locks" -backend-config="region=us-east-1"
  backend "s3" {
    key     = "staging/terraform.tfstate"
    encrypt = true
  }
}

provider "aws" {
  region = var.aws_region

  default_tags {
    tags = local.tags
  }
}

locals {
  name = "${var.project}-${var.environment}"
  tags = {
    Project     = "MineSense Safety"
    Environment = var.environment
    ManagedBy   = "Terraform"
    Repository  = "Resolum-MineSense-Safety/minesense-safety"
  }
}

module "network" {
  source = "../../modules/network"

  name               = local.name
  cidr_block         = var.vpc_cidr
  availability_zones = var.availability_zones
  single_nat_gateway = var.single_nat_gateway
}

module "eks" {
  source = "../../modules/eks"

  cluster_name        = local.name
  kubernetes_version  = var.kubernetes_version
  vpc_id              = module.network.vpc_id
  private_subnet_ids  = module.network.private_subnet_ids
  node_instance_types = var.node_instance_types
  node_min_size       = var.node_min_size
  node_desired_size   = var.node_desired_size
  node_max_size       = var.node_max_size
  access_groups       = var.access_groups
}

module "data" {
  source = "../../modules/data"

  name                       = local.name
  vpc_id                     = module.network.vpc_id
  private_subnet_ids         = module.network.private_subnet_ids
  allowed_security_group_ids = [module.eks.node_security_group_id]
  postgres_instance_class    = var.postgres_instance_class
  postgres_multi_az          = var.high_availability
  documentdb_instances       = var.high_availability ? 2 : 1
  backup_retention_days      = var.backup_retention_days
  deletion_protection        = var.deletion_protection
}

module "messaging" {
  source = "../../modules/messaging"
  count  = var.enable_kafka ? 1 : 0

  name                       = local.name
  vpc_id                     = module.network.vpc_id
  private_subnet_ids         = module.network.private_subnet_ids
  allowed_security_group_ids = [module.eks.node_security_group_id]
  oidc_provider_arn          = module.eks.oidc_provider_arn
  oidc_provider              = module.eks.oidc_provider
  kubernetes_namespace       = var.environment
}

module "iot" {
  source = "../../modules/iot"

  name = local.name
}

module "reporting" {
  source = "../../modules/reporting"

  name                 = local.name
  lambda_package_path  = var.report_lambda_package_path
  oidc_provider_arn    = module.eks.oidc_provider_arn
  oidc_provider        = module.eks.oidc_provider
  kubernetes_namespace = var.environment
  force_destroy_bucket = !var.deletion_protection
}

module "delivery" {
  source = "../../modules/delivery"

  name               = local.name
  cluster_name       = module.eks.cluster_name
  cluster_arn        = module.eks.cluster_arn
  oidc_provider_arn  = module.eks.oidc_provider_arn
  github_environment = var.environment
  tags               = local.tags
}
