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
