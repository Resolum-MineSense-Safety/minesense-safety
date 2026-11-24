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

variable "force_destroy_bucket" {
  description = "Delete the generated reports together with the bucket on terraform destroy."
  type        = bool
  default     = false
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

variable "alarm_email" {
  description = "Address subscribed to the report-flow alarms; empty keeps the SNS topic without subscribers."
  type        = string
  default     = ""
}

variable "queue_age_alarm_seconds" {
  description = "Alarm when the oldest report request waits longer than this."
  type        = number
  default     = 600
}

variable "duration_alarm_ratio" {
  description = "Alarm when the p95 render time goes above this fraction of the Lambda timeout."
  type        = number
  default     = 0.8
}
