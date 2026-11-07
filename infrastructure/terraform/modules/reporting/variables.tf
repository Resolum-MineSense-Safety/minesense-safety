variable "name" {
  type = string
}

variable "lambda_package_path" {
  description = "Zip produced by 'dotnet lambda package' for src/serverless/ReportPdfGenerator."
  type        = string
}

variable "lambda_memory_mb" {
  type    = number
  default = 1024
}

variable "lambda_timeout_seconds" {
  type    = number
  default = 120
}

variable "report_retention_days" {
  description = "Days generated PDFs are kept before expiring."
  type        = number
  default     = 365
}

variable "oidc_provider_arn" {
  description = "EKS OIDC provider, used to grant the Reporting Service its IAM role."
  type        = string
}

variable "oidc_provider" {
  type = string
}

variable "kubernetes_namespace" {
  type = string
}

variable "tags" {
  type    = map(string)
  default = {}
}
