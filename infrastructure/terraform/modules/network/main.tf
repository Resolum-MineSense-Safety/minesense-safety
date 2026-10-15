# VPC with public subnets (load balancers, NAT) and private subnets
# (EKS nodes, databases, Lambda). Subnets are tagged for the AWS Load
# Balancer Controller so Kubernetes Services/Ingresses find them.
module "vpc" {
  source  = "terraform-aws-modules/vpc/aws"
  version = "~> 5.13"

  name = var.name
  cidr = var.cidr_block
  azs  = var.availability_zones

  public_subnets  = [for index, _ in var.availability_zones : cidrsubnet(var.cidr_block, 8, index)]
  private_subnets = [for index, _ in var.availability_zones : cidrsubnet(var.cidr_block, 4, index + 1)]

  enable_nat_gateway   = true
  single_nat_gateway   = var.single_nat_gateway
  enable_dns_hostnames = true
  enable_dns_support   = true

  public_subnet_tags = {
    "kubernetes.io/role/elb" = 1
  }

  private_subnet_tags = {
    "kubernetes.io/role/internal-elb" = 1
  }

  tags = var.tags
}
