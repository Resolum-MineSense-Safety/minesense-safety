variable "name" {
  description = "Prefix of the resources, e.g. minesense-staging."
  type        = string
}

variable "cluster_name" {
  type = string
}

variable "cluster_arn" {
  type = string
}

variable "oidc_provider_arn" {
  description = "OIDC provider of the EKS cluster (IRSA)."
  type        = string
}

variable "github_repository" {
  description = "owner/name of the repository allowed to deploy."
  type        = string
  default     = "Resolum-MineSense-Safety/minesense-safety"
}

variable "github_environment" {
  description = "GitHub environment whose jobs may assume the deploy role (staging or production)."
  type        = string
}

variable "tags" {
  type    = map(string)
  default = {}
}
