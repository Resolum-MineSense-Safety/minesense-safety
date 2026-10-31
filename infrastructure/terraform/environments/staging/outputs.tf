output "cluster_name" {
  value = module.eks.cluster_name
}

output "postgres_endpoint" {
  value = module.data.postgres_endpoint
}

output "documentdb_endpoint" {
  value = module.data.documentdb_endpoint
}

output "kafka_cluster_arn" {
  value = module.messaging.kafka_cluster_arn
}

output "kafka_bootstrap_brokers" {
  value = module.messaging.kafka_bootstrap_brokers
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
