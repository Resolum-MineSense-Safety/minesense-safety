output "aws_region" {
  value = var.aws_region
}

output "vpc_id" {
  value = module.network.vpc_id
}

output "cluster_name" {
  value = module.eks.cluster_name
}

output "postgres_endpoint" {
  value = module.data.postgres_endpoint
}

output "postgres_address" {
  description = "Host of the PostgreSQL endpoint, without the port."
  value       = module.data.postgres_address
}

output "postgres_secret_arn" {
  description = "Secret managed by RDS with the master credentials (create-k8s-secrets.sh reads it)."
  value       = module.data.postgres_secret_arn
}

output "documentdb_endpoint" {
  value = module.data.documentdb_endpoint
}

output "documentdb_secret_arn" {
  description = "Secret with the DocumentDB master credentials (create-k8s-secrets.sh reads it)."
  value       = module.data.documentdb_secret_arn
}

output "kafka_cluster_arn" {
  value = one(module.messaging[*].kafka_cluster_arn)
}

output "kafka_bootstrap_brokers" {
  description = "Empty when enable_kafka is false: the services then only log their events."
  value       = try(module.messaging[0].kafka_bootstrap_brokers, "")
}

output "kafka_client_role_arn" {
  description = "IRSA role of the services that publish or consume domain events."
  value       = try(module.messaging[0].kafka_client_role_arn, "")
}

output "mqtt_endpoint" {
  value = module.iot.mqtt_endpoint
}

output "reports_bucket" {
  value = module.reporting.reports_bucket
}

output "report_requests_queue_url" {
  value = module.reporting.report_requests_queue_url
}

output "reporting_service_role_arn" {
  value = module.reporting.reporting_service_role_arn
}

output "github_deploy_role_arn" {
  value = module.delivery.deploy_role_arn
}

output "load_balancer_controller_role_arn" {
  value = module.delivery.load_balancer_controller_role_arn
}
