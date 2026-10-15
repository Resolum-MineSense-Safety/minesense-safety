output "kafka_cluster_arn" {
  value = aws_msk_serverless_cluster.events.arn
}
