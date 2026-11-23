variable "name" {
  type = string
}

variable "vpc_id" {
  type = string
}

variable "private_subnet_ids" {
  type = list(string)
}

variable "allowed_security_group_ids" {
  type = list(string)
}

variable "oidc_provider_arn" {
  description = "EKS OIDC provider, used to grant the services their IAM role (IRSA)."
  type        = string
}

variable "oidc_provider" {
  type = string
}

variable "kubernetes_namespace" {
  type = string
}

variable "client_service_accounts" {
  description = "ServiceAccounts of the services that call AddMineSenseEvents (publishers and consumers)."
  type        = list(string)
  default = [
    "alert-service",
    "analytics-service",
    "edge-synchronization-service",
    "fatigue-detection-service",
    "feedback-reliability-service",
    "fleet-monitoring-service",
    "incident-management-service",
    "privacy-audit-service",
  ]
}

variable "tags" {
  type    = map(string)
  default = {}
}
