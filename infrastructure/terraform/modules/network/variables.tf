variable "name" {
  description = "Prefix for every resource (project-environment)."
  type        = string
}

variable "cidr_block" {
  description = "CIDR of the VPC."
  type        = string
}

variable "availability_zones" {
  description = "Availability zones used by the subnets (at least two for EKS and RDS)."
  type        = list(string)
}

variable "single_nat_gateway" {
  description = "One shared NAT gateway (cheaper, staging) or one per AZ (resilient, production)."
  type        = bool
  default     = true
}

variable "tags" {
  type    = map(string)
  default = {}
}
