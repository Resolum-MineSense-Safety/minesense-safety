output "mqtt_endpoint" {
  description = "MQTTS endpoint the Edge gateways connect to (port 8883)."
  value       = data.aws_iot_endpoint.data.endpoint_address
}

output "gateway_policy_name" {
  value = aws_iot_policy.gateway.name
}
