output "kafka_cluster_arn" {
  value = aws_msk_serverless_cluster.events.arn
}

output "kafka_client_role_arn" {
  description = "IRSA role of the ServiceAccounts in client_service_accounts."
  value       = aws_iam_role.kafka_client.arn
}

output "kafka_bootstrap_brokers" {
  value = data.aws_msk_bootstrap_brokers.events.bootstrap_brokers_sasl_iam
}
