variable "name" {
  type = string
}

variable "topic_root" {
  description = "Root of the MQTT topics published by the Edge gateways."
  type        = string
  default     = "minesense"
}

variable "tags" {
  type    = map(string)
  default = {}
}
