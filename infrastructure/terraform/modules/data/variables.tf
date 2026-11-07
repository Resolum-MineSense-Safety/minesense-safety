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
  description = "Security groups allowed to reach the databases (EKS nodes)."
  type        = list(string)
}

variable "postgres_instance_class" {
  type    = string
  default = "db.t4g.micro"
}

variable "postgres_multi_az" {
  type    = bool
  default = false
}

variable "documentdb_instance_class" {
  type    = string
  default = "db.t4g.medium"
}

variable "documentdb_instances" {
  type    = number
  default = 1
}

variable "backup_retention_days" {
  type    = number
  default = 7
}

variable "deletion_protection" {
  type    = bool
  default = false
}

variable "tags" {
  type    = map(string)
  default = {}
}
