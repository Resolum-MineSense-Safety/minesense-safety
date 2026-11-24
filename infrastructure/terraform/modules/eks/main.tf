# Managed Kubernetes cluster (ADR-008). Each environment gets its own cluster;
# microservices run in the namespace named after the environment and are
# deployed with the Helm chart under infrastructure/helm.
module "eks" {
  source  = "terraform-aws-modules/eks/aws"
  version = "~> 20.24"

  cluster_name    = var.cluster_name
  cluster_version = var.kubernetes_version

  vpc_id     = var.vpc_id
  subnet_ids = var.private_subnet_ids

  # kubectl and Helm reach the API from outside (laptop, GitHub Actions); the nodes use the private endpoint.
  cluster_endpoint_public_access           = true
  cluster_endpoint_private_access          = true
  enable_cluster_creator_admin_permissions = true
  enable_irsa                              = true

  cluster_addons = {
    coredns                = {}
    kube-proxy             = {}
    vpc-cni                = { before_compute = true }
    eks-pod-identity-agent = {}
  }

  eks_managed_node_groups = {
    services = {
      # Amazon Linux 2 no longer exists for Kubernetes 1.33 and later.
      ami_type       = "AL2023_x86_64_STANDARD"
      instance_types = var.node_instance_types
      min_size       = var.node_min_size
      desired_size   = var.node_desired_size
      max_size       = var.node_max_size
      labels = {
        workload = "microservices"
      }
    }
  }

  tags = var.tags
}
