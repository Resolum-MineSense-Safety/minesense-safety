variable "aws_region" {
  type    = string
  default = "us-east-1"
}

variable "project" {
  type    = string
  default = "minesense"
}

variable "environment" {
  type    = string
  default = "production"
}

variable "vpc_cidr" {
  type    = string
  default = "10.30.0.0/16"
}

variable "availability_zones" {
  type    = list(string)
  default = ["us-east-1a", "us-east-1b", "us-east-1c"]
}

variable "single_nat_gateway" {
  type    = bool
  default = false
}

variable "kubernetes_version" {
  description = "EKS version in standard support; versions in extended support cost 0.60 USD/h instead of 0.10."
  type        = string
  default     = "1.35"
}

variable "enable_kafka" {
  description = "MSK Serverless (ADR-004). It is the most expensive piece per hour; without it the services only log their events."
  type        = bool
  default     = true
}

variable "node_instance_types" {
  type    = list(string)
  default = ["m6i.large"]
}

variable "node_min_size" {
  type    = number
  default = 4
}

variable "node_desired_size" {
  type    = number
  default = 4
}

variable "node_max_size" {
  type    = number
  default = 6
}

variable "postgres_instance_class" {
  type    = string
  default = "db.t4g.medium"
}

variable "high_availability" {
  description = "Multi-AZ PostgreSQL and two DocumentDB instances."
  type        = bool
  default     = true
}

variable "backup_retention_days" {
  type    = number
  default = 14
}

variable "deletion_protection" {
  type    = bool
  default = true
}

variable "report_lambda_package_path" {
  description = "Zip built by the CD pipeline from src/serverless/ReportPdfGenerator."
  type        = string
  default     = "../../../../src/serverless/ReportPdfGenerator/artifacts/ReportPdfGenerator.zip"
}

variable "access_groups" {
  description = "IAM role ARN per RBAC group (oncall, deployers) of infrastructure/k8s/rbac.yaml."
  type        = map(string)
  default     = {}
}
